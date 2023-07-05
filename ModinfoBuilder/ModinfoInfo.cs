using System.Text;
using System.Xml;

namespace ModinfoBuilder;

internal class ModinfoInfo
{
    private readonly FileInfo file;
    private readonly string path;

    private readonly IEnumerable<string> resources;
    private List<string>? unusedFiles;
    ModinfoRecord rec = (0, 0, 0, 0);

    readonly StringBuilder log = new();
    readonly MD5Util md5 = new();

    public StringBuilder Log { get => log; }

    public ModinfoInfo(FileInfo file, string path)
    {
        this.file = file;
        this.path = path;
        resources = path.GetAllSources();
    }

    public async Task<ModinfoRecord> Modify()
    {
        rec = (0, 0, 0, 0);
        Log.AppendLine($"- {path}");
        XmlDocument doc = GetDocumentByPath(file);
        unusedFiles = resources.ToList();
        try
        {
            foreach (XmlElement data in GetFileNodes(doc)
                .Cast<XmlNode>()
                .Select(node => (XmlElement)node))
            {
                var d = await ChangeResource(data);
                rec = rec.AddFileStatus(ResolveFileStatus(d));
            }
        }
        catch (Exception e)
        {
            Log.AppendLine($"{file.Name}이 {e}");
        }
        foreach (string file in unusedFiles)
        {
            Log.AppendLine($"경고 : {file}이 modinfo에 없습니다.");
        }
        using FileStream stream = file.Open(FileMode.Create);
        doc.Save(stream);

        rec.Missed = unusedFiles.Count;
        Log.AppendLine($"{rec.Changed} 변경됨, {rec.Ignored} 유지됨, {rec.NotFound}, 파일 없음, {rec.Missed} modinfo에 없음");
        Log.AppendLine();
        return rec;
    }

    static XmlDocument GetDocumentByPath(FileInfo path)
    {
        XmlDocument doc = new();
        FileStream stream = path.OpenRead();
        doc.Load(stream);
        stream.Close();

        return doc;
    }
    async Task<FileStatus> ChangeResource(XmlElement node)
    {
        string checkPath = node.InnerText.ReplaceSlash();
        string? path = unusedFiles
            ?.Where(p => p.EndsWith(checkPath))
            .FirstOrDefault();
        if (path is null) return new NotFound(checkPath);

        string oldHash = node.GetAttribute("md5");
        string newHash = await md5.CalculatePath(path);
        node.SetAttribute("md5", newHash);

        if (oldHash != newHash) return new Changed(checkPath, oldHash, newHash, path);
        else return new Ignored(checkPath, path);
    }
    FileStatus ResolveFileStatus(FileStatus code)
    {
        Log.Append(code.StatusText);
        if (code.FileExists && unusedFiles != null)
        {
            unusedFiles.Remove(code.FullPath);
        }

        return code;
    }
    static XmlNodeList GetFileNodes(XmlDocument doc) => doc
        .SelectNodes("Mod/Files/File")
        ?? throw new Exception("유효한 modinfo 파일이 아닙니다.");
}
