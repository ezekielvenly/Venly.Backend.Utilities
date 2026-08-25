namespace Venly.Audit.Helper;

public sealed class AuditMaintenanceClientOptions
{
    public const string SectionName = "AuditMaintenanceClient";

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>AuditService's own <c>HmacSettings:Secret</c> — the endpoint is verified against it.</summary>
    public string HmacSecret { get; set; } = string.Empty;

    /// <summary>
    /// 30 seconds, not the 10 the notification client uses. Sealing re-reads and hashes an hour of audit
    /// rows, and a large hour is legitimately slow — the workflow already allows the activity 30 minutes.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
