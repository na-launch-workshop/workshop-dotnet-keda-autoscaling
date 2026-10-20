using KedaWorkshop;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<LagReporter>();
builder.Services.AddHostedService<KafkaEventConsumer>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/status", (LagReporter reporter) => Results.Json(reporter.GetStatus()));

app.Run();
