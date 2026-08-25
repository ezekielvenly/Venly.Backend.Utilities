namespace Venly.Audit.Helper;

/// <summary>
/// Implemented by a MediatR command to opt into auditing. <see cref="AuditPipelineBehaviour{TRequest,
/// TResponse}"/> publishes an audit event after the handler succeeds and does nothing for a command that does
/// not implement this.
///
/// <para>Opt-in rather than opt-out, and the trade-off is deliberate: opt-out audits everything by default and
/// then quietly stops the moment someone adds an exclusion, while opt-in makes "this action is audited" a
/// visible property of the command's declaration. The cost is that a new command has to remember — which is
/// why the entity type and action type live here, on the command, and not in a registry somewhere else.</para>
///
/// <para>Everything a command cannot know up front — the id of a row it is about to create, the state before
/// it changed, the reason the caller gave — is filled in by the handler through
/// <see cref="IAuditScope"/>.</para>
/// </summary>
public interface IAuditable
{
    /// <summary>Dotted and past-tense-neutral, e.g. <c>staff.account.invite</c>. Free text: ERD-audit's
    /// action_type is a text column, because the set of actions grows with every service.</summary>
    string AuditActionType { get; }

    /// <summary>The kind of thing acted on, in snake_case, e.g. <c>staff_account</c>.</summary>
    string AuditEntityType { get; }

    /// <summary>
    /// The id of the thing acted on, when the command already knows it. Null for a create — the handler sets
    /// it through <see cref="IAuditScope.SetEntityId"/> once the row exists.
    /// </summary>
    string? AuditEntityId { get; }
}
