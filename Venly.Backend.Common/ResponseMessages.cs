namespace Venly.Backend.Common;

public static class ResponseMessages
{
    public const string Success = "Successful";
    public const string ValidationError = "A validation error has occurred, check error list for more info.";

    public static string GetSuccessMessage() => Success;
    public static string GetValidationMessage() => ValidationError;
    public static string GetBadRequestMessage(string detail) => $"Bad request: {detail}";
}
