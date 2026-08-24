using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Venly.Database.Helper;

/// <summary>
/// Finds the <see cref="DbContext"/> types a service registered, by reading the service collection back.
///
/// It holds the <see cref="IServiceCollection"/> and reads it on first use rather than snapshotting at
/// registration time, and that is the whole point: <c>AddVenlyAutoMigration</c> can then be called before or
/// after the <c>AddDbContext</c> calls it is meant to find. Snapshotting would have made the order matter, and
/// getting it wrong would produce a silent no-op — the same class of bug as the missing migrate call this
/// mechanism replaces, only harder to see, because a service that migrates nothing logs nothing.
///
/// Reading the collection after <c>Build()</c> is safe: the provider copies the descriptors out, and the
/// builder only makes the collection read-only to further WRITES. Nothing here writes to it.
/// </summary>
public sealed class DbContextRegistry
{
    private readonly IServiceCollection _services;
    private IReadOnlyList<Type>? _cached;

    internal DbContextRegistry(IServiceCollection services) => _services = services;

    /// <summary>
    /// Every concrete <see cref="DbContext"/> registered, ordered by full name so the migration order — and
    /// therefore the log — is the same on every boot.
    /// </summary>
    public IReadOnlyList<Type> DiscoverContextTypes() =>
        _cached ??= [.. _services
            .Select(descriptor => descriptor.ServiceType)
            .Where(IsConcreteDbContext)
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];

    private static bool IsConcreteDbContext(Type type) =>
        type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
        && typeof(DbContext).IsAssignableFrom(type)
        && type != typeof(DbContext);
}
