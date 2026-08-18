using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Venly.Messaging.Events;

namespace Venly.Notification.Helper;

public sealed class NotificationClient(
    HttpClient httpClient, IOptions<NotificationClientOptions> options) : INotificationClient
{
    private const string SendPath = "/api/v1/notifications/send";

    public async Task SendAsync(NotificationRequested request, CancellationToken ct = default)
    {
        var secret = options.Value.HmacSecret;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("NotificationClient:HmacSecret is not configured.");

        var body = JsonSerializer.Serialize(request);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ComputeSignature(secret, timestamp, "POST", SendPath, body);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, SendPath)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("X-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-Signature", signature);

        using var response = await httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    public static string ComputeSignature(string secret, long timestamp, string method, string path, string body)
    {
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        var signingString = $"{timestamp}\n{method}\n{path}\n{bodyHash}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signingString));
        return Convert.ToBase64String(hash);
    }
}
