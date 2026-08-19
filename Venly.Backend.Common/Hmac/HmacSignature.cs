using System.Security.Cryptography;
using System.Text;

namespace Venly.Backend.Common.Hmac;

public static class HmacSignature
{
    public static string Compute(
        string secret, long timestamp, string method, string path, string body, string? nonce = null)
    {
        var bodyHash = ComputeSha256(body);
        var signingString = string.IsNullOrWhiteSpace(nonce)
            ? $"{timestamp}\n{method}\n{path}\n{bodyHash}"
            : $"{timestamp}\n{nonce}\n{method}\n{path}\n{bodyHash}";
        return ComputeHmac(secret, signingString);
    }

    public static string ComputeHmac(string secret, string data)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    public static string ComputeSha256(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
