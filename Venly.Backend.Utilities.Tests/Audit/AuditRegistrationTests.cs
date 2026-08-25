using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Venly.Audit.Helper;

namespace Venly.Backend.Utilities.Tests.Audit;

public class AuditRegistrationTests
{
    private static ServiceProvider Build(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVenlyAuditPublisher(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void The_publisher_resolves()
    {
        using var provider = Build(("AuditPublisher:Enabled", "false"));

        Assert.IsType<KafkaAuditPublisher>(provider.GetRequiredService<IAuditPublisher>());
    }

    [Fact]
    public void The_publisher_is_a_singleton_because_a_kafka_producer_is_expensive()
    {
        using var provider = Build(("AuditPublisher:Enabled", "false"));

        Assert.Same(
            provider.GetRequiredService<IAuditPublisher>(),
            provider.GetRequiredService<IAuditPublisher>());
    }

    [Fact]
    public void The_scope_is_scoped_so_a_handler_and_the_behaviour_share_one()
    {
        using var provider = Build(("AuditPublisher:Enabled", "false"));

        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        Assert.Same(
            first.ServiceProvider.GetRequiredService<IAuditScope>(),
            first.ServiceProvider.GetRequiredService<IAuditScope>());

        Assert.NotSame(
            first.ServiceProvider.GetRequiredService<IAuditScope>(),
            second.ServiceProvider.GetRequiredService<IAuditScope>());
    }

    [Fact]
    public void The_actor_accessor_and_the_flush_service_are_registered()
    {
        using var provider = Build(("AuditPublisher:Enabled", "false"));

        Assert.IsType<HttpContextAuditActorAccessor>(provider.GetRequiredService<IAuditActorAccessor>());
        Assert.Contains(provider.GetServices<IHostedService>(), s => s is AuditProducerFlushService);
    }

    [Fact]
    public void The_behaviour_is_not_registered_here_because_each_service_adds_it_to_its_own_mediatr()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVenlyAuditPublisher(
            new ConfigurationBuilder()
                .AddInMemoryCollection([new KeyValuePair<string, string?>("AuditPublisher:Enabled", "false")])
                .Build());

        // The descriptors, not the resolved services: MS.DI cannot resolve an unbound open generic at all
        // ("Cannot create arrays of open type"), so GetServices(typeof(IPipelineBehavior<,>)) throws rather
        // than returning nothing.
        //
        // MediatR behaviours are registered inside AddMediatR's config callback, which is per-service. This
        // pins that AddVenlyAuditPublisher deliberately does NOT do it, so a service that forgets the
        // AddOpenBehavior line gets a publisher nothing calls rather than a silent double registration.
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void A_disabled_publisher_never_builds_a_kafka_producer()
    {
        using var provider = Build(("AuditPublisher:Enabled", "false"));

        // The whole point of the off switch. Registering the Confluent producer regardless made DI construct
        // it — and throw on its missing BootstrapServers — in exactly the processes that had turned
        // publishing off on purpose.
        Assert.IsType<NullAuditMessageProducer>(provider.GetRequiredService<IAuditMessageProducer>());
    }

    [Fact]
    public void An_enabled_publisher_with_no_broker_configured_fails_loudly_on_first_use()
    {
        using var provider = Build(("AuditPublisher:Enabled", "true"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IAuditMessageProducer>());

        Assert.Contains("BootstrapServers", exception.Message, StringComparison.Ordinal);
    }
}
