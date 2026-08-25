namespace Venly.Audit.Helper;

/// <summary>
/// The service-to-service calls WorkflowService makes into AuditService to run scheduled audit maintenance.
///
/// <para>Only sealing is here. Partition creation, partition detachment and lag refresh are executed by
/// WorkflowService against the <c>audit</c> schema directly — they need no entities and no row hashing, so a
/// round trip would buy nothing. Sealing is different: it depends on <c>RowFingerprint</c> and
/// <c>MerkleTree</c>, and a second copy of those that drifted by one renamed property would make
/// <c>/api/v1/audit/seals/verify</c> report tampering that never happened.</para>
/// </summary>
public interface IAuditMaintenanceClient
{
    /// <summary>
    /// Seals one table for one period and returns the seal id. Idempotent on the server — a period already
    /// sealed returns the existing seal — which is required, because Temporal retries activities.
    /// </summary>
    Task<string> SealPeriodAsync(SealPeriodRequest request, CancellationToken ct = default);
}
