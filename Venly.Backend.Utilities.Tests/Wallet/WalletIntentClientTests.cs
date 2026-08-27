using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Venly.Backend.Utilities.Tests.Notification;
using Venly.Wallet.Helper;

namespace Venly.Backend.Utilities.Tests.Wallet;

/// <summary>
/// The per-movement intent client. What matters here is that it does NOT throw on a non-2xx: a webhook handler
/// has to tell a 409 from a 422 and act differently, and an exception would collapse that into "the call
/// failed".
/// </summary>
public class WalletIntentClientTests
{
    private static WalletIntentClient NewClient(
        FakeHttpMessageHandler handler, string secret = "test-secret") =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://wallet.internal") },
            Options.Create(new WalletClientOptions { HmacSecret = secret }));

    private static FakeHttpMessageHandler Responding(
        HttpStatusCode status,
        string json = "{}",
        Action<HttpRequestMessage, string?>? capture = null) =>
        new(async (request, ct) =>
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            capture?.Invoke(request, body);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });

    [Fact]
    public async Task A_confirm_returning_200_is_a_success()
    {
        var handler = Responding(HttpStatusCode.OK, """{"responseCode":200,"responseMessage":"Successful"}""");

        var result = await NewClient(handler).ConfirmAsync("TINA", "fincra");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsConflict);
    }

    [Fact]
    public async Task A_confirm_returning_409_is_reported_as_a_CONFLICT_and_does_not_throw()
    {
        // What a REPLAYED webhook produces: the intent was already posted. Treating it as success is what makes
        // duplicate delivery harmless, and the handler is what makes that call -- so the client must not throw.
        var handler = Responding(
            HttpStatusCode.Conflict,
            """{"responseCode":409,"responseMessage":"This intent has already been posted."}""");

        var result = await NewClient(handler).ConfirmAsync("TINA", "fincra");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsConflict);
        Assert.Contains("already been posted", result.Message);
    }

    [Fact]
    public async Task A_confirm_returning_422_is_reported_as_UNPROCESSABLE_and_does_not_throw()
    {
        // The reservation expired. The money moved at the provider and the reservation did not survive, which
        // needs a person -- and a person cannot be told if the client threw the same way it does for a replay.
        var handler = Responding(
            HttpStatusCode.UnprocessableEntity,
            """{"responseCode":422,"responseMessage":"This intent's reservation has expired."}""");

        var result = await NewClient(handler).ConfirmAsync("TINA", "fincra");

        Assert.True(result.IsUnprocessable);
        Assert.False(result.IsConflict);
    }

    [Fact]
    public async Task A_fail_passes_the_reason_through_VERBATIM()
    {
        // A customer-facing failure message must be the provider's own, not a paraphrase.
        string? captured = null;
        var handler = Responding(HttpStatusCode.OK, "{}", (_, body) => captured = body);

        await NewClient(handler).FailAsync("TINA", "Beneficiary account name mismatch");

        Assert.Contains("\"reason\":\"Beneficiary account name mismatch\"", captured);
    }

    [Fact]
    public async Task A_lookup_by_reference_returns_the_intent()
    {
        var handler = Responding(HttpStatusCode.OK, """
            {"responseCode":200,"responseData":{
              "intentId":"TINABC","reference":"SGI-ABCDEFGHJKMN","status":"Reserved","kind":"Payout",
              "currency":"GBP","amount":100.00,"destinationCurrency":"NGN","destinationAmount":205000.00,
              "quotedRate":2050}}
            """);

        var intent = await NewClient(handler).FindByReferenceAsync("SGI-ABCDEFGHJKMN");

        Assert.NotNull(intent);
        Assert.Equal("TINABC", intent!.IntentId);
        Assert.Equal(2050m, intent.QuotedRate);
        Assert.Equal("NGN", intent.DestinationCurrency);
    }

    [Fact]
    public async Task A_lookup_that_finds_NOTHING_returns_null_rather_than_throwing()
    {
        // A webhook for a payout this system never made is not an error to retry -- 404 is an ANSWER.
        var handler = Responding(HttpStatusCode.NotFound, """{"responseCode":404}""");

        Assert.Null(await NewClient(handler).FindByReferenceAsync("SGI-NOTHING"));
    }

    [Fact]
    public async Task A_lookup_is_a_GET_on_the_by_reference_path()
    {
        HttpRequestMessage? captured = null;
        var handler = Responding(HttpStatusCode.NotFound, "{}", (request, _) => captured = request);

        await NewClient(handler).FindByReferenceAsync("SGI-ABCDEFGHJKMN");

        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal(
            "/internal/wallet/intents/by-reference/SGI-ABCDEFGHJKMN",
            captured.RequestUri!.AbsolutePath);
    }

    [Fact]
    public void The_reference_is_NOT_url_escaped_and_that_is_deliberate()
    {
        // Uri normalises %2F back to a slash when it builds the request URI, so escaping one here would produce
        // a path with an extra SEGMENT rather than a segment containing a slash -- and WalletService would 404.
        //
        // References come from a 30-symbol alphabet with a fixed prefix, so they contain nothing needing an
        // escape. This pins the reasoning where someone would otherwise reach for EscapeDataString.
        Assert.Equal(
            "/internal/wallet/intents/by-reference/SGI-ABCDEFGHJKMN",
            WalletIntentClient.ByReferencePath("SGI-ABCDEFGHJKMN"));
    }

    [Fact]
    public async Task A_GET_signs_the_EMPTY_body()
    {
        // The same string HmacAuthorizationFilter hashes on its side. Signing "null" or omitting the body hash
        // would fail every GET with a 401 that looked like a wrong secret.
        HttpRequestMessage? captured = null;
        var handler = Responding(HttpStatusCode.NotFound, "{}", (request, _) => captured = request);

        await NewClient(handler).FindByReferenceAsync("SGI-A");

        var timestamp = long.Parse(captured!.Headers.GetValues("X-Timestamp").Single());
        var expected = WalletMaintenanceClient.ComputeSignature(
            "test-secret", timestamp, "GET", WalletIntentClient.ByReferencePath("SGI-A"), string.Empty);

        Assert.Equal(expected, captured.Headers.GetValues("X-Signature").Single());
        Assert.Null(captured.Content);
    }

    [Fact]
    public async Task A_funding_intent_is_shaped_HERE_so_a_handler_cannot_reserve_a_payout()
    {
        string? captured = null;
        var handler = Responding(
            HttpStatusCode.Created,
            """
            {"responseCode":201,"responseData":{"intentId":"TINF","reference":"SGI-F","status":"Reserved",
             "kind":"Funding","currency":"GBP","amount":50.00,"destinationCurrency":"GBP",
             "destinationAmount":50.00,"quotedRate":null}}
            """,
            (_, body) => captured = body);

        var (result, intent) = await NewClient(handler).CreateFundingAsync(
            new CreateFundingIntentRequest("CUSA", "GBP", 50.00m, "FCR-COLLECTION-1", null, "Card top-up"));

        Assert.True(result.IsSuccess);
        Assert.Equal("TINF", intent!.IntentId);
        Assert.Contains("\"kind\":\"Funding\"", captured);
        Assert.Contains("\"idempotencyKey\":\"FCR-COLLECTION-1\"", captured);

        // Same currency both sides: a funding never crosses one.
        Assert.Contains("\"destinationCurrency\":\"GBP\"", captured);
    }

    [Fact]
    public async Task A_realisation_sends_the_EXECUTED_rate()
    {
        string? captured = null;
        var handler = Responding(HttpStatusCode.Created, "{}", (_, body) => captured = body);

        await NewClient(handler).RecordFxRealisationAsync("TINA", 2055.5m, "fincra:conv:1");

        Assert.Contains("\"realisedRate\":2055.5", captured);
        Assert.Contains("\"providerReference\":\"fincra:conv:1\"", captured);
    }

    [Fact]
    public async Task A_missing_secret_throws_rather_than_signing_with_an_empty_key()
    {
        var client = new WalletIntentClient(
            new HttpClient(Responding(HttpStatusCode.OK)) { BaseAddress = new Uri("https://wallet.internal") },
            Options.Create(new WalletClientOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConfirmAsync("TINA", null));
    }

    [Fact]
    public async Task A_NON_JSON_error_body_still_yields_the_status_code()
    {
        // A proxy error page must not lose the status the caller decides on.
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("<html>502</html>", Encoding.UTF8, "text/html"),
            }));

        var result = await NewClient(handler).ConfirmAsync("TINA", null);

        Assert.Equal(502, result.StatusCode);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void The_intent_paths_are_under_internal_wallet()
    {
        Assert.StartsWith("/internal/wallet/", WalletIntentClient.IntentsPath, StringComparison.Ordinal);
        Assert.StartsWith("/internal/wallet/", WalletIntentClient.RealisationsPath, StringComparison.Ordinal);
    }
}
