using Venly.Messaging.Events;

namespace Venly.Backend.Utilities.Tests.Messaging;

public class NotificationRequestedTests
{
    [Fact]
    public void Records_with_same_values_are_equal()
    {
        var fields = new Dictionary<string, string> { ["FirstName"] = "Ada" };

        var a = new NotificationRequested("OTP_SIGNUP", NotificationChannel.Email, "ada@example.com", fields);
        var b = new NotificationRequested("OTP_SIGNUP", NotificationChannel.Email, "ada@example.com", fields);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Records_with_different_recipients_are_not_equal()
    {
        var fields = new Dictionary<string, string>();

        var a = new NotificationRequested("OTP_SIGNUP", NotificationChannel.Email, "ada@example.com", fields);
        var b = new NotificationRequested("OTP_SIGNUP", NotificationChannel.Email, "grace@example.com", fields);

        Assert.NotEqual(a, b);
    }
}
