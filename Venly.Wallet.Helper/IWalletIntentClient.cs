namespace Venly.Wallet.Helper;

/// <summary>
/// The PER-MOVEMENT intent calls, over HMAC.
///
/// <para>Separate from <see cref="IWalletMaintenanceClient"/> on purpose, and the separation is a capability
/// boundary rather than tidiness: maintenance is scheduled work, an intent is somebody's money, and one
/// interface carrying both would give the scheduler the ability to confirm a payment. WorkflowService holds the
/// maintenance client; PaymentService holds this one.</para>
/// </summary>
public interface IWalletIntentClient
{
    /// <summary>
    /// Finds an intent by the reference the provider echoed back as <c>customerReference</c>. Null when no
    /// movement in this system carries it — a webhook for a payout we never made is not an error.
    /// </summary>
    Task<WalletIntentLookup?> FindByReferenceAsync(string reference, CancellationToken ct = default);

    /// <summary>
    /// Posts the movement. A 409 means it was already posted, which is what a duplicate delivery produces.
    /// </summary>
    Task<WalletCallResult> ConfirmAsync(
        string intentId, string? postedBy, CancellationToken ct = default);

    /// <param name="reason">
    /// The provider's OWN message, passed through verbatim. A customer-facing failure reason must be what the
    /// provider said and not a paraphrase of it.
    /// </param>
    Task<WalletCallResult> FailAsync(string intentId, string reason, CancellationToken ct = default);

    /// <summary>Reserves a funding intent for an inbound collection.</summary>
    Task<(WalletCallResult Result, WalletIntentLookup? Intent)> CreateFundingAsync(
        CreateFundingIntentRequest request, CancellationToken ct = default);

    /// <param name="realisedRate">
    /// What the provider ACTUALLY executed at, in destination units per one source unit. Not the quote.
    /// </param>
    Task<WalletCallResult> RecordFxRealisationAsync(
        string intentId, decimal realisedRate, string? providerReference, CancellationToken ct = default);
}
