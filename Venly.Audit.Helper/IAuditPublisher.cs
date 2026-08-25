using Venly.Messaging.Events;

namespace Venly.Audit.Helper;

/// <summary>
/// How every service publishes an audit event. There is no HTTP client here and there is not going to be one:
/// an audit write that blocks the action being audited couples the availability of the audit store to the
/// availability of the whole product, and an audit write that is retried by the CALLER is a duplicate waiting
/// to happen. Kafka's local queue and its own retries are the right instrument.
///
/// Both methods return void, which is the contract rather than an oversight — see
/// <see cref="KafkaAuditPublisher"/>.
/// </summary>
public interface IAuditPublisher
{
    void Publish(AuditEntryRecorded entry);

    void PublishDecision(AuditDecisionRecorded decision);

    /// <summary>
    /// Blocks until the local queue drains or the timeout expires. Called once, on shutdown, by
    /// AuditProducerFlushService — never on a request path.
    /// </summary>
    Task FlushAsync(TimeSpan timeout, CancellationToken ct = default);
}
