namespace Venly.Rails.Helper;

public sealed class RailsClientOptions
{
    public const string SectionName = "RailsClient";

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// PaymentService's own <c>HmacSettings:Secret</c> — the <c>/internal/payment</c> surface is verified against
    /// it.
    ///
    /// <para>Deliberately NOT the same value as <c>WalletClient:HmacSecret</c>: that one authenticates a caller
    /// to WalletService, this one to PaymentService, and a single value serving both would mean anyone able to
    /// forge a call into one could forge a call into the other.</para>
    /// </summary>
    public string HmacSecret { get; set; } = string.Empty;

    /// <summary>
    /// SIXTY seconds, longer than the wallet client's thirty. Every call behind this one reaches an external
    /// payment provider, and PaymentService allows that provider thirty seconds of its own — so a timeout here
    /// shorter than theirs would abandon a request that was still in flight and report a failure the provider
    /// never had.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
