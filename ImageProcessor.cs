namespace LongRunning.Api;

public class ImageProcessor(IStatusTracker statusTracker, JobQueue jobQueue, ILogger<ImageProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var job in jobQueue.DequeueAsync(ct))
        {
            try
            {
                await  Task.Delay(2000, ct);
                await statusTracker.SetStatusAsync(
                    job.Id,
                    "processing"
                );

                // Generate thumbnails
                await GenerateThumbnailsAsync(
                    job.OriginalPath,
                    job.OutputFolder
                );
                await  Task.Delay(2000, ct);
                await statusTracker.SetStatusAsync(
                    job.Id,
                    "thumbnails_generated"
                );
                // Optimize images
                await OptimizeImagesAsync(
                    job.OriginalPath,
                    job.OutputFolder
                );
                await Task.Delay(2000, ct);
                await statusTracker.SetStatusAsync(
                    job.Id,
                    "completed"
                );
            }
            catch (Exception ex)
            {
                await statusTracker.SetStatusAsync(
                    job.Id,
                    "failed"
                );

                logger.LogError(ex, "Failed to process image {Id}", job.Id);
            }
        }
    }

    private async Task GenerateThumbnailsAsync(string originalPath, string outputFolder)
    {
        // Simulate thumbnail generation
        await Task.Delay(1000); // Simulate processing time
        
        var thumbnailSizes = new[] { 128, 256, 512 };
        
        foreach (var size in thumbnailSizes)
        {
            var thumbnailPath = Path.Combine(outputFolder, $"thumb_{size}.jpg");
            
            // In a real implementation, you would use an image processing library like:
            // - SixLabors.ImageSharp
            // - SkiaSharp
            // - System.Drawing (Windows only)
            // For now, just simulate by copying the file
            File.Copy(originalPath, thumbnailPath, overwrite: true);
            
            logger.LogInformation("Generated {Size}px thumbnail at {Path}", size, thumbnailPath);
        }
    }

    private async Task OptimizeImagesAsync(string originalPath, string outputFolder)
    {
        // Simulate image optimization
        await Task.Delay(1000); // Simulate processing time
        
        var optimizedPath = Path.Combine(outputFolder, "optimized.jpg");
        
        // In a real implementation, you would:
        // - Compress the image
        // - Strip metadata
        // - Convert to optimal format
        // For now, just simulate by copying the file
        File.Copy(originalPath, optimizedPath, overwrite: true);
        
        logger.LogInformation("Optimized image at {Path}", optimizedPath);
    }
}