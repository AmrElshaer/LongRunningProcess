namespace LongRunning.Api;

public interface IStatusTracker
{
    ValueTask SetStatusAsync(string id, string status);
    bool TryGetStatus(string id, out string? status);
}