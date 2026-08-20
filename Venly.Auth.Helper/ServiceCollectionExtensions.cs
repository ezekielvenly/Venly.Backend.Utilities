using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Venly.Auth.Helper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthClientOptions>(configuration.GetSection(AuthClientOptions.SectionName));

        services.AddHttpClient<IAuthClient, AuthClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<AuthClientOptions>>().Value;

            // AuthClient signs the literal relative path "/connect/token", so the base address must carry
            // scheme/host/port and nothing else — a path prefix makes the signed and requested paths disagree
            // and every assertion comes back 401.
            if (string.IsNullOrWhiteSpace(opts.BaseUrl))
                throw new InvalidOperationException("AuthClient:BaseUrl is not configured.");
            client.BaseAddress = new Uri(opts.BaseUrl);
        });

        return services;
    }
}
