using System.Security.Cryptography;

internal class MD5Util
{
    readonly MD5 md5 = MD5.Create();
    public string CalculatePath(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] bytes = ReadAllBytes(stream);
        return Calculate(bytes);
    }
    string Calculate(byte[] bytes)
    {
        byte[] res = md5
        .ComputeHash(bytes);
        return res
            .Aggregate("", (acc, x) => $"{acc}{x:x2}")
            .ToUpper();
    }
    static byte[] ReadAllBytes(Stream instream)
    {
        if (instream is MemoryStream stream)
            return stream.ToArray();

        using var memoryStream = new MemoryStream();
        instream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}