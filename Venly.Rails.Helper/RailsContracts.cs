namespace Venly.Rails.Helper;

/// <summary>
/// The wire contracts for PaymentService's <c>/internal/payment/rails</c> surface, shared by the service that
/// serves them and WalletService's reconciliation, which pulls through them.
///
/// <para>MINOR UNITS, because that is what the ledger speaks and PaymentService has already converted at the
/// provider boundary. A contract in major units here would put the conversion in two places.</para>
/// </summary>
/// <param name="RollingReserveMinor">
/// Reported and reconciled against NOTHING: SendGram has no account for a rolling reserve. Null means the
/// provider did not say, which is a different fact from zero.
/// </param>
public sealed record RailsBalanceSnapshot(
    string Currency,
    long AvailableMinor,
    long LockedMinor,
    long LedgerMinor,
    long? RollingReserveMinor,
    DateTime AsAt);

/// <param name="Provider">
/// <c>"fincra"</c> or <c>"stub"</c>. Carried through to <c>external_balance_snapshot.Source</c>, so a
/// reconciliation run against the stub is distinguishable from one against the real provider.
/// </param>
public sealed record RailsBalancesResult(string Provider, List<RailsBalanceSnapshot> Balances);

public sealed record RailsStatementLineResult(
    string Reference,
    string Currency,
    long AmountMinor,
    string Direction,
    string? Narration,
    DateTime PostedAt);

public sealed record RailsBankResult(string Code, string Name, string? Type);

public sealed record RailsAccountNameResult(
    string AccountNumber, string BankCode, string AccountHolderName);

public sealed record ResolveAccountRequestBody(string AccountNumber, string BankCode);
