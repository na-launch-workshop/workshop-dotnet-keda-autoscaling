// Floods /api/score with concurrent requests to trigger 503s.
//
// Usage: dotnet Surge.dll [url] [concurrency] [duration_seconds]
//
// Set SURGE_INSECURE=1 to skip TLS verification (e.g. self-signed cluster certs).

var url = args.Length > 0 ? args[0] : "http://localhost:8080/api/score";
var concurrency = args.Length > 1 ? int.Parse(args[1]) : 20;
var duration = TimeSpan.FromSeconds(args.Length > 2 ? int.Parse(args[2]) : 60);

var handler = new HttpClientHandler
{
    // The OpenShift router sets a sticky-session cookie. Ignore it, or every request would
    // land on the same pod no matter how many KEDA adds.
    UseCookies = false
};
if (Environment.GetEnvironmentVariable("SURGE_INSECURE") is { Length: > 0 })
{
    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
}

using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };

Console.WriteLine($"Surging {url} — {concurrency} concurrent workers for {(int)duration.TotalSeconds}s");
Console.WriteLine("Press Ctrl+C to stop early.\n");

using var stop = new CancellationTokenSource(duration);
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

long total = 0, ok = 0;

async Task Worker()
{
    while (!stop.IsCancellationRequested)
    {
        try
        {
            using var response = await http.GetAsync(url, stop.Token);
            if (response.IsSuccessStatusCode) Interlocked.Increment(ref ok);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            // Timeouts and connection errors count as failures.
        }

        Interlocked.Increment(ref total);
    }
}

var workers = Enumerable.Range(0, concurrency).Select(_ => Task.Run(Worker)).ToList();

var started = DateTime.UtcNow;
while (!stop.IsCancellationRequested)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stop.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    var t = Interlocked.Read(ref total);
    var o = Interlocked.Read(ref ok);
    Console.WriteLine($"  {(int)(DateTime.UtcNow - started).TotalSeconds,3}s  total={t,4}  ok={o,4}  errors={t - o,4}");
}

await Task.WhenAll(workers);
Console.WriteLine("\nSurge complete.");
