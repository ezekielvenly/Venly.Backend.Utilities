using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Venly.FeatureFlag.Helper;

namespace Venly.Backend.Utilities.Tests.FeatureFlags;

/// <summary>
/// The behaviour worth pinning is what happens when AdminService is DOWN. A flag client that answers "off"
/// the moment the admin service restarts turns every rollout into an outage; one that throws turns it into a
/// 500. It serves the last snapshot it saw, however old, and only falls back to false when it has never seen
/// one.
/// </summary>
public class FeatureFlagClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Last = request;
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Ok(params FeatureFlagSnapshotEntry[] flags)
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                responseCode = 200,
                responseMessage = "OK",
                responseData = new FeatureFlagSnapshot(flags, DateTime.UtcNow),
                errors = (string[]?)null,
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
    }

    private static FeatureFlagClient Create(StubHandler handler, int cacheSeconds = 30)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://adminservice:5023") };
        var options = Options.Create(new FeatureFlagClientOptions
        {
            BaseUrl = "http://adminservice:5023",
            HmacSecret = "test-secret",
            CacheSeconds = cacheSeconds,
        });

        return new FeatureFlagClient(client, options, NullLogger<FeatureFlagClient>.Instance);
    }

    [Fact]
    public async Task Reads_the_flag_out_of_the_snapshot()
    {
        var handler = new StubHandler(_ => Ok(new FeatureFlagSnapshotEntry("gateway.permission_cache.enabled", true, [])));
        var client = Create(handler);

        Assert.True(await client.IsEnabledAsync("gateway.permission_cache.enabled"));
    }

    [Fact]
    public async Task A_key_the_snapshot_does_not_carry_is_off()
    {
        var handler = new StubHandler(_ => Ok(new FeatureFlagSnapshotEntry("some.other.flag", true, [])));
        var client = Create(handler);

        Assert.False(await client.IsEnabledAsync("gateway.permission_cache.enabled"));
    }

    [Fact]
    public async Task The_snapshot_is_fetched_once_within_the_cache_window()
    {
        var handler = new StubHandler(_ => Ok(new FeatureFlagSnapshotEntry("a.b", true, [])));
        var client = Create(handler);

        await client.IsEnabledAsync("a.b");
        await client.IsEnabledAsync("a.b");
        await client.IsEnabledAsync("a.b");

        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task It_signs_the_request()
    {
        var handler = new StubHandler(_ => Ok(new FeatureFlagSnapshotEntry("a.b", true, [])));
        var client = Create(handler);

        await client.IsEnabledAsync("a.b");

        Assert.NotNull(handler.Last);
        Assert.True(handler.Last!.Headers.Contains("X-Signature"));
        Assert.True(handler.Last.Headers.Contains("X-Timestamp"));
        Assert.True(handler.Last.Headers.Contains("X-Nonce"));
        Assert.Equal("/internal/feature-flag-snapshot", handler.Last.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task A_failure_with_no_snapshot_ever_fetched_answers_false()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = Create(handler);

        Assert.False(await client.IsEnabledAsync("a.b"));
    }

    [Fact]
    public async Task A_failure_after_a_successful_fetch_serves_the_stale_snapshot()
    {
        var fail = false;
        var handler = new StubHandler(_ => fail
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : Ok(new FeatureFlagSnapshotEntry("a.b", true, [])));

        // Zero-second cache, so the second call always attempts a refetch.
        var client = Create(handler, cacheSeconds: 0);

        Assert.True(await client.IsEnabledAsync("a.b"));

        fail = true;
        Assert.True(await client.IsEnabledAsync("a.b"));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task A_thrown_transport_error_is_swallowed()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));
        var client = Create(handler);

        Assert.False(await client.IsEnabledAsync("a.b"));
    }

    [Fact]
    public async Task With_no_secret_configured_it_never_calls_and_answers_false()
    {
        // A misconfigured secret is a deployment mistake, and the log line says so. What it must not do is
        // sign with an empty key and have AdminService reject it as a forgery.
        var handler = new StubHandler(_ => Ok(new FeatureFlagSnapshotEntry("a.b", true, [])));
        var client = new FeatureFlagClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://adminservice:5023") },
            Options.Create(new FeatureFlagClientOptions { BaseUrl = "http://adminservice:5023" }),
            NullLogger<FeatureFlagClient>.Instance);

        Assert.False(await client.IsEnabledAsync("a.b"));
        Assert.Equal(0, handler.Calls);
    }
}
