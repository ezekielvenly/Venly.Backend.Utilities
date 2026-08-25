namespace Venly.Audit.Helper;

/// <summary>
/// The seam between <see cref="KafkaAuditPublisher"/> and Confluent's client. It exists so the publisher's
/// envelope construction, key selection and failure swallowing can be tested without a broker — every one of
/// those is a decision worth pinning, and none of them needs Kafka to be running.
/// </summary>
public interface IAuditMessageProducer
{
    /// <summary>
    /// Hands a message to the producer's local queue and returns immediately. Delivery — including retries —
    /// happens on the producer's own thread. Throws only if the message cannot be QUEUED (a full queue, a
    /// disposed producer); a delivery failure surfaces through the delivery-report handler instead.
    /// </summary>
    void Produce(string topic, string key, string value);

    Task FlushAsync(TimeSpan timeout, CancellationToken ct);
}
