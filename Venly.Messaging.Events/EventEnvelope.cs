namespace Venly.Messaging.Events;

/// <summary>
/// The wrapper every event on every SendGram topic carries.
///
/// It exists for the AUDIT ARCHIVE. ERD-audit's event_archive stores event_id, event_type, aggregate_type,
/// aggregate_id and sequence_number for any message on any topic, and a bare payload record supplies none of
/// them — the archiver would have to guess, per topic, forever. Publishing the envelope instead makes those
/// five fields a property of the bus rather than of each consumer's cleverness.
///
/// SequenceNumber is 0 for a producer with no ordering of its own. The archiver replaces it with the Kafka
/// offset, which is a real monotonic sequence per (topic, partition).
/// </summary>
public sealed record EventEnvelope<T>(
    string EventId,
    string EventType,
    string AggregateType,
    string? AggregateId,
    long SequenceNumber,
    DateTimeOffset OccurredAt,
    T Payload);
