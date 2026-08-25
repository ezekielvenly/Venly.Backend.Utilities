using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Venly.Audit.Helper;

/// <summary>
/// Signs and posts a seal request. Deliberately a near-copy of Venly.Notification.Helper's
/// <c>NotificationClient</c>: the signing string is the one <c>HmacAuthorizationFilter</c> verifies, and a
/// second implementation that differed by a newline would fail every request with a 401 that looks like a
/// wrong secret.
/// </summary>
public sealed class AuditMaintenanceClient(
    HttpClient httpClient, IOptions<AuditMaintenanceClientOptions> options) : IAuditMaintenanceClient
{
    public const string SealPath = "/internal/audit/seals";

    /// <summary>
    /// Matches the camelCase JSON the services exchange. AuditService's response envelope is serialised by
    /// ASP.NET's web defaults, so the reader has to use them too or <c>responseData</c> comes back null.
    /// </summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<string> SealPeriodAsync(SealPeriodRequest request, CancellationToken ct = default)
    {
        var secret = options.Value.HmacSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                $"{AuditMaintenanceClientOptions.SectionName}:HmacSecret is not configured. Without it this "
                + "client would sign with an empty key and every seal would come back 401 — which reads as a "
                + "wrong secret rather than a missing one.");
        }

        var body = JsonSerializer.Serialize(request, Json);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ComputeSignature(secret, timestamp, "POST", SealPath, body);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, SealPath)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("X-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-Signature", signature);

        using var response = await httpClient.SendAsync(httpRequest, ct);

        // Throws on any non-2xx. The activity MUST fail so Temporal retries it: swallowing this would record
        // a seal that does not exist, and the next run seals the next hour, so the hole is permanent.
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<SealEnvelope>(Json, ct);

        if (string.IsNullOrWhiteSpace(envelope?.ResponseData?.SealId))
        {
            throw new InvalidOperationException(
                $"AuditService accepted the seal request for {request.TableName} "
                + $"[{request.PeriodStart:O}, {request.PeriodEnd:O}) but returned no seal id. Treating this "
                + "as a failure so Temporal retries rather than recording a seal that may not exist.");
        }

        return envelope.ResponseData.SealId;
    }

    /// <summary>
    /// Exposed so a test can recompute what the client sent, and because a verifier in another language would
    /// have to reimplement exactly this.
    /// </summary>
    public static string ComputeSignature(
        string secret, long timestamp, string method, string path, string body)
    {
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        var signingString = $"{timestamp}\n{method}\n{path}\n{bodyHash}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signingString));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Just enough of <c>RequestResponse&lt;SealPeriodResponse&gt;</c> to read the id. Declared here rather
    /// than taking a project reference on Venly.Backend.Common purely for one envelope shape.
    /// </summary>
    private sealed record SealEnvelope(SealPeriodResponse? ResponseData);
}
