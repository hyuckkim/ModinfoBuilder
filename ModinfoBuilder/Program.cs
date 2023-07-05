using ModinfoBuilder;
using System.Text.RegularExpressions;

if (args.Length == 0)
{
    Console.Write("사용 방법: ModinfoBuilder.exe <폴더 경로>");
    return;
}

string rootPath = new DirectoryInfo(args[0]).FullName.ReplaceSlash();
Console.WriteLine($"> {rootPath}");
Console.WriteLine();

string[] info = rootPath.GetAllModinfo().ToArray();

List<ModinfoInfo> modinfoList = (
    from string path in info
    let modInfo = new FileInfo(path)
    let folderName = Regex.Replace(path, @"[/\\][^/\\]+\..+", "")// Remove file name and use paths only
    select new ModinfoInfo(modInfo, folderName)
    ).ToList();

await Task.WhenAll(modinfoList.Select(async m =>
{
    ModinfoRecord r = await m.Modify();
    Console.Write(m.Log);
    return r;
}));

Console.Write("계속하려면 아무 키나 누르십시오...");
Console.Read();

internal static class Extension
{
    public static string ReplaceSlash(this string s)
    {
        return s.Replace("\\", "/");
    }

    public static IEnumerable<string> GetAllModinfo(this string path)
    {
        return Directory.GetFiles(path, "*.modinfo", SearchOption.AllDirectories);
    }

    public static IEnumerable<string> GetAllSources(this string path)
    {
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Where(e => !e.EndsWith(".modinfo"))
                .Select(e => e.ReplaceSlash());
    }
}
