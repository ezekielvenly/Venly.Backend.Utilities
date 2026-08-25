using Venly.Messaging.Events;

namespace Venly.Audit.Helper;

/// <summary>
/// Everything about the caller that an audit record needs, resolved once per request.
/// </summary>
public readonly record struct AuditActor(
    AuditActorType Type,
    string? Id,
    IReadOnlyDictionary<string, string?>? PermissionsSnapshot,
    string? SourceIpHash,
    string? CorrelationId);
