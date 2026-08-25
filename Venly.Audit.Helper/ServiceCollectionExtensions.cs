using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Venly.Audit.Helper;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the audit publisher, the per-request scope and the actor accessor.
    ///
    /// <para>It deliberately does NOT register <see cref="AuditPipelineBehaviour{TRequest, TResponse}"/>:
    /// MediatR behaviours are added inside each service's own <c>AddMediatR</c> callback, and the ORDER
    /// matters — the audit behaviour must come after <c>ValidatePipelineBehaviour</c>. Add this line to the
    /// service's MediatR configuration:</para>
    ///
    /// <code>
    /// services.AddMediatR(config =>
    /// {
    ///     config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
    ///     config.AddOpenBehavior(typeof(ValidatePipelineBehaviour&lt;,&gt;));
    ///     config.AddOpenBehavior(typeof(AuditPipelineBehaviour&lt;,&gt;));
    /// });
    /// </code>
    /// </summary>
    public static IServiceCollection AddVenlyAuditPublisher(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AuditPublisherOptions>(
            configuration.GetSection(AuditPublisherOptions.SectionName));

        // Needed by HttpContextAuditActorAccessor. TryAdd because a web host has usually added it already.
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);

        // Singleton: a Kafka producer holds its own threads and connection pool and is built to be shared.
        //
        // The implementation is chosen HERE rather than null-checked later. KafkaAuditPublisher takes an
        // IAuditMessageProducer in its constructor, so registering the Confluent one unconditionally made DI
        // build it — and throw on its missing BootstrapServers — even in a process that had explicitly set
        // Enabled to false. That is the exact case the switch exists for.
        var enabled = configuration
            .GetSection(AuditPublisherOptions.SectionName)
            .GetValue("Enabled", true);

        if (enabled)
            services.TryAddSingleton<IAuditMessageProducer, ConfluentAuditMessageProducer>();
        else
            services.TryAddSingleton<IAuditMessageProducer, NullAuditMessageProducer>();

        services.TryAddSingleton<IAuditPublisher, KafkaAuditPublisher>();

        // Scoped: one per request, shared between the handler that enriches it and the behaviour that reads it.
        services.TryAddScoped<IAuditScope, AuditScope>();

        services.TryAddSingleton<IAuditActorAccessor, HttpContextAuditActorAccessor>();

        services.AddSingleton<IHostedService, AuditProducerFlushService>();

        return services;
    }
}
