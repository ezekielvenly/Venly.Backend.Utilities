using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Venly.Backend.Common.Hmac;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HmacAuthorizeAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<HmacAuthorizationFilter>();
}
