using System.Xml;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

MD5 md5 = MD5.Create();

if (args.Length == 0)
{
    Console.Write("사용 방법: ModinfoBuilder.exe <폴더 경로>");
    return;
}

string rootPath = new DirectoryInfo(args[0]).FullName.ReplaceSlash();
Console.WriteLine($"> {rootPath}");
Console.WriteLine();

string[] info = rootPath.GetAllModinfo().ToArray();

foreach (string path in info)
{
    FileInfo modInfo = new(path);
    
    string folderName = Regex.Replace(path, @"[/\\][^/\\]+\..+", ""); // Remove file name and use paths only

    if (modInfo is not null)
    {
        Console.WriteLine($"- {folderName}");

        var (changed, ignored, notFound, missed) = await modifyModinfo(modInfo, folderName);

        Console.WriteLine($"{changed} 변경됨, {ignored} 유지됨, {notFound}, 파일 없음, {missed} modinfo에 없음");
        Console.WriteLine();
    }
}
Console.Write("계속하려면 아무 키나 누르십시오...");
Console.Read();


async Task<ModinfoRecord> modifyModinfo(FileInfo modInfo, string path)
{
    ModinfoRecord rec = (0, 0, 0, 0);
    XmlDocument doc = GetDocumentByPath(modInfo);
    List<string> unusedFiles = path.GetAllSources().ToList();
    try
    {
        foreach (XmlElement data in GetFileNodes(doc)
            .Cast<XmlNode>()
            .Select(node => (XmlElement)node))
        {
            var d = await ChangeResource(unusedFiles, data);
            rec.AddFileStatus(ResolveFileStatus(d, unusedFiles));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"{modInfo.Name}이 {e}");
    }
    foreach (string file in unusedFiles)
    {
        Console.WriteLine($"경고 : {file}이 modinfo에 없습니다.");
    }
    using FileStream stream = modInfo.Open(FileMode.Create);
    doc.Save(stream);

    rec.missed = unusedFiles.Count;
    return rec;
}
FileStatus ResolveFileStatus(FileStatus code, List<string> unusedFiles)
{
    Console.Write(code.StatusText);
    if (code.FileExists)
    {
        unusedFiles.Remove(code.FullPath);
    }

    return code;
}
async Task<FileStatus> ChangeResource(IEnumerable<string> files, XmlElement node)
{
    string checkPath = node.InnerText.ReplaceSlash();
    string? path = files
        .Where(p => p.EndsWith(checkPath))
        .FirstOrDefault();
    if (path is null) return new NotFound(checkPath);

    FileStream ns = File.OpenRead(path);

    string oldHash = node.GetAttribute("md5");

    string hash = await CalculateMD5ByFile(ns);
    node.SetAttribute("md5", hash);

    string newHash = node.GetAttribute("md5");

    if (oldHash != newHash) return new Changed(checkPath, oldHash, newHash, path);
    else return new Ignored(checkPath, path);
}
XmlDocument GetDocumentByPath(FileInfo path)
{
    XmlDocument doc = new();
    FileStream stream = path.OpenRead();
    doc.Load(stream);
    stream.Close();

    return doc;
}
XmlNodeList GetFileNodes(XmlDocument doc) => doc
    .SelectNodes("Mod/Files/File") 
    ?? throw new Exception("유효한 modinfo 파일이 아닙니다.");

async Task<string> CalculateMD5ByFile(FileStream stream)
{
    byte[] res = await md5
    .ComputeHashAsync(stream);
    return res
        .Aggregate("", (acc, x) => $"{acc}{x:x2}")
        .ToUpper();
}

internal static class Extension
{
    public static string ReplaceSlash(this string s) => s.Replace("\\", "/");

    public static IEnumerable<string> GetAllModinfo(this string path) 
        => Directory.GetFiles(path, "*.modinfo", SearchOption.AllDirectories);
    public static IEnumerable<string> GetAllSources(this string path) 
        => Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Where(e => !e.EndsWith(".modinfo"))
            .Select(e => e.ReplaceSlash());
}