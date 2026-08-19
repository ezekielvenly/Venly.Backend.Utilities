using System.Net;

namespace Venly.Backend.Common.Errors;

public sealed class ForbiddenException : AppException
{
    private ForbiddenException(string message) : base(HttpStatusCode.Forbidden, message)
    {
    }

    public static ForbiddenException InsufficientPermissions() =>
        new("You do not have permission to perform this action.");

    public static ForbiddenException ResourceAccessDenied(string resourceName) =>
        new($"Access to '{resourceName}' is denied.");
}
