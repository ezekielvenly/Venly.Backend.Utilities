namespace Venly.Wallet.Helper;

/// <summary>
/// What WalletService answered, WITHOUT throwing on a non-2xx.
///
/// <para>Deliberately not <c>EnsureSuccessStatusCode</c>, unlike the maintenance client. A webhook handler has
/// to be able to tell a 409 from a 422 and treat them differently: a 409 on confirm means the intent was ALREADY
/// posted, which is the expected outcome of a duplicate webhook delivery and therefore a success, while a 422
/// means the reservation expired and a human is needed. A client that threw on both would collapse that
/// distinction into "the call failed".</para>
/// </summary>
public sealed record WalletCallResult(int StatusCode, string? Message)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;

    /// <summary>
    /// The intent was already in the requested state. On confirm this is what a REPLAYED webhook produces, and
    /// treating it as success is what makes duplicate delivery harmless.
    /// </summary>
    public bool IsConflict => StatusCode == 409;

    /// <summary>
    /// The movement could not be applied — an expired reservation, most often. The money moved at the provider
    /// and the reservation did not survive, which needs a person.
    /// </summary>
    public bool IsUnprocessable => StatusCode == 422;

    public bool IsNotFound => StatusCode == 404;
}

/// <summary>Enough of an intent to decide what to do with a webhook about it.</summary>
public sealed record WalletIntentLookup(
    string IntentId,
    string Reference,
    string Status,
    string Kind,
    string Currency,
    decimal Amount,
    string DestinationCurrency,
    decimal DestinationAmount,
    decimal? QuotedRate);

/// <param name="IdempotencyKey">
/// The provider's own reference. A retried collection webhook then returns the FIRST intent rather than
/// reserving a second time — which is the whole reason WalletService takes one.
/// </param>
public sealed record CreateFundingIntentRequest(
    string CustomerId,
    string Currency,
    decimal Amount,
    string IdempotencyKey,
    string? CorrelationId,
    string? Narration);
