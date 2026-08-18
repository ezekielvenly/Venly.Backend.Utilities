using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Venly.Notification.Helper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationClientOptions>(
            configuration.GetSection(NotificationClientOptions.SectionName));

        services.AddHttpClient<INotificationClient, NotificationClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<NotificationClientOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
        });

        return services;
    }
}
