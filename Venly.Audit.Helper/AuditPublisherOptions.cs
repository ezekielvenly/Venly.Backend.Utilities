namespace Venly.Audit.Helper;

public sealed class AuditPublisherOptions
{
    public const string SectionName = "AuditPublisher";

    /// <summary>Comma-separated broker list. Required when <see cref="Enabled"/> is true.</summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>
    /// Off switch for a process that must run without a broker — a test host, a design-time tool, a service
    /// booted to run migrations. Off means events are dropped SILENTLY, so it defaults to on: a service that
    /// forgot to configure a broker should fail loudly at boot, not audit nothing for a month.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public int FlushTimeoutSeconds { get; set; } = 10;

    /// <summary>Identifies this producer in broker logs. Defaults to the entry assembly name when empty.</summary>
    public string ClientId { get; set; } = string.Empty;
}
