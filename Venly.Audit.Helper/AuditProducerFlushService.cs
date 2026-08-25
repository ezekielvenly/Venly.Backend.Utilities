using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Venly.Audit.Helper;

/// <summary>
/// Drains the producer's local queue on shutdown.
///
/// This is the price of not awaiting each publish. Between <c>Produce</c> returning and the broker
/// acknowledging, the event exists only in this process's memory — so a process that exits without flushing
/// loses every audit event still in flight. A container stop is the common case, not an exotic one.
/// </summary>
public sealed class AuditProducerFlushService(
    IAuditPublisher publisher,
    IOptions<AuditPublisherOptions> options,
    ILogger<AuditProducerFlushService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Max(1, options.Value.FlushTimeoutSeconds));

        try
        {
            await publisher.FlushAsync(timeout, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never rethrow from StopAsync: it would mask whatever is actually stopping the host.
            logger.LogError(ex, "Flushing queued audit events failed during shutdown.");
        }
    }
}
