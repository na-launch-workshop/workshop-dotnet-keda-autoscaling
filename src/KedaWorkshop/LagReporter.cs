using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace KedaWorkshop;

/// <summary>Reads committed offsets vs. end offsets so the dashboard can show lag.</summary>
public sealed class LagReporter : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly KafkaSettings _settings;
    private readonly ILogger<LagReporter> _logger;
    private readonly IConsumer<Ignore, Ignore> _consumer;

    public LagReporter(IOptions<KafkaSettings> options, ILogger<LagReporter> logger)
    {
        _settings = options.Value;
        _logger = logger;

        // A consumer that never subscribes: it only issues OffsetFetch / metadata
        // requests, so it never joins the consumer group or steals partitions.
        _consumer = new ConsumerBuilder<Ignore, Ignore>(new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroup,
            EnableAutoCommit = false
        }).Build();
    }

    public Dictionary<string, object> GetStatus()
    {
        var result = new Dictionary<string, object>
        {
            ["topic"] = _settings.Topic,
            ["consumerGroup"] = _settings.ConsumerGroup
        };

        try
        {
            var partitions = Enumerable.Range(0, _settings.PartitionCount)
                .Select(p => new TopicPartition(_settings.Topic, p))
                .ToList();

            var committed = _consumer.Committed(partitions, Timeout);

            long totalLag = 0;
            var lagPerPartition = new Dictionary<string, long>();
            foreach (var tpo in committed)
            {
                var watermarks = _consumer.QueryWatermarkOffsets(tpo.TopicPartition, Timeout);
                var committedOffset = tpo.Offset == Offset.Unset ? 0 : tpo.Offset.Value;
                var lag = Math.Max(0, watermarks.High.Value - committedOffset);
                lagPerPartition[tpo.Partition.Value.ToString()] = lag;
                totalLag += lag;
            }

            result["totalLag"] = totalLag;
            result["lagPerPartition"] = lagPerPartition;
        }
        catch (Exception e)
        {
            _logger.LogWarning("Unable to read lag: {Message}", e.Message);
            result["totalLag"] = -1;
            result["error"] = e.Message;
        }

        return result;
    }

    public void Dispose() => _consumer.Dispose();
}
