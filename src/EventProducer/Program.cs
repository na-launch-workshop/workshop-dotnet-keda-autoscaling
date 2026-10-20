// Floods the keda-events topic so KEDA has a backlog to react to.
// Usage: dotnet run --project src/EventProducer -- [count]

using System.Text.Json;
using Confluent.Kafka;

var count = args.Length > 0 && int.TryParse(args[0], out var parsed) ? parsed : 100;
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
                       ?? "kafka-kafka-bootstrap:9092";
var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "keda-events";

using var producer = new ProducerBuilder<string, string>(
    new ProducerConfig { BootstrapServers = bootstrapServers }).Build();

Console.WriteLine($"Sending {count} messages to topic '{topic}'...");
for (var i = 1; i <= count; i++)
{
    var value = JsonSerializer.Serialize(new { id = i, message = $"work item {i} of {count}" });
    producer.Produce(topic, new Message<string, string>
    {
        Key = Guid.NewGuid().ToString(),
        Value = value
    });
    Console.Write($"\r  Sent {i} / {count}");
}

producer.Flush(TimeSpan.FromSeconds(30));
Console.WriteLine($"\n{count} messages sent. Watch the lag build in Kafka UI.");
