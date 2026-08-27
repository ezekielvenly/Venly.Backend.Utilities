using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Venly.Wallet.Helper;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IWalletMaintenanceClient"/> as a typed HttpClient.
    ///
    /// <para>Only the process that runs wallet maintenance should call this. A service that merely moves money
    /// has no business holding WalletService's HMAC secret — the same separation
    /// <c>AddAuditMaintenanceClient</c> draws.</para>
    /// </summary>
    public static IServiceCollection AddWalletMaintenanceClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<WalletClientOptions>(configuration.GetSection(WalletClientOptions.SectionName));

        services.AddHttpClient<IWalletMaintenanceClient, WalletMaintenanceClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<WalletClientOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl);

            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="IWalletIntentClient"/> as a typed HttpClient, reading the SAME
    /// <see cref="WalletClientOptions"/>.
    ///
    /// <para>A separate call from <see cref="AddWalletMaintenanceClient"/> even though both read one section,
    /// because the two capabilities are separate: a process that moves money per movement has no business
    /// holding the ability to trigger a reconciliation sweep, and a scheduler has no business confirming a
    /// payment. PaymentService calls this one; WorkflowService calls the other.</para>
    /// </summary>
    public static IServiceCollection AddWalletIntentClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<WalletClientOptions>(configuration.GetSection(WalletClientOptions.SectionName));

        services.AddHttpClient<IWalletIntentClient, WalletIntentClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<WalletClientOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl);

            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
        });

        return services;
    }
}
