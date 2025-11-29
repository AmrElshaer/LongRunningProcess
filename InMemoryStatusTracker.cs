using System.Collections.Concurrent;

namespace LongRunning.Api;

public class InMemoryStatusTracker : IStatusTracker
{
    private readonly ConcurrentDictionary<string, string> _statuses = new();

    public ValueTask SetStatusAsync(string id, string status)
    {
        _statuses[id] = status;
        return ValueTask.CompletedTask;
    }

    public bool TryGetStatus(string id, out string? status)
    {
        return _statuses.TryGetValue(id, out status);
    }
}