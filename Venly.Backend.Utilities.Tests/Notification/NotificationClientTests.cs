using System.Net;
using Microsoft.Extensions.Options;
using Venly.Messaging.Events;
using Venly.Notification.Helper;

namespace Venly.Backend.Utilities.Tests.Notification;

public class NotificationClientTests
{
    [Fact]
    public async Task SendAsync_signs_request_and_posts_to_send_endpoint()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;

        var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            captured = request;
            capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://notification.internal") };
        var options = Options.Create(new NotificationClientOptions { HmacSecret = "test-secret" });
        var client = new NotificationClient(httpClient, options);

        var request = new NotificationRequested(
            "OTP_SIGNUP", NotificationChannel.Email, "ada@example.com",
            new Dictionary<string, string> { ["Code"] = "123456" });

        await client.SendAsync(request);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/api/v1/notifications/send", captured.RequestUri!.AbsolutePath);
        Assert.True(captured.Headers.TryGetValues("X-Timestamp", out var timestamps));
        Assert.True(captured.Headers.TryGetValues("X-Signature", out var signatures));

        var timestamp = long.Parse(timestamps!.Single());
        var expectedSignature = NotificationClient.ComputeSignature(
            "test-secret", timestamp, "POST", "/api/v1/notifications/send", capturedBody!);

        Assert.Equal(expectedSignature, signatures!.Single());
    }

    [Fact]
    public async Task SendAsync_throws_when_secret_not_configured()
    {
        var handler = new FakeHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://notification.internal") };
        var options = Options.Create(new NotificationClientOptions());
        var client = new NotificationClient(httpClient, options);

        var request = new NotificationRequested(
            "OTP_SIGNUP", NotificationChannel.Email, "ada@example.com", new Dictionary<string, string>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
    }
}

internal sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => handler(request, cancellationToken);
}
