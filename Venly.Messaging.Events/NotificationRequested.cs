namespace Venly.Messaging.Events;

/// <param name="CustomerId">
/// Whose preference to consult, and null when there is nobody to consult.
///
/// <para>
/// NotificationService cannot resolve this itself. It holds the notification schema and the customer lives in
/// another one with no foreign key between them, so a destination is just a string to it — which is why the id
/// has to travel on the contract rather than being looked up on arrival.
/// </para>
/// <para>
/// Null means SEND. A recipient with no customer behind them — a payout recipient receiving a tracking SMS, a
/// support address — has no preference and must not be silently suppressed by the absence of one. Every caller
/// that predates preferences passes null and keeps its existing behaviour exactly.
/// </para>
/// </param>
public sealed record NotificationRequested(
    string TemplateCode,
    NotificationChannel Channel,
    string Recipient,
    IReadOnlyDictionary<string, string> Fields,
    string? CustomerId = null);
