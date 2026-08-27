namespace Venly.Rails.Helper;

/// <summary>
/// PaymentService's rails surface, over HMAC. What WalletService's reconciliation PULLS through.
///
/// <para>Read-only by design. There is no payout here and there must not be: WalletService is the ledger, and a
/// ledger that could instruct a payment could move money without an intent. The instruction direction is the
/// other way round — PaymentService drives WalletService's intents from provider outcomes.</para>
/// </summary>
public interface IRailsClient
{
    /// <summary>
    /// What the provider says it holds. Fed straight into <c>IngestProviderBalances</c>, which is why the
    /// figures are already in minor units and the provider's name comes with them.
    /// </summary>
    Task<RailsBalancesResult> GetBalancesAsync(CancellationToken ct = default);

    Task<List<RailsStatementLineResult>> GetStatementAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<List<RailsBankResult>> GetBanksAsync(
        string currency, string country, CancellationToken ct = default);

    Task<RailsAccountNameResult> ResolveAccountAsync(
        string accountNumber, string bankCode, CancellationToken ct = default);
}
