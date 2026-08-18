namespace Venly.Messaging.Events;

public sealed record NotificationRequested(
    string TemplateCode,
    NotificationChannel Channel,
    string Recipient,
    IReadOnlyDictionary<string, string> Fields);
