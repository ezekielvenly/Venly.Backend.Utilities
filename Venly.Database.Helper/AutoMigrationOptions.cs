namespace Venly.Database.Helper;

/// <summary>
/// Settings for the boot-time migration run, bound from the <c>Database</c> configuration section.
/// </summary>
public sealed class AutoMigrationOptions
{
    /// <summary>The configuration section these are bound from.</summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Whether <c>MigrateDatabasesAsync</c> does anything. Defaults to true: every environment this solution
    /// currently runs in wants the schema brought up on boot, and a service that silently skipped its
    /// migrations would fail later on a missing relation rather than here, where the cause is obvious.
    /// Set <c>Database:AutoMigrate=false</c> where a deploy pipeline owns the schema instead.
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// How many times to check for a reachable database before giving up. This covers the database still
    /// starting, not a broken migration: `docker compose up` gates on Postgres' healthcheck, but a host run,
    /// a Kubernetes rollout, or a restarted container has no such gate, and a service that boots a second
    /// before its database crashed on connect.
    /// </summary>
    public int ConnectRetries { get; set; } = 10;

    /// <summary>Delay before the second connect attempt. Doubles each attempt, capped by <see cref="MaxConnectDelay"/>.</summary>
    public TimeSpan InitialConnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Ceiling for the backoff, so ten retries cannot become several minutes of silence.</summary>
    public TimeSpan MaxConnectDelay { get; set; } = TimeSpan.FromSeconds(10);
}
