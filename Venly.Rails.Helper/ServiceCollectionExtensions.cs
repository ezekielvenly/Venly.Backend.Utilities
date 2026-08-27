using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Venly.Rails.Helper;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IRailsClient"/> as a typed HttpClient.
    ///
    /// <para>Only a process that needs to PULL from the payment provider should call this. WorkflowService does,
    /// so a reconciliation schedule can fetch balances without an operator pushing them; WalletService itself
    /// deliberately does NOT — the ledger never calls a provider, directly or through a hop.</para>
    /// </summary>
    public static IServiceCollection AddRailsClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RailsClientOptions>(configuration.GetSection(RailsClientOptions.SectionName));

        services.AddHttpClient<IRailsClient, RailsClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<RailsClientOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl);

            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
        });

        return services;
    }
}
