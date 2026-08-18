using Venly.Messaging.Events;

namespace Venly.Notification.Helper;

public interface INotificationClient
{
    Task SendAsync(NotificationRequested request, CancellationToken ct = default);
}
