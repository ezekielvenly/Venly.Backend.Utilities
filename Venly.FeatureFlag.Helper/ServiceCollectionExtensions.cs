using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Venly.FeatureFlag.Helper;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IFeatureFlagClient"/> as a SINGLETON over a NAMED HttpClient.
    ///
    /// <para>
    /// Deliberately not <c>AddHttpClient&lt;IFeatureFlagClient, FeatureFlagClient&gt;</c>, which registers the
    /// implementation as TRANSIENT — every caller would get a client with an empty cache and the snapshot
    /// would be refetched on every single lookup, which is the one thing this class exists to avoid.
    /// </para>
    /// <para>
    /// A named client keeps the pooled, rotating handler <c>IHttpClientFactory</c> provides while letting the
    /// client itself be a singleton. The two lifetimes are independent and both are wanted.
    /// </para>
    /// </summary>
    public static IServiceCollection AddFeatureFlagClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FeatureFlagClientOptions>(
            configuration.GetSection(FeatureFlagClientOptions.SectionName));

        services.AddHttpClient(nameof(FeatureFlagClient), (sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<FeatureFlagClientOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                    client.BaseAddress = new Uri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddSingleton<IFeatureFlagClient>(sp =>
            new FeatureFlagClient(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(FeatureFlagClient)),
                sp.GetRequiredService<IOptions<FeatureFlagClientOptions>>(),
                sp.GetRequiredService<ILogger<FeatureFlagClient>>()));

        return services;
    }
}
