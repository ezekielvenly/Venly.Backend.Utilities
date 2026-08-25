using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Venly.Messaging.Events;

namespace Venly.Audit.Helper;

/// <summary>
/// Wraps a payload in an <see cref="EventEnvelope{T}"/> and hands it to the producer.
///
/// <para><b>Why nothing here is awaited.</b> The alternative — <c>await ProduceAsync</c> — makes every audited
/// action wait for a broker round trip, and turns a broker blip into a failure of the action itself. Instead
/// the message goes into librdkafka's local queue, which retries on its own thread with idempotence enabled,
/// and a genuinely undeliverable message is logged by the delivery-report handler in
/// <see cref="ConfluentAuditMessageProducer"/>. The queue is drained on shutdown.</para>
///
/// <para><b>Why the failure is swallowed.</b> By the time this runs, the action being audited has already
/// succeeded and its transaction has already committed. Rethrowing would convert a completed business
/// operation into a 500 for the caller — losing the action AND the audit record instead of just the audit
/// record.</para>
/// </summary>
public sealed class KafkaAuditPublisher(
    IAuditMessageProducer producer,
    IOptions<AuditPublisherOptions> options,
    TimeProvider time,
    ILogger<KafkaAuditPublisher> logger) : IAuditPublisher
{
    /// <summary>
    /// Shared so AuditService's consumers deserialise with exactly the options the publisher serialised with.
    /// Enums by name, because an ordinal binds the wire format to declaration order.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public void Publish(AuditEntryRecorded entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var eventId = Guid.NewGuid().ToString();

        var envelope = new EventEnvelope<AuditEntryRecorded>(
            EventId: eventId,
            EventType: nameof(AuditEntryRecorded),
            AggregateType: entry.EntityType,
            AggregateId: entry.EntityId,
            SequenceNumber: 0,
            OccurredAt: entry.OccurredAt,
            Payload: entry);

        Send(AuditTopics.EntryRecorded, entry.EntityId ?? entry.ActorId ?? eventId, envelope,
            entry.ActionType);
    }

    public void PublishDecision(AuditDecisionRecorded decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        var eventId = Guid.NewGuid().ToString();

        var envelope = new EventEnvelope<AuditDecisionRecorded>(
            EventId: eventId,
            EventType: nameof(AuditDecisionRecorded),
            AggregateType: decision.SubjectType,
            AggregateId: decision.SubjectId,
            SequenceNumber: 0,
            OccurredAt: decision.DecidedAt,
            Payload: decision);

        Send(AuditTopics.DecisionRecorded, decision.SubjectId ?? eventId, envelope, decision.DecisionType);
    }

    public Task FlushAsync(TimeSpan timeout, CancellationToken ct = default) =>
        options.Value.Enabled ? producer.FlushAsync(timeout, ct) : Task.CompletedTask;

    private void Send<T>(string topic, string key, EventEnvelope<T> envelope, string what)
    {
        if (!options.Value.Enabled)
            return;

        try
        {
            producer.Produce(topic, key, JsonSerializer.Serialize(envelope, JsonOptions));
        }
        catch (Exception ex)
        {
            // Error, not Warning: a dropped audit event is a compliance gap, and the only place it will ever
            // be visible is this line.
            logger.LogError(ex,
                "Could not queue an audit event to '{Topic}'. Action: {What}, EventId: {EventId}. "
                + "The audited action itself succeeded and is unaffected.",
                topic, what, envelope.EventId);
        }
    }

    /// <summary>
    /// Present so the injected <see cref="TimeProvider"/> is used rather than being dead weight in the
    /// constructor: callers that do not carry their own timestamp get the publisher's clock.
    /// </summary>
    public DateTimeOffset Now() => time.GetUtcNow();
}
