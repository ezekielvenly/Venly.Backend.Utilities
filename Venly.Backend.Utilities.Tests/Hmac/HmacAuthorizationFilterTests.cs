using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Venly.Backend.Common.Hmac;

namespace Venly.Backend.Utilities.Tests.Hmac;

public class HmacAuthorizationFilterTests
{
    private const string Secret = "test-secret";
    private const string Path = "/api/v1/notifications/send";
    private const string Body = "{\"templateCode\":\"OTP_SIGNUP\"}";

    [Fact]
    public async Task OnAuthorizationAsync_allows_request_with_valid_signature()
    {
        var context = BuildContext(out var httpContext, Body);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ComputeSignature(Secret, timestamp, "POST", Path, Body);
        httpContext.Request.Headers["X-Timestamp"] = timestamp.ToString();
        httpContext.Request.Headers["X-Signature"] = signature;

        var filter = CreateFilter(Secret);
        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_rejects_request_with_wrong_signature()
    {
        var context = BuildContext(out var httpContext, Body);
        httpContext.Request.Headers["X-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        httpContext.Request.Headers["X-Signature"] = "not-the-right-signature";

        var filter = CreateFilter(Secret);
        await filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task OnAuthorizationAsync_rejects_request_outside_timestamp_tolerance()
    {
        var context = BuildContext(out var httpContext, Body);
        var staleTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var signature = ComputeSignature(Secret, staleTimestamp, "POST", Path, Body);
        httpContext.Request.Headers["X-Timestamp"] = staleTimestamp.ToString();
        httpContext.Request.Headers["X-Signature"] = signature;

        var filter = CreateFilter(Secret);
        await filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task OnAuthorizationAsync_fails_closed_when_secret_not_configured()
    {
        var context = BuildContext(out var httpContext, Body);
        httpContext.Request.Headers["X-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        httpContext.Request.Headers["X-Signature"] = "anything";

        var filter = CreateFilter(secret: "");
        await filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    private static HmacAuthorizationFilter CreateFilter(string secret) =>
        new(
            Options.Create(new HmacOptions { Secret = secret, TimestampToleranceSeconds = 300 }),
            new NoOpHmacNonceStore(),
            NullLogger<HmacAuthorizationFilter>.Instance);

    private static AuthorizationFilterContext BuildContext(out HttpContext httpContext, string body)
    {
        httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = Path;
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static string ComputeSignature(string secret, long timestamp, string method, string path, string body)
    {
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        var signingString = $"{timestamp}\n{method}\n{path}\n{bodyHash}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signingString));
        return Convert.ToBase64String(hash);
    }
}
