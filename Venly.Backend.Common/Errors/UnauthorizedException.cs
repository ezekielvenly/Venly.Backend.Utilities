using System.Net;

namespace Venly.Backend.Common.Errors;

public sealed class UnauthorizedException : AppException
{
    private UnauthorizedException(string message) : base(HttpStatusCode.Unauthorized, message)
    {
    }

    public static UnauthorizedException MissingClaim(string claimName) =>
        new($"'{claimName}' claim is missing from the token.");

    public static UnauthorizedException InvalidCredentials() =>
        new("The supplied credentials are invalid.");

    public static UnauthorizedException TokenExpired() =>
        new("The token has expired.");
}
