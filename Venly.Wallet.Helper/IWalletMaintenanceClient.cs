namespace Venly.Wallet.Helper;

/// <summary>
/// The SCHEDULED work WorkflowService drives inside WalletService. HMAC, over <c>/internal/wallet/*</c>.
///
/// <para>Deliberately separate from any per-movement intent client. Maintenance is scheduled work and an intent
/// is a movement of somebody's money; one interface carrying both would give WorkflowService the ability to
/// confirm a payment, which is not a capability a scheduler needs and not one worth granting by accident.</para>
/// </summary>
public interface IWalletMaintenanceClient
{
    /// <summary>
    /// Releases reservations past their window. Idempotent and bounded, so a retry costs nothing and a backlog
    /// drains oldest-first rather than stalling the service.
    /// </summary>
    Task<ExpireIntentsResult> ExpireReservationsAsync(int batchSize, CancellationToken ct = default);

    /// <param name="currency">
    /// Null runs every currency. Named, only that one — so an operator can re-run one during an incident.
    /// </param>
    Task<List<ReconciliationRunSummary>> RunReconciliationAsync(
        string? currency, CancellationToken ct = default);

    Task<SampleFxPositionResultBody> SampleFxPositionsAsync(
        SampleFxPositionRequestBody request, CancellationToken ct = default);

    Task<IngestProviderBalancesResultBody> IngestProviderBalancesAsync(
        IngestProviderBalancesRequestBody request, CancellationToken ct = default);

    Task<List<SafeguardingSnapshotSummary>> GenerateSafeguardingSnapshotsAsync(
        CancellationToken ct = default);
}
