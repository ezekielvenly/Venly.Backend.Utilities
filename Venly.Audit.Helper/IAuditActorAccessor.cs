namespace Venly.Audit.Helper;

public interface IAuditActorAccessor
{
    /// <summary>
    /// The caller behind the current operation. Never throws and never returns null — an unattributable
    /// action is recorded as <c>System</c> with a null id, because an audit row with an honest "nobody known"
    /// is more useful than no row at all.
    /// </summary>
    AuditActor Current();
}
