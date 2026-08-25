namespace Venly.Messaging.Events;

/// <summary>
/// Who performed an audited action. Persisted by AuditService as the native PostgreSQL enum
/// <c>audit_actor_type</c> with labels STAFF, CUSTOMER, SERVICE, SYSTEM.
///
/// <c>Service</c> and <c>System</c> are separate on purpose. Service means a named peer acted on its own
/// behalf — a helper client, the gateway. System means nothing acted: a scheduled job, a migration, a
/// consumer replaying. Collapsing them would make "nobody was responsible" and "another service was
/// responsible" the same row.
/// </summary>
public enum AuditActorType
{
    Staff,
    Customer,
    Service,
    System,
}
