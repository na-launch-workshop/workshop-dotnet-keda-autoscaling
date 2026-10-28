namespace ScoreBoard;

/// <summary>Simulated live game state — a team scores every 15 seconds or so.</summary>
public sealed class Game
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(15);

    private readonly object _lock = new();
    private readonly string _pod = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown";
    private int _knicks = 108;
    private int _spurs = 102;
    private DateTime _lastUpdate = DateTime.UtcNow;

    public object Snapshot()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _lastUpdate > TickInterval)
            {
                _lastUpdate = now;
                var points = Random.Shared.Next(1, 4);
                if (Random.Shared.Next(2) == 0) _knicks += points; else _spurs += points;
            }

            return new
            {
                knicks = _knicks,
                spurs = _spurs,
                quarter = 4,
                clock = "3:24",
                period = "Q4",
                pod = _pod
            };
        }
    }
}
