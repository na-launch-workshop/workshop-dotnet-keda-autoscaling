using ScoreBoard;

// At most 12 /api/score requests at once per pod. Under the instructor's 20-worker surge
// this gives a success rate of about 65% on a single pod — enough to see, not so low the
// dashboard looks broken.
var capacity = new SemaphoreSlim(12);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Game>();
builder.Services.AddSingleton<RequestMetrics>();
builder.Services.AddSingleton<PodStatus>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/score", async (Game game, RequestMetrics metrics) =>
{
    if (!capacity.Wait(0))
    {
        // Slow the rejection down too, so a rejected client does not immediately retry.
        await Task.Delay(500);
        metrics.Record(false);
        return Results.Json(new { error = "overloaded" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        var score = game.Snapshot();
        await Task.Delay(400);
        metrics.Record(true);
        return Results.Json(score);
    }
    finally
    {
        capacity.Release();
    }
});

app.MapGet("/api/health", () => Results.Json(new { status = "ok" }));

app.MapGet("/api/metrics", (RequestMetrics metrics) => Results.Json(metrics.Snapshot()));

app.MapGet("/api/status", async (PodStatus status) => Results.Json(await status.GetAsync()));

app.Run();
