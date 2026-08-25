using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Venly.Audit.Helper;
using Venly.Messaging.Events;

namespace Venly.Backend.Utilities.Tests.Audit;

public class KafkaAuditPublisherTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    private static AuditEntryRecorded Entry(
        string? entityId = "CUSABCDEFGHIJKLMNOPQR", string? actorId = "STFABCDEFGHIJKLMNOPQR") =>
        new(AuditActorType.Staff, actorId, null, "customer.status.change", "customer", entityId,
            null, null, null, null, null, null, null, "corr-1", Now);

    private static (KafkaAuditPublisher Publisher, FakeAuditMessageProducer Producer) Build(bool enabled = true)
    {
        var producer = new FakeAuditMessageProducer();
        var options = Options.Create(new AuditPublisherOptions
        {
            BootstrapServers = "localhost:9092",
            Enabled = enabled,
        });

        var publisher = new KafkaAuditPublisher(
            producer, options, new FakeTimeProvider(Now), NullLogger<KafkaAuditPublisher>.Instance);

        return (publisher, producer);
    }

    [Fact]
    public void Publishing_an_entry_sends_an_envelope_to_the_entry_topic()
    {
        var (publisher, producer) = Build();

        publisher.Publish(Entry());

        var message = Assert.Single(producer.Produced);
        Assert.Equal(AuditTopics.EntryRecorded, message.Topic);

        var envelope = JsonSerializer.Deserialize<EventEnvelope<AuditEntryRecorded>>(
            message.Value, KafkaAuditPublisher.JsonOptions);

        Assert.NotNull(envelope);
        Assert.Equal(nameof(AuditEntryRecorded), envelope.EventType);
        Assert.Equal("customer", envelope.AggregateType);
        Assert.Equal("CUSABCDEFGHIJKLMNOPQR", envelope.AggregateId);
        Assert.Equal(Now, envelope.OccurredAt);
        Assert.Equal("customer.status.change", envelope.Payload.ActionType);
        Assert.True(Guid.TryParse(envelope.EventId, out _), "EventId should be a GUID.");
    }

    [Fact]
    public void The_partition_key_is_the_entity_so_one_entity_keeps_its_order()
    {
        var (publisher, producer) = Build();

        publisher.Publish(Entry(entityId: "CUS111"));

        Assert.Equal("CUS111", producer.Produced[0].Key);
    }

    [Fact]
    public void The_partition_key_falls_back_to_the_actor_when_there_is_no_entity()
    {
        var (publisher, producer) = Build();

        publisher.Publish(Entry(entityId: null, actorId: "STF999"));

        Assert.Equal("STF999", producer.Produced[0].Key);
    }

    [Fact]
    public void The_partition_key_falls_back_to_the_event_id_when_there_is_neither()
    {
        var (publisher, producer) = Build();

        publisher.Publish(Entry(entityId: null, actorId: null));

        var envelope = JsonSerializer.Deserialize<EventEnvelope<AuditEntryRecorded>>(
            producer.Produced[0].Value, KafkaAuditPublisher.JsonOptions);

        // Never an empty key: an empty key is a valid Kafka key that buckets every keyless event onto one
        // partition, which is a hot partition rather than the round-robin a null key would give.
        Assert.Equal(envelope!.EventId, producer.Produced[0].Key);
    }

    [Fact]
    public void Publishing_a_decision_sends_to_the_decision_topic_keyed_by_subject()
    {
        var (publisher, producer) = Build();

        publisher.PublishDecision(new AuditDecisionRecorded(
            "kyc.review", "customer", "CUS777", "APPROVED", "Documents verified.",
            "STF1", "STF2", null, null, Now));

        var message = Assert.Single(producer.Produced);
        Assert.Equal(AuditTopics.DecisionRecorded, message.Topic);
        Assert.Equal("CUS777", message.Key);
    }

    [Fact]
    public void A_producer_failure_is_swallowed_because_auditing_must_not_fail_the_action()
    {
        var (publisher, producer) = Build();
        producer.ThrowOnProduce = new InvalidOperationException("broker gone");

        // The action being audited has already succeeded and been committed. Throwing here would turn a
        // successful business operation into a 500 for the caller.
        var exception = Record.Exception(() => publisher.Publish(Entry()));

        Assert.Null(exception);
    }

    [Fact]
    public void Publishing_is_a_no_op_when_the_publisher_is_disabled()
    {
        var (publisher, producer) = Build(enabled: false);

        publisher.Publish(Entry());

        Assert.Empty(producer.Produced);
    }
}

/// <summary>A TimeProvider with a fixed clock, so an asserted timestamp is stable.</summary>
public sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
