using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace KedaWorkshop;

/// <summary>Consumes keda-events in the background, slowly, to simulate work.</summary>
public sealed class KafkaEventConsumer(
    IOptions<KafkaSettings> options,
    ILogger<KafkaEventConsumer> logger) : BackgroundService
{
    private readonly KafkaSettings _settings = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Factory.StartNew(
            () => Consume(stoppingToken),
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private void Consume(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_settings.Topic);
        logger.LogInformation("Subscribed to {Topic} as {Group}", _settings.Topic, _settings.ConsumerGroup);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result?.Message is null)
                {
                    continue;
                }

                logger.LogInformation("Processing: {Message}", result.Message.Value);
                Thread.Sleep(_settings.ProcessingDelayMs);
                logger.LogInformation("Completed at {Timestamp}: {Message}",
                    DateTimeOffset.UtcNow, result.Message.Value);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("Consumer closed");
        }
    }
}
