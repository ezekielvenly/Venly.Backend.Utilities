namespace Venly.FeatureFlag.Helper;

public sealed class FeatureFlagClientOptions
{
    public const string SectionName = "FeatureFlagClient";

    /// <summary>AdminService's base address. The snapshot is service-to-service and does not go through the gateway.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Must equal AdminService's <c>HmacSettings:Secret</c>.</summary>
    public string HmacSecret { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// How long a fetched snapshot is served before another fetch is attempted.
    ///
    /// Thirty seconds is the promise the console makes to the operator ("takes effect within 30 seconds"), so
    /// changing it here means changing the copy there.
    /// </summary>
    public int CacheSeconds { get; set; } = 30;
}
