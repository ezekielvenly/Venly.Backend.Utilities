namespace Venly.Audit.Helper;

/// <summary>
/// The producer registered when <see cref="AuditPublisherOptions.Enabled"/> is false. It exists because the
/// alternative did not work: <see cref="KafkaAuditPublisher"/> takes an <see cref="IAuditMessageProducer"/> in
/// its constructor, so DI built the Confluent one — and threw on its missing BootstrapServers — even for a
/// process that had explicitly turned publishing off. That is precisely the case the switch is for: a test
/// host, a design-time tool, a service booted only to run migrations.
///
/// Swapping the implementation rather than null-checking at each call site also means a disabled process
/// allocates no librdkafka threads and opens no connections at all.
/// </summary>
public sealed class NullAuditMessageProducer : IAuditMessageProducer
{
    public void Produce(string topic, string key, string value)
    {
        // Deliberately nothing. KafkaAuditPublisher short-circuits before reaching here when disabled; this
        // is the belt to that braces.
    }

    public Task FlushAsync(TimeSpan timeout, CancellationToken ct) => Task.CompletedTask;
}
