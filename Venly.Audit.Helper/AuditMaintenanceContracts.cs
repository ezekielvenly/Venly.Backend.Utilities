namespace Venly.Audit.Helper;

/// <summary>
/// The wire contract for <c>POST /internal/audit/seals</c>, shared by AuditService (which serves it) and
/// WorkflowService (which calls it through <see cref="IAuditMaintenanceClient"/>).
///
/// <para>It lives here rather than in either service because a request record defined twice is a contract
/// that can drift on one side only — and this one crosses an HMAC boundary, where a mismatch surfaces as a
/// deserialisation failure inside a Temporal activity rather than as a compile error.</para>
///
/// <para>The period is half-open, <c>[PeriodStart, PeriodEnd)</c>, matching <c>ISealService.SealAsync</c>. An
/// inclusive upper bound would put the row at the boundary into two consecutive seals, so one row would
/// decide two Merkle roots.</para>
/// </summary>
public sealed record SealPeriodRequest(string TableName, DateTime PeriodStart, DateTime PeriodEnd);

/// <summary>
/// The seal's id. A record rather than a bare string so the endpoint can grow a field — a row count, the
/// root — without breaking either side.
/// </summary>
public sealed record SealPeriodResponse(string SealId);
