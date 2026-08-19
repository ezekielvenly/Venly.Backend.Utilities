using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Venly.Backend.Common.Hmac;

public class HmacAuthorizationFilter(
    IOptions<HmacOptions> options,
    IHmacNonceStore nonceStore,
    ILogger<HmacAuthorizationFilter> logger) : IAsyncAuthorizationFilter
{
    private const string TimestampHeader = "X-Timestamp";
    private const string SignatureHeader = "X-Signature";
    private const string NonceHeader = "X-Nonce";
    private const int MaxNonceLength = 128;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        if (string.IsNullOrWhiteSpace(options.Value.Secret))
        {
            logger.LogError("HMAC request rejected: no signing secret is configured for this service.");
            Reject(context, "Service is not configured for signed requests.");
            return;
        }

        if (!request.Headers.TryGetValue(TimestampHeader, out var timestampValue) ||
            !long.TryParse(timestampValue, out var timestamp))
        {
            Reject(context, $"Missing or invalid '{TimestampHeader}' header.");
            return;
        }

        if (!request.Headers.TryGetValue(SignatureHeader, out var signatureValue) ||
            string.IsNullOrWhiteSpace(signatureValue))
        {
            Reject(context, $"Missing '{SignatureHeader}' header.");
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tolerance = options.Value.TimestampToleranceSeconds;

        if (Math.Abs(now - timestamp) > tolerance)
        {
            logger.LogWarning("HMAC request rejected: timestamp {Timestamp} is outside tolerance window.", timestamp);
            Reject(context, "Request timestamp is outside the allowed window.");
            return;
        }

        request.EnableBuffering();
        request.Body.Position = 0;
        var body = await new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
        request.Body.Position = 0;

        var method = request.Method.ToUpperInvariant();
        var path = request.Path.Value ?? string.Empty;

        string? nonce = request.Headers.TryGetValue(NonceHeader, out var nonceValue)
            ? nonceValue.ToString()
            : null;

        if (!string.IsNullOrWhiteSpace(nonce) && nonce.Length > MaxNonceLength)
        {
            Reject(context, $"'{NonceHeader}' is too long.");
            return;
        }

        var expectedSignature = HmacSignature.Compute(options.Value.Secret, timestamp, method, path, body, nonce);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signatureValue.ToString())))
        {
            logger.LogWarning("HMAC request rejected: signature mismatch for {Method} {Path}.", method, path);
            Reject(context, "Invalid HMAC signature.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(nonce)
            && !await nonceStore.TryReserveAsync(
                nonce!, TimeSpan.FromSeconds(tolerance * 2), context.HttpContext.RequestAborted))
        {
            logger.LogWarning("HMAC request rejected: nonce replayed for {Method} {Path}.", method, path);
            Reject(context, "Request has already been used.");
        }
    }

    private static void Reject(AuthorizationFilterContext context, string message)
    {
        context.Result = new ObjectResult(new RequestResponse<string>
        {
            ResponseCode = StatusCodes.Status401Unauthorized,
            ResponseMessage = message,
        })
        {
            StatusCode = StatusCodes.Status401Unauthorized,
        };
    }
}
