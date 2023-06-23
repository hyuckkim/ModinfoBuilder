using System.Security.Cryptography;

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