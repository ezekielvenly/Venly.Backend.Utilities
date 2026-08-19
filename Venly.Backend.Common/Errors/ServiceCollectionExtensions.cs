using Microsoft.Extensions.DependencyInjection;

namespace Venly.Backend.Common.Errors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVenlyErrors(this IServiceCollection services)
    {
        services.AddExceptionHandler<AppExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
