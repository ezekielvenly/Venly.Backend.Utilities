namespace Venly.Messaging.Events;

/// <summary>
/// The audit topic names, in one place because three things must agree on them: the publisher in
/// Venly.Audit.Helper, AuditService's consumers, and the kafka-init one-shot in docker-compose.yml that
/// creates them. librdkafka has defaulted allow.auto.create.topics to false on the CONSUMER path since 1.6,
/// so a topic nothing has published to yet is a permanent "Unknown topic or partition" retry loop rather than
/// an empty subscription — which is why the compose entry is not optional.
/// </summary>
public static class AuditTopics
{
    public const string EntryRecorded = "audit.entry.recorded";

    public const string DecisionRecorded = "audit.decision.recorded";

    public static readonly string[] All = [EntryRecorded, DecisionRecorded];
}
