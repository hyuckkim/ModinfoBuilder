using System.Security.Cryptography;

internal class MD5Util
{
    private readonly MD5 md5 = MD5.Create();
    public async Task<string> CalculatePath(string path)
    {
        FileStream stream = File.OpenRead(path);
        // byte[] bytes = ReadAllBytes(stream);
        var result = await CalculateAsync(stream);

        stream.Close();
        return result;
    }

    private string Calculate(byte[] bytes)
    {
        byte[] res = md5
        .ComputeHash(bytes);
        return res
            .Aggregate("", (acc, x) => $"{acc}{x:x2}")
            .ToUpper();
    }

    private async Task<string> CalculateAsync(FileStream stream)
    {
        byte[] res = await md5.ComputeHashAsync(stream);
        return res
            .Aggregate("", (acc, x) => $"{acc}{x:x2}")
            .ToUpper();
    }

    private static byte[] ReadAllBytes(Stream instream)
    {
        if (instream is MemoryStream stream)
        {
            return stream.ToArray();
        }

        using MemoryStream memoryStream = new();
        instream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}