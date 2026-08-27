namespace Venly.Wallet.Helper;

public sealed class WalletClientOptions
{
    public const string SectionName = "WalletClient";

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// WalletService's own <c>HmacSettings:Secret</c> — the <c>/internal</c> surface is verified against it.
    ///
    /// <para>The SAME value, and getting that wrong is the trap worth naming: every activity comes back 401
    /// from inside a Temporal activity, which reads as "the job failed" with nothing pointing at a credential.
    /// The task times out, retries to exhaustion, and the workflow looks like it did nothing.</para>
    /// </summary>
    public string HmacSecret { get; set; } = string.Empty;

    /// <summary>
    /// 30 seconds. A reconciliation run reads three currencies' positions and their latest snapshots; the
    /// expiry sweep takes row locks on every wallet in its batch. Neither is the sub-second call the
    /// notification client's 10 seconds was sized for.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
