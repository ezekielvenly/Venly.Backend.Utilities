namespace Venly.Notification.Helper;

public sealed class NotificationClientOptions
{
    public const string SectionName = "NotificationClient";

    public string BaseUrl { get; set; } = string.Empty;
    public string HmacSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
}
