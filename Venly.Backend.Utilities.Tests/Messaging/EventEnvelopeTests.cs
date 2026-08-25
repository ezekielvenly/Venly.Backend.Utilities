using System.Text.Json;
using System.Text.Json.Serialization;
using Venly.Messaging.Events;

namespace Venly.Backend.Utilities.Tests.Messaging;

public class EventEnvelopeTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public void An_envelope_round_trips_its_payload()
    {
        var entry = new AuditEntryRecorded(
            ActorType: AuditActorType.Staff,
            ActorId: "STFABCDEFGHIJKLMNOPQR",
            EffectivePermissionsSnapshot: new Dictionary<string, string?> { ["checked"] = "staff.account.list" },
            ActionType: "staff.account.list",
            EntityType: "staff_account",
            EntityId: "STFZZZZZZZZZZZZZZZZZZ",
            BeforeValue: null,
            AfterValue: """{"status":"ACTIVE"}""",
            Rationale: null,
            ChangeRequestId: null,
            EmergencyAccessId: null,
            DeniedPermissionKey: null,
            SourceIpHash: "abc123",
            CorrelationId: "corr-1",
            OccurredAt: new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero));

        var envelope = new EventEnvelope<AuditEntryRecorded>(
            EventId: "11111111-1111-1111-1111-111111111111",
            EventType: nameof(AuditEntryRecorded),
            AggregateType: "staff_account",
            AggregateId: "STFZZZZZZZZZZZZZZZZZZ",
            SequenceNumber: 0,
            OccurredAt: entry.OccurredAt,
            Payload: entry);

        var json = JsonSerializer.Serialize(envelope, Options);
        var back = JsonSerializer.Deserialize<EventEnvelope<AuditEntryRecorded>>(json, Options);

        Assert.NotNull(back);
        Assert.Equal("11111111-1111-1111-1111-111111111111", back.EventId);
        Assert.Equal(AuditActorType.Staff, back.Payload.ActorType);
        Assert.Equal("staff.account.list", back.Payload.ActionType);
        Assert.Equal("staff.account.list", back.Payload.EffectivePermissionsSnapshot!["checked"]);
    }

    [Fact]
    public void The_actor_type_serialises_by_name_not_by_ordinal()
    {
        var json = JsonSerializer.Serialize(AuditActorType.Customer, Options);

        // Ordinals bind the wire format to declaration order; a name survives a member being inserted.
        Assert.Equal("\"Customer\"", json);
    }

    [Fact]
    public void The_topic_names_are_the_ones_kafka_init_creates()
    {
        Assert.Equal("audit.entry.recorded", AuditTopics.EntryRecorded);
        Assert.Equal("audit.decision.recorded", AuditTopics.DecisionRecorded);
    }
}
