namespace Venly.Auth.Helper;

public class AuthClientOptions
{
    public const string SectionName = "AuthClient";

    public string BaseUrl { get; set; } = string.Empty;

    // Must equal AuthService's own HmacSettings:Secret — a caller signs with the target service's secret.
    public string HmacSecret { get; set; } = string.Empty;
}
