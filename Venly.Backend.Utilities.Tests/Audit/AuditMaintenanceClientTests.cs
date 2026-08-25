using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Venly.Audit.Helper;
using Venly.Backend.Utilities.Tests.Notification;

namespace Venly.Backend.Utilities.Tests.Audit;

public class AuditMaintenanceClientTests
{
    private static readonly DateTime Hour = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SealPeriodAsync_signs_the_request_and_returns_the_seal_id()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;

        var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            captured = request;
            capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);

            // The envelope AuditService actually returns: RequestResponse<SealPeriodResponse>.
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"responseCode":200,"responseMessage":"Successful","responseData":{"sealId":"ISLABCDEFGHIJKLMNOPQRS"}}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://audit.internal") };
        var options = Options.Create(new AuditMaintenanceClientOptions { HmacSecret = "test-secret" });
        var client = new AuditMaintenanceClient(httpClient, options);

        var sealId = await client.SealPeriodAsync(
            new SealPeriodRequest("audit_entry", Hour, Hour.AddHours(1)));

        Assert.Equal("ISLABCDEFGHIJKLMNOPQRS", sealId);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/internal/audit/seals", captured.RequestUri!.AbsolutePath);

        Assert.True(captured.Headers.TryGetValues("X-Timestamp", out var timestamps));
        Assert.True(captured.Headers.TryGetValues("X-Signature", out var signatures));

        var timestamp = long.Parse(timestamps!.Single());
        var expected = AuditMaintenanceClient.ComputeSignature(
            "test-secret", timestamp, "POST", "/internal/audit/seals", capturedBody!);

        Assert.Equal(expected, signatures!.Single());
    }

    [Fact]
    public async Task SealPeriodAsync_throws_when_the_secret_is_not_configured()
    {
        var handler = new FakeHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://audit.internal") };
        var client = new AuditMaintenanceClient(
            httpClient, Options.Create(new AuditMaintenanceClientOptions()));

        // Signing with an empty secret produces a valid-looking signature that the server rejects, so the
        // failure would surface as a 401 from a misconfiguration nobody would connect to this.
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SealPeriodAsync(
            new SealPeriodRequest("audit_entry", Hour, Hour.AddHours(1))));
    }

    [Fact]
    public async Task A_non_success_status_throws_rather_than_returning_an_empty_seal_id()
    {
        var handler = new FakeHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://audit.internal") };
        var client = new AuditMaintenanceClient(
            httpClient, Options.Create(new AuditMaintenanceClientOptions { HmacSecret = "s" }));

        // The activity must FAIL so Temporal retries it. Returning null or "" would record a successful
        // seal that does not exist, and the next run seals the NEXT hour, leaving a permanent hole.
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.SealPeriodAsync(
            new SealPeriodRequest("audit_entry", Hour, Hour.AddHours(1))));
    }

    [Fact]
    public async Task A_success_status_with_no_seal_id_throws()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"responseCode":200,"responseMessage":"Successful","responseData":null}""",
                    Encoding.UTF8, "application/json"),
            }));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://audit.internal") };
        var client = new AuditMaintenanceClient(
            httpClient, Options.Create(new AuditMaintenanceClientOptions { HmacSecret = "s" }));

        // Same reason as above: a 200 with no id is a failed seal, not a successful one.
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SealPeriodAsync(
            new SealPeriodRequest("audit_entry", Hour, Hour.AddHours(1))));
    }
}
