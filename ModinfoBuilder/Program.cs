using System.Xml;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

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
        XmlNodeList nodes = GetFileNodes(doc);

        foreach (var data in nodes.Cast<XmlNode>()
            .Select(e => (node: e, path: GetPathbyNode(e, files)))
            .Select(n => (code: ChangeResource(n.path, (XmlElement)n.node), n.path)))
        {

            switch (await data.code)
            { 
                case FileStatus.notFound:
                    notFound++;
                    break;
                case FileStatus.ignored:
                    ignored++;
                    break;
                case FileStatus.changed:
                    changed++;
                    break;
            }
            if (data.path is not null)
            {
                files.Remove(data.path);
            }
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
string? GetPathbyNode(XmlNode node, IEnumerable<string> files)
{
    string checkPath = node.InnerText.Replace("\\", "/");
    return files
        .Where(p => p.EndsWith(checkPath))
        .FirstOrDefault();
}
async Task<FileStatus> ChangeResource(string? path, XmlElement node)
{
    if (path is null)
    {
        Console.WriteLine($"경고 : {path}가 없습니다.");
        return FileStatus.notFound;
    }

    FileStream ns = File.OpenRead(path);

    string oldHash = node.GetAttribute("md5");

    string hash = await CalculateMD5ByFile(ns);
    node.SetAttribute("md5", hash);

    string newHash = node.GetAttribute("md5");
    if (oldHash != newHash)
    {
        Console.WriteLine($"{path}의 해시를 수정했습니다 : {oldHash[..8]}.. -> {newHash[..8]}..");
        return FileStatus.changed;
    }
    else return FileStatus.ignored;
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

enum FileStatus
{
    changed,
    ignored,
    notFound
}