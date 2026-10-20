# .NET

This project uses ASP.NET Core (.NET 8) with
[Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) for Kafka access.

## Layout

| Path | Purpose |
|---|---|
| `src/KedaWorkshop/Program.cs` | Minimal API: wires up the consumer, `/status` and the dashboard |
| `src/KedaWorkshop/KafkaEventConsumer.cs` | `BackgroundService` that consumes `keda-events` slowly |
| `src/KedaWorkshop/LagReporter.cs` | Reads committed vs. end offsets to report consumer lag |
| `src/KedaWorkshop/KafkaSettings.cs` | Bound from the `Kafka` configuration section |
| `src/EventProducer/Program.cs` | Standalone console app that floods the topic |

## Running the application in dev mode

```shell script
dotnet run --project src/KedaWorkshop
```

The dashboard is then served at `http://localhost:8080/` and the raw lag JSON at
`http://localhost:8080/status`.

## Producing a backlog

```shell script
dotnet run --project src/EventProducer -- 3000
```

## Packaging and running the application

```shell script
dotnet publish src/KedaWorkshop -c Release -o publish
dotnet publish/KedaWorkshop.dll
```

## Configuration

Defaults live in `src/KedaWorkshop/appsettings.json` and can be overridden with
double-underscore environment variables:

| Variable | Default |
|---|---|
| `Kafka__BootstrapServers` | `kafka-kafka-bootstrap:9092` |
| `Kafka__Topic` | `keda-events` |
| `Kafka__ConsumerGroup` | `keda-consumer-group` |
| `Kafka__ProcessingDelayMs` | `2500` |
| `Kafka__PartitionCount` | `5` |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080` |

The producer reads `KAFKA_BOOTSTRAP_SERVERS` and `KAFKA_TOPIC` instead, so it matches the
environment the devfile already sets.

## Related Guides

- ASP.NET Core ([guide](https://learn.microsoft.com/aspnet/core/)): Build the application
- Confluent.Kafka ([guide](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)): Publish and consume Kafka records
- KEDA Kafka scaler ([guide](https://keda.sh/docs/latest/scalers/apache-kafka/)): Scale deployments from Kafka lag
