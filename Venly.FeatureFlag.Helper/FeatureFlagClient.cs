using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Venly.Backend.Common;
using Venly.Backend.Common.Hmac;

namespace Venly.FeatureFlag.Helper;

/// <summary>
/// Reads AdminService's flag snapshot and answers <see cref="IsEnabledAsync"/> out of memory.
///
/// <para>
/// <b>Registered as a SINGLETON</b>, unlike NotificationClient — the cached snapshot is the point, and a
/// scoped client would refetch on every request. It is registered over a NAMED HttpClient so the underlying
/// handler is still pooled and rotated by IHttpClientFactory.
/// </para>
/// <para>
/// <b>Stale-if-error.</b> A failed refresh keeps the previous snapshot rather than clearing it, and the age is
/// deliberately not capped. The alternative — expiring a stale snapshot into "everything off" — means an
/// AdminService restart silently reverts every rollout in the estate, which is a far larger incident than
/// serving a flag set that is a few minutes behind.
/// </para>
/// </summary>
public sealed class FeatureFlagClient(
    HttpClient httpClient,
    IOptions<FeatureFlagClientOptions> options,
    ILogger<FeatureFlagClient> logger) : IFeatureFlagClient
{
    private const string SnapshotPath = "/internal/feature-flag-snapshot";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // One in-flight refresh at a time. Without it, a cold start under load sends one snapshot request per
    // concurrent caller at the exact moment the service is least able to serve them.
    private readonly SemaphoreSlim _gate = new(1, 1);

    private FeatureFlagSnapshot? _snapshot;
    private DateTime _fetchedAtUtc = DateTime.MinValue;

    public async Task<bool> IsEnabledAsync(
        string key, FeatureFlagContext? context = null, CancellationToken ct = default)
    {
        var snapshot = await GetSnapshotAsync(ct);
        var flag = snapshot?.Flags.FirstOrDefault(f =>
            string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

        return FeatureFlagEvaluator.IsEnabled(flag, context, DateTime.UtcNow);
    }

    public async Task<FeatureFlagSnapshot?> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (!IsStale())
            return _snapshot;

        await _gate.WaitAsync(ct);
        try
        {
            // Re-check inside the gate: whoever was ahead of us has already refreshed it.
            if (!IsStale())
                return _snapshot;

            var fetched = await FetchAsync(ct);

            // A failed fetch leaves _snapshot alone — that is the stale-if-error rule. The timestamp still
            // moves so a hard-down AdminService is retried once per window rather than on every single call.
            _fetchedAtUtc = DateTime.UtcNow;

            if (fetched is not null)
                _snapshot = fetched;

            return _snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsStale() =>
        DateTime.UtcNow - _fetchedAtUtc >= TimeSpan.FromSeconds(Math.Max(0, options.Value.CacheSeconds));

    private async Task<FeatureFlagSnapshot?> FetchAsync(CancellationToken ct)
    {
        var secret = options.Value.HmacSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            // Not attempted rather than signed with an empty key: an unsigned request is rejected downstream
            // as a forgery, which would report a configuration mistake as a security event.
            logger.LogError(
                "FeatureFlagClient:HmacSecret is not configured, so no flag can be resolved. Every flag will "
                + "read as off until it is set.");
            return null;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");

        // Empty body: it is a GET. The signature covers the exact path the receiver sees.
        var signature = HmacSignature.Compute(secret, timestamp, "GET", SnapshotPath, string.Empty, nonce);

        using var request = new HttpRequestMessage(HttpMethod.Get, SnapshotPath);
        request.Headers.TryAddWithoutValidation("X-Timestamp", timestamp.ToString());
        request.Headers.TryAddWithoutValidation("X-Nonce", nonce);
        request.Headers.TryAddWithoutValidation("X-Signature", signature);

        try
        {
            using var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Feature flag snapshot returned {Status}; serving the previous snapshot.",
                    (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var envelope = JsonSerializer.Deserialize<RequestResponse<FeatureFlagSnapshot>>(body, Json);

            if (envelope?.ResponseData is null)
            {
                logger.LogWarning("Feature flag snapshot came back with no body; serving the previous snapshot.");
                return null;
            }

            return envelope.ResponseData;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Never rethrows. A caller asking "is this flag on" is on some other feature's path, and a flag
            // lookup must not be the thing that fails it.
            logger.LogWarning(ex, "Could not fetch the feature flag snapshot; serving the previous snapshot.");
            return null;
        }
    }
}
