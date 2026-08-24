using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Venly.Database.Helper;

/// <summary>
/// Boot-time schema migration, shared by every DB-backed service in the solution.
///
/// This exists because it was hand-rolled four times and forgotten once. AuthService shipped without its
/// migrate call, so its database was created empty and OpenIddict failed on the first token request with
/// "relation OpenIddictApplications does not exist" — on a schema its InitialCreate migration had been ready
/// to build all along. Four copies of the same six lines is what made that possible, so there is now one copy.
///
/// What it adds over <c>await db.Database.MigrateAsync()</c>:
///
///   - It finds the contexts itself, so registering a new <c>DbContext</c> is the only step. Nothing has to be
///     added here, and nothing can be left out.
///   - It waits for a reachable database instead of dying on the first refused connection.
///   - It says what it did. A skipped migration, a pending list, and an up-to-date schema now look different
///     in the log; before, all three looked like silence.
/// </summary>
public static class AutoMigrationExtensions
{
    /// <summary>
    /// Registers the boot-time migration run. Call it anywhere in service registration — before or after the
    /// <c>AddDbContext</c> calls, it makes no difference (see <see cref="DbContextRegistry"/>).
    /// </summary>
    public static IServiceCollection AddVenlyAutoMigration(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new AutoMigrationOptions();
        configuration.GetSection(AutoMigrationOptions.SectionName).Bind(options);

        services.AddSingleton(options);
        services.AddSingleton(new DbContextRegistry(services));

        return services;
    }

    /// <summary>
    /// Applies every pending migration for every registered <see cref="DbContext"/>, then returns. Call it
    /// after <c>builder.Build()</c> and before <c>app.Run()</c> — and before any seeding, which needs the
    /// tables to exist.
    ///
    /// Throws if a migration fails. That is deliberate: a service running against a half-built schema fails
    /// later, further from the cause, and usually on a customer request rather than on boot.
    /// </summary>
    public static async Task MigrateDatabasesAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        var options = host.Services.GetService<AutoMigrationOptions>()
            ?? throw new InvalidOperationException(
                $"{nameof(MigrateDatabasesAsync)} was called without {nameof(AddVenlyAutoMigration)}. "
                + "Add the registration in the service's DependencyInjection, or drop this call.");

        var registry = host.Services.GetRequiredService<DbContextRegistry>();
        var logger = host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(AutoMigrationExtensions).FullName!);

        var contextTypes = registry.DiscoverContextTypes();

        if (!options.AutoMigrate)
        {
            // Loud on purpose. Turning this off is a deployment choice, and the next person reading a
            // "relation does not exist" error needs to see that the schema was nobody's job this boot.
            logger.LogWarning(
                "Database:AutoMigrate is false — skipping migrations for {ContextCount} context(s): {Contexts}",
                contextTypes.Count,
                string.Join(", ", contextTypes.Select(t => t.Name)));
            return;
        }

        if (contextTypes.Count == 0)
        {
            // Not an error, and not currently reached: the four services that call this all register a
            // context. It stays for the service that references EF Core before it registers anything —
            // DatabaseAutoMigrationConventionTests requires the wiring from the moment the dependency is
            // added, which is deliberately earlier than the first DbContext.
            logger.LogInformation("No DbContext is registered — nothing to migrate.");
            return;
        }

        foreach (var contextType in contextTypes)
        {
            // A scope per context, not one for all of them: DbContext is scoped, and holding several open
            // across a retry loop keeps that many connections busy while the database is still starting.
            using var scope = host.Services.CreateScope();
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);

            await ApplyMigrationsAsync(context, contextType, options, logger, cancellationToken);
        }
    }

    /// <summary>
    /// Applies one context's migrations, retrying only while the database SERVER is unreachable.
    ///
    /// The retry deliberately wraps MigrateAsync rather than gating on a connectivity probe first. The obvious
    /// version of this — check <c>CanConnectAsync</c>, then migrate — is wrong, and wrong in a way that only
    /// shows up on a first boot: <c>CanConnectAsync</c> connects to the TARGET database, so a Postgres that is
    /// up and healthy but does not yet have an <c>authservice</c> database reports false. That is not a
    /// transient state to wait out, it is the state <c>MigrateAsync</c> exists to fix — EF creates the database
    /// if it is missing. Gating on it burned all ten retries against a healthy server and then threw, on a
    /// stack whose migrations were ready to run.
    ///
    /// So the classification is by exception instead: a socket or timeout failure means nothing is listening
    /// yet and is worth another attempt; anything else — bad migration SQL, a permissions problem, a broken
    /// model — fails immediately, because retrying it nine more times only buries the real error.
    /// </summary>
    private static async Task ApplyMigrationsAsync(
        DbContext context,
        Type contextType,
        AutoMigrationOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, options.ConnectRetries);
        var delay = options.InitialConnectDelay;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                // Reading the history table needs the database to exist, which on a first boot it does not.
                // A failure here is therefore not news — it is answered by migrating — so it only decides how
                // much detail the log carries.
                List<string>? pending = null;
                try
                {
                    pending = [.. await context.Database.GetPendingMigrationsAsync(cancellationToken)];
                }
                catch (Exception ex) when (!IsServerUnreachable(ex) && ex is not OperationCanceledException)
                {
                    logger.LogInformation(
                        "{Context}: no migration history yet — the database is most likely absent and will be "
                        + "created now.", contextType.Name);
                    logger.LogDebug(ex, "{Context}: migration history was unreadable.", contextType.Name);
                }

                if (pending is { Count: 0 })
                {
                    logger.LogInformation("{Context}: schema is up to date.", contextType.Name);
                    return;
                }

                if (pending is { Count: > 0 })
                {
                    logger.LogInformation(
                        "{Context}: applying {PendingCount} migration(s): {Migrations}",
                        contextType.Name, pending.Count, string.Join(", ", pending));
                }

                await context.Database.MigrateAsync(cancellationToken);

                logger.LogInformation("{Context}: migrations applied.", contextType.Name);
                return;
            }
            catch (Exception ex) when (
                IsServerUnreachable(ex) && ex is not OperationCanceledException && attempt < attempts)
            {
                logger.LogInformation(
                    "{Context}: database server not reachable (attempt {Attempt} of {Attempts}); "
                    + "retrying in {Delay}.",
                    contextType.Name, attempt, attempts, delay);
                logger.LogDebug(ex, "{Context}: connection attempt {Attempt} failed.", contextType.Name, attempt);

                await Task.Delay(delay, cancellationToken);

                var doubled = delay + delay;
                delay = doubled > options.MaxConnectDelay ? options.MaxConnectDelay : doubled;
            }
        }
    }

    /// <summary>
    /// Whether the exception chain says nothing was listening — the only condition worth retrying.
    ///
    /// Matched on BCL types so this project does not have to reference a database provider: Npgsql surfaces a
    /// refused or unroutable server as a <see cref="SocketException"/> wrapped in its own exception type, and a
    /// server that accepts the socket but never completes the handshake as a timeout. A missing database, by
    /// contrast, arrives as a provider error with no socket failure underneath, and is handled by migrating.
    /// </summary>
    private static bool IsServerUnreachable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException or TimeoutException or IOException)
                return true;
        }

        return false;
    }
}
