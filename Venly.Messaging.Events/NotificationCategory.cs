namespace Venly.Messaging.Events;

/// <summary>
/// What KIND of message this is, which is the only thing a customer's notification preference acts on.
///
/// <para>
/// Declared here rather than in NotificationService because two sides need it: the TEMPLATE carries the
/// category, and the SEND names the customer whose preference is consulted. A category living in one service
/// would have to be re-expressed as a string on the wire, and a string is where "Marketting" gets silently
/// treated as an unknown category and delivered anyway.
/// </para>
/// <para>
/// Whether a category can be turned off is NOT stored anywhere — see <see cref="IsDisableable"/>. A per-message
/// switch would let someone mark a security notice optional, which is precisely what the requirement forbids.
/// </para>
/// </summary>
public enum NotificationCategory
{
    /// <summary>
    /// Something happened to the customer's money: a transfer initiated, funded, delivered, refunded, held.
    /// Never disableable.
    /// </summary>
    Transactional = 0,

    /// <summary>
    /// Something happened to the customer's account: a lockout, a new credential, a contact change, a sign-in
    /// from somewhere new. Never disableable — these are how a takeover becomes visible to the real owner, so a
    /// setting that suppressed them would be a setting an attacker would look for first.
    /// </summary>
    Security = 1,

    /// <summary>Marketing and product updates. Disableable.</summary>
    Marketing = 2,

    /// <summary>Rate alerts the customer asked for. Disableable.</summary>
    RateAlert = 3,
}

public static class NotificationCategoryRules
{
    /// <summary>
    /// Whether a customer may switch this category off.
    ///
    /// <para>
    /// Derived, and deliberately not a column. The database design carried a <c>disableable</c> flag on the
    /// template, which would make it operator-writable — and an operator who marked a Security template
    /// disableable would hand every customer a switch that turns off the notifications that reveal an account
    /// takeover. The rule belongs to the CATEGORY, where it is a property of what the message is rather than a
    /// property of one row somebody can edit.
    /// </para>
    /// </summary>
    public static bool IsDisableable(this NotificationCategory category) =>
        category is NotificationCategory.Marketing or NotificationCategory.RateAlert;

    /// <summary>Every category, so a settings screen can be rendered without hard-coding the list client-side.</summary>
    public static readonly NotificationCategory[] All =
        Enum.GetValues<NotificationCategory>();
}
