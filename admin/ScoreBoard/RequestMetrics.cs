namespace ScoreBoard;

/// <summary>
/// Success/failure of /api/score over a rolling 30-second window, so the success rate
/// recovers within 30 seconds of the surge stopping.
/// </summary>
public sealed class RequestMetrics
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);

    private readonly Queue<(DateTime At, bool Success)> _window = new();
    private readonly object _lock = new();

    public void Record(bool success)
    {
        var now = DateTime.UtcNow;
        lock (_lock)
        {
            _window.Enqueue((now, success));
            Trim(now);
        }
    }

    public object Snapshot()
    {
        int total, ok;
        lock (_lock)
        {
            Trim(DateTime.UtcNow);
            total = _window.Count;
            ok = _window.Count(e => e.Success);
        }

        var rate = total > 0 ? (int)Math.Round(ok * 100.0 / total) : 100;
        return new { total, ok, fail = total - ok, rate };
    }

    private void Trim(DateTime now)
    {
        while (_window.Count > 0 && _window.Peek().At < now - Window)
        {
            _window.Dequeue();
        }
    }
}
