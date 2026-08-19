using System.Net;
using Venly.Backend.Common.Errors;

namespace Venly.Backend.Utilities.Tests.Errors;

public class ForbiddenExceptionTests
{
    [Fact]
    public void InsufficientPermissions_is_a_403()
    {
        var exception = ForbiddenException.InsufficientPermissions();

        Assert.Equal(HttpStatusCode.Forbidden, exception.HttpStatusCode);
    }

    [Fact]
    public void ResourceAccessDenied_reports_the_resource_name_and_403()
    {
        var exception = ForbiddenException.ResourceAccessDenied("NotificationTemplate");

        Assert.Equal(HttpStatusCode.Forbidden, exception.HttpStatusCode);
        Assert.Contains("NotificationTemplate", exception.Message);
    }
}
