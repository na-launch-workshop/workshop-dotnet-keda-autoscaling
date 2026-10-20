namespace KedaWorkshop;

public sealed class KafkaSettings
{
    public string BootstrapServers { get; set; } = "kafka-kafka-bootstrap:9092";

    public string Topic { get; set; } = "keda-events";

    public string ConsumerGroup { get; set; } = "keda-consumer-group";

    /// <summary>Partition count of the topic, used when reporting lag.</summary>
    public int PartitionCount { get; set; } = 5;

    /// <summary>Slow processing is deliberate — it is what lets a backlog build up in Kafka.</summary>
    public int ProcessingDelayMs { get; set; } = 2500;
}
