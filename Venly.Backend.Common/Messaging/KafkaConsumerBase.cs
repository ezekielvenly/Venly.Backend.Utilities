using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Venly.Backend.Common.Messaging;

public abstract class KafkaConsumerBase : BackgroundService
{
    private readonly KafkaSettings _settings;
    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly ILogger Logger;

    protected KafkaConsumerBase(
        IServiceScopeFactory scopeFactory,
        KafkaSettings settings,
        ILogger logger)
    {
        ScopeFactory = scopeFactory;
        _settings = settings;
        Logger = logger;
    }

    protected abstract string Topic { get; }

    protected virtual string KafkaGroupId => $"{_settings.ConsumerGroupId}.{Topic}";

    /// <summary>
    /// Where a consumer with no committed offset starts. Latest by default, because that is right for a
    /// consumer that ACTS on messages — a service restarting should not re-send a month of notifications.
    ///
    /// An ARCHIVER wants the opposite: starting at Latest means everything published before its first boot is
    /// missing from the archive permanently, and "the archive is complete except for the bit before we turned
    /// it on" is not an archive. It overrides this to Earliest.
    /// </summary>
    protected virtual AutoOffsetReset OffsetReset => AutoOffsetReset.Latest;

    /// <summary>
    /// The message body. Override this for the common case; override the <see cref="ConsumeResult{TKey,
    /// TValue}"/> overload instead when the Kafka coordinates matter.
    /// </summary>
    protected virtual Task ProcessMessageAsync(string message, CancellationToken stoppingToken) =>
        throw new NotImplementedException(
            $"{GetType().Name} overrides neither ProcessMessageAsync overload.");

    /// <summary>
    /// The message WITH its Kafka coordinates. Override this instead of the string overload when partition,
    /// offset or broker timestamp matter — an offset is the only monotonic sequence number a message has, and
    /// a checkpoint is meaningless without one.
    ///
    /// The default forwards to the string overload, so every existing consumer is unaffected.
    /// </summary>
    protected virtual Task ProcessMessageAsync(
        ConsumeResult<Ignore, string> result, CancellationToken stoppingToken) =>
        ProcessMessageAsync(result.Message.Value, stoppingToken);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = KafkaGroupId,
            AutoOffsetReset = OffsetReset,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetErrorHandler((_, e) =>
                Logger.LogError("Kafka consumer error: {Reason} (IsFatal: {IsFatal})", e.Reason, e.IsFatal))
            .Build();

        consumer.Subscribe(Topic);
        Logger.LogInformation("{Consumer} subscribed to '{Topic}'", GetType().Name, Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<Ignore, string>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);

                if (result?.Message?.Value is null)
                    continue;

                Logger.LogInformation(
                    "{Consumer} received message. Partition: {Partition}, Offset: {Offset}",
                    GetType().Name, result.Partition.Value, result.Offset.Value);

                await ProcessMessageAsync(result, stoppingToken);

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                Logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "{Consumer} handler failed at offset {Offset}; rewinding for redelivery.",
                    GetType().Name, result?.Offset.Value);
                if (result is not null)
                {
                    try
                    {
                        consumer.Seek(result.TopicPartitionOffset);
                    }
                    catch (KafkaException seekEx)
                    {
                        Logger.LogError(seekEx, "{Consumer} seek-back to {Offset} failed.",
                            GetType().Name, result.TopicPartitionOffset);
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        Logger.LogInformation("{Consumer} stopped.", GetType().Name);
    }
}
