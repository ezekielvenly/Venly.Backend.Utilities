using Venly.Audit.Helper;

namespace Venly.Backend.Utilities.Tests.Audit;

/// <summary>
/// Records what would have gone to Kafka. Hand-written rather than faked with FakeItEasy because every test
/// below asserts on the captured messages, and a list is easier to read than a call-matcher.
/// </summary>
public sealed class FakeAuditMessageProducer : IAuditMessageProducer
{
    public List<(string Topic, string Key, string Value)> Produced { get; } = [];

    public int FlushCount { get; private set; }

    /// <summary>When set, Produce throws it — the "broker is unreachable" case.</summary>
    public Exception? ThrowOnProduce { get; set; }

    public void Produce(string topic, string key, string value)
    {
        if (ThrowOnProduce is not null)
            throw ThrowOnProduce;

        Produced.Add((topic, key, value));
    }

    public Task FlushAsync(TimeSpan timeout, CancellationToken ct)
    {
        FlushCount++;
        return Task.CompletedTask;
    }
}
