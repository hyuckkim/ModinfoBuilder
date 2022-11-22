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

        var mi = modifyModinfo(modInfo, folderName);

        Console.WriteLine($"{mi.changed} 변경됨, {mi.ignored} 유지됨, {mi.notFound}, 파일 없음, {mi.missed} modinfo에 없음");
        Console.WriteLine();
    }
}
Console.Write("계속하려면 아무 키나 누르십시오...");
Console.Read();

(int changed, int ignored, int notFound, int missed) modifyModinfo(FileInfo modInfo, string path)
{
    (int changed, int ignored, int notFound, int missed) = (0, 0, 0, 0);
    XmlDocument doc = GetDocumentByPath(modInfo);
    List<string> files = GetAllSources(path).ToList();
    try
    {
        XmlNodeList nodes = GetFileNodes(doc);

        foreach (XmlNode node in nodes)
        {
            string checkPath = node.InnerText.Replace("\\", "/");
            string? fullPath = files
                .Where(p => p.EndsWith(checkPath))
                .FirstOrDefault();
            if (fullPath is null)
            {
                Console.WriteLine($"경고 : {checkPath}가 없습니다.");
                notFound++;
                continue;
            }

            FileStream ns = File.OpenRead(fullPath);
            XmlElement element = (XmlElement)node;

            string oldHash = element.GetAttribute("md5"); 

            string hash = CalculateMD5ByFile(ns);
            element.SetAttribute("md5", hash);

            string newHash = element.GetAttribute("md5");
            if (oldHash != newHash)
            {
                Console.WriteLine($"{checkPath}의 해시를 수정했습니다 : {oldHash[..8]}.. -> {newHash[..8]}..");
                changed++;
            }
            else ignored++;
            files.Remove(fullPath);
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

string CalculateMD5ByFile(FileStream stream) => md5
    .ComputeHash(stream)
    .Aggregate("", (acc, x) => $"{acc}{x:x2}")
    .ToUpper();

IEnumerable<string> GetAllModinfo(string path) => Directory.GetFiles(path, "*.modinfo", SearchOption.AllDirectories);
IEnumerable<string> GetAllSources(string path) => Directory.GetFiles(path, "*", SearchOption.AllDirectories)
        .Where(e => !e.EndsWith(".modinfo"))
        .Select(e => e.Replace("\\", "/"));