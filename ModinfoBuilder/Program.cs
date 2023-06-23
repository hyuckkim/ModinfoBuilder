﻿using System.Xml;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ModinfoBuilder;

if (args.Length == 0)
{
    Console.Write("사용 방법: ModinfoBuilder.exe <폴더 경로>");
    return;
}

string rootPath = new DirectoryInfo(args[0]).FullName.ReplaceSlash();
Console.WriteLine($"> {rootPath}");
Console.WriteLine();

string[] info = rootPath.GetAllModinfo().ToArray();

var modinfoList = 
    from string path in info
    let modInfo = new FileInfo(path)
    let folderName = Regex.Replace(path, @"[/\\][^/\\]+\..+", "")// Remove file name and use paths only
    select new ModinfoInfo(modInfo, folderName);
    
// Todo : modinfoInfo 클래스를 만들어서 분리!
foreach (var modinfo in modinfoList)
{
    await modinfo.Modify();
}

Console.Write("계속하려면 아무 키나 누르십시오...");
Console.Read();

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
internal static class MD5Util
{
    static readonly MD5 md5 = MD5.Create();
    public static async Task<string> Calculate(FileStream stream)
    {
        byte[] res = await md5
        .ComputeHashAsync(stream);
        return res
            .Aggregate("", (acc, x) => $"{acc}{x:x2}")
            .ToUpper();
    }
}