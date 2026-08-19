using System.Net;
using Venly.Backend.Common.Errors;

namespace Venly.Backend.Utilities.Tests.Errors;

public class UnauthorizedExceptionTests
{
    [Fact]
    public void MissingClaim_reports_the_claim_name_and_401()
    {
        var exception = UnauthorizedException.MissingClaim("NameIdentifier");

        Assert.Equal(HttpStatusCode.Unauthorized, exception.HttpStatusCode);
        Assert.Contains("NameIdentifier", exception.Message);
    }

    [Fact]
    public void InvalidCredentials_is_a_401()
    {
        var exception = UnauthorizedException.InvalidCredentials();

        Assert.Equal(HttpStatusCode.Unauthorized, exception.HttpStatusCode);
    }

    [Fact]
    public void TokenExpired_is_a_401()
    {
        var exception = UnauthorizedException.TokenExpired();

        Assert.Equal(HttpStatusCode.Unauthorized, exception.HttpStatusCode);
    }
}
