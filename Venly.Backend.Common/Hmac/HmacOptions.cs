namespace Venly.Backend.Common.Hmac;

public class HmacOptions
{
    public const string SectionName = "HmacSettings";

    public string Secret { get; set; } = string.Empty;

    public int TimestampToleranceSeconds { get; set; } = 300;
}
