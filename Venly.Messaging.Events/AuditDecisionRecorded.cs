namespace Venly.Messaging.Events;

/// <summary>
/// One row of ERD-audit's <c>decision_record</c>: a human judgement with an outcome and a reason — a change
/// request approved, a KYC case cleared, a payout held. Distinct from <see cref="AuditEntryRecorded"/>, which
/// records that something HAPPENED; this records that someone DECIDED.
/// </summary>
public sealed record AuditDecisionRecorded(
    string DecisionType,
    string SubjectType,
    string SubjectId,
    string Outcome,
    string? Rationale,
    string? RaisedByStaffId,
    string? DecidedByStaffId,
    string? EvidenceSnapshot,
    string? ListVersion,
    DateTimeOffset DecidedAt);
