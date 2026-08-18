namespace Venly.Backend.Common.Messaging;

public class KafkaSettings
{
    public const string SectionName = "KafkaSettings";

    public string BootstrapServers { get; set; } = string.Empty;
    public string ConsumerGroupId { get; set; } = string.Empty;
}
