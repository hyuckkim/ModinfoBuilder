using System.Security.Cryptography;

internal static class MD5Util
{
    static readonly MD5 md5 = MD5.Create();
    public static string Calculate(FileStream stream)
    {
        byte[] bytes = stream.ReadAllBytes();
        byte[] res = md5
        .ComputeHash(bytes);
        return res
            .Aggregate("", (acc, x) => $"{acc}{x:x2}")
            .ToUpper();
    }
    static byte[] ReadAllBytes(this Stream instream)
    {
        if (instream is MemoryStream stream)
            return stream.ToArray();

        using var memoryStream = new MemoryStream();
        instream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}