using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace LongRunning.Api;

public class InMemoryStatusTracker : IStatusTracker
{
    private readonly ConcurrentDictionary<string, string> _statuses = new();
    private readonly IHubContext<JobStatusHub> _hubContext;

    public InMemoryStatusTracker(IHubContext<JobStatusHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async ValueTask SetStatusAsync(string id, string status)
    {
        _statuses[id] = status;
        
        // Send real-time update to all clients subscribed to this job
        await _hubContext.Clients
            .Group($"job-{id}")
            .SendAsync("StatusUpdate", new 
            { 
                id, 
                status, 
                timestamp = DateTime.UtcNow,
                message = GetStatusMessage(status)
            });
    }

    private static string GetStatusMessage(string status) => status switch
    {
        "queued" => "Your image has been queued for processing",
        "processing" => "Processing image - generating thumbnails and optimizing",
        "completed" => "Processing complete! Your images are ready for download",
        "failed" => "Processing failed. Please try again or contact support",
        _ => $"Status: {status}"
    };

    public bool TryGetStatus(string id, out string? status)
    {
        return _statuses.TryGetValue(id, out status);
    }
}