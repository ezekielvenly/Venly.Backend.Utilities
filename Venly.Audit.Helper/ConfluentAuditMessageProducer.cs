using System.Reflection;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Venly.Audit.Helper;

/// <summary>
/// The real producer. Singleton — a Kafka producer is expensive to construct, holds its own threads and its
/// own connection pool, and is designed to be shared for the process lifetime.
///
/// <para>The three settings that matter, and why:</para>
/// <list type="bullet">
///   <item><c>EnableIdempotence = true</c> — librdkafka retries on its own, and without idempotence a retry
///   after a partial failure writes the event twice. An audit log that double-counts is worse than one that
///   is a second late.</item>
///   <item><c>Acks = All</c> — the broker acknowledges only once the message is on every in-sync replica.
///   Anything weaker means a leader failover can lose an acknowledged audit event.</item>
///   <item><c>MessageSendMaxRetries</c> high with a bounded <c>MessageTimeoutMs</c> — the queue rides out a
///   broker restart, but a message never sits in it forever pretending it will be delivered.</item>
/// </list>
/// </summary>
public sealed class ConfluentAuditMessageProducer : IAuditMessageProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<ConfluentAuditMessageProducer> _logger;

    public ConfluentAuditMessageProducer(
        IOptions<AuditPublisherOptions> options, ILogger<ConfluentAuditMessageProducer> logger)
    {
        _logger = logger;

        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.BootstrapServers))
        {
            throw new InvalidOperationException(
                $"{AuditPublisherOptions.SectionName}:BootstrapServers is not configured. Set it, or set "
                + $"{AuditPublisherOptions.SectionName}:Enabled to false if this process genuinely must run "
                + "without publishing audit events.");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            ClientId = string.IsNullOrWhiteSpace(settings.ClientId)
                ? Assembly.GetEntryAssembly()?.GetName().Name ?? "venly-audit"
                : settings.ClientId,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 10,
            MessageTimeoutMs = 300_000,
            LingerMs = 20,
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                _logger.LogError("Audit producer error: {Reason} (IsFatal: {IsFatal})", e.Reason, e.IsFatal))
            .Build();
    }

    public void Produce(string topic, string key, string value) =>
        _producer.Produce(topic, new Message<string, string> { Key = key, Value = value }, report =>
        {
            if (report.Error.IsError)
            {
                // The end of the line for this event. Nothing retries past here, so this log line IS the
                // record that an audit event was lost.
                _logger.LogError(
                    "An audit event was NOT delivered to '{Topic}' (key {Key}): {Reason}. It is lost.",
                    topic, key, report.Error.Reason);
            }
        });

    public Task FlushAsync(TimeSpan timeout, CancellationToken ct)
    {
        // Confluent's Flush is synchronous and blocking. Off the caller's thread, because this runs during
        // host shutdown and blocking there stalls every other IHostedService's StopAsync.
        return Task.Run(() =>
        {
            var remaining = _producer.Flush(timeout);

            if (remaining > 0)
            {
                _logger.LogError(
                    "{Remaining} audit event(s) were still queued when the flush timed out after {Timeout}. "
                    + "They are lost.", remaining, timeout);
            }
        }, ct);
    }

    public void Dispose() => _producer.Dispose();
}
