namespace Venly.Auth.Helper;

public record TokenPair(string AccessToken, string RefreshToken, int ExpiresIn);
