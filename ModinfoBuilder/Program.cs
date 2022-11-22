using System.Xml;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;

MD5 md5 = MD5.Create();

string rootPath;
if (args.Length > 0)
{
    rootPath = args[0];
}
else
{
    Console.Write("경로를 입력하십시오: ");
    rootPath = Console.ReadLine() ?? Directory.GetCurrentDirectory();
    rootPath = rootPath.Trim('\"');
}
string[] info = GetAllModinfo(rootPath).ToArray();

Regex regex = new(@"[/\\][^/\\]+\..+"); // Select File Name Only in All Path.
foreach (string path in info)
{
    FileInfo modInfo = new(path);
    string folderName = regex.Replace(path, ""); // Remove Filename and use paths only

    if (modInfo is not null)
    {
        Console.WriteLine($"{folderName}");

        var (changed, ignored, notFound, missed) = await modifyModinfo(modInfo, folderName);

        Console.WriteLine($"{changed} 변경됨, {ignored} 유지됨, {notFound}, 파일 없음, {missed} modinfo에 없음");
        Console.WriteLine();
    }
}
Console.Write("계속하려면 아무 키나 누르십시오...");
Console.Read();

async Task<(int changed, int ignored, int notFound, int missed)> modifyModinfo(FileInfo modInfo, string path)
{
    (int changed, int ignored, int notFound, int missed) = (0, 0, 0, 0);
    XmlDocument doc = GetDocumentByPath(modInfo);
    List<string> files = GetAllSources(path).ToList();
    try
    {
        foreach (Task<FileStatus>? data in GetFileNodes(doc)
            .Cast<XmlNode>()
            .Select(async n => await ChangeResource(files, (XmlElement)n)))
        {
            FileStatus code = await data;
            Console.Write(code.StatusText());
            if (code.FileBeRemove())
            {
                files.Remove(code.FilePath);
            }
            if (code is Changed) changed++;
            if (code is Ignored) ignored++;
            if (code is NotFound) notFound++;
        }
        using FileStream stream = modInfo.Open(FileMode.Create);
        doc.Save(stream);
    }
    catch (Exception e)
    {
        Console.WriteLine($"{modInfo.Name}이 {e}");
    }
    foreach (string file in files)
    {
        Console.WriteLine($"경고 : {file}이 modinfo에 없습니다.");
    }
    missed = files.Count;
    return (changed, ignored, notFound, missed);
}
async Task<FileStatus> ChangeResource(IEnumerable<string> files, XmlElement node)
{
    string checkPath = node.InnerText.Replace("\\", "/");
    string? path = files
        .Where(p => p.EndsWith(checkPath))
        .FirstOrDefault();
    if (path is null) return new NotFound(checkPath);

    FileStream ns = File.OpenRead(path);

    string oldHash = node.GetAttribute("md5");

    string hash = await CalculateMD5ByFile(ns);
    node.SetAttribute("md5", hash);

    string newHash = node.GetAttribute("md5");

    if (oldHash != newHash) return new Changed(path, oldHash, newHash);
    else return new Ignored(path);
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

IEnumerable<string> GetAllModinfo(string path) => Directory.GetFiles(path, "*.modinfo", SearchOption.AllDirectories);
IEnumerable<string> GetAllSources(string path) => Directory.GetFiles(path, "*", SearchOption.AllDirectories)
        .Where(e => !e.EndsWith(".modinfo"))
        .Select(e => e.Replace("\\", "/"));


abstract class FileStatus
{
    public abstract string StatusText();
    public abstract bool FileBeRemove();
    public string FilePath { get; set; } = string.Empty;

}

class Changed : FileStatus
{
    private readonly string oldHash, newHash;
    public Changed(string FilePath, string oldHash, string newHash)
    {
        this.oldHash = oldHash;
        this.newHash = newHash;
        this.FilePath = FilePath;
    }
    public override string StatusText() => $"{FilePath}의 해시를 수정했습니다 : {oldHash[..8]}.. -> {newHash[..8]}..\n";
    public override bool FileBeRemove() => true;
}

class Ignored : FileStatus
{
    public Ignored(string FilePath)
    {
        this.FilePath = FilePath;
    }
    public override string StatusText() => string.Empty;
    public override bool FileBeRemove() => true;
}

class NotFound : FileStatus
{
    public NotFound(string CheckPath)
    {
        FilePath = CheckPath;
    }
    public override string StatusText() => $"경고 : {FilePath}가 없습니다.\n";
    public override bool FileBeRemove() => false;
}