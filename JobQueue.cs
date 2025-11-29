using System.Threading.Channels;

namespace LongRunning.Api;

public class JobQueue
{
    private readonly Channel<ImageProcessingJob> _channel;

    public JobQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<ImageProcessingJob>(options);
    }

    public ValueTask EnqueueAsync(ImageProcessingJob job, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(job, ct);
    }

    public IAsyncEnumerable<ImageProcessingJob> DequeueAsync(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}