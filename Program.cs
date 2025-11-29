using LongRunning.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register application services
builder.Services.AddSingleton<IStatusTracker, InMemoryStatusTracker>();
builder.Services.AddSingleton<JobQueue>();
builder.Services.AddHostedService<ImageProcessor>();
builder.Services.AddAntiforgery();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapPost("/upload-image", async (IFormFile? file, IWebHostEnvironment env, IStatusTracker statusTracker, JobQueue jobQueue, CancellationToken ct) =>
{
    if (file is null) return Results.BadRequest("No file uploaded.");
    if (file.Length == 0) return Results.BadRequest("Empty file.");

    // Phase 1: Accept the work and store original
    var id = Guid.NewGuid().ToString("N");
    var folderPath = Path.Combine(env.ContentRootPath, "uploads", id);
    Directory.CreateDirectory(folderPath);
    var fileName = $"{id}{Path.GetExtension(file.FileName)}";
    var originalPath = Path.Combine(folderPath, fileName);

    await using (var fs = System.IO.File.Create(originalPath))
    {
        await file.CopyToAsync(fs, ct);
    }

    // Mark queued status
    await statusTracker.SetStatusAsync(id, "queued");

    // Enqueue job for phase 2
    var job = new ImageProcessingJob(id, originalPath, folderPath);
    await jobQueue.EnqueueAsync(job, ct);

    // Return 202 Accepted with Location header to check status
    return Results.AcceptedAtRoute("GetStatus", new { id }, new { id, status = "queued" });
})
.DisableAntiforgery()
.WithName("UploadImage");

app.MapGet("/status/{id}", (string id, IStatusTracker statusTracker, HttpContext httpContext) =>
{
    if (!statusTracker.TryGetStatus(id, out var status))
    {
        return Results.NotFound(new { id, status = "not found" });
    }

    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    
    if (status == "completed")
    {
        var response = new
        {
            id,
            status,
            links = new Dictionary<string, string>
            {
                ["original"] = $"{baseUrl}/images/{id}/original",
                ["thumbnail_128"] = $"{baseUrl}/images/{id}/thumb_128.jpg",
                ["thumbnail_256"] = $"{baseUrl}/images/{id}/thumb_256.jpg",
                ["thumbnail_512"] = $"{baseUrl}/images/{id}/thumb_512.jpg",
                ["optimized"] = $"{baseUrl}/images/{id}/optimized.jpg"
            }
        };
        return Results.Ok(response);
    }
    
    return Results.Ok(new { id, status });
})
.WithName("GetStatus");

// Serve processed images
app.MapGet("/images/{id}/{filename}", (string id, string filename, IWebHostEnvironment env) =>
{
    var filePath = Path.Combine(env.ContentRootPath, "uploads", id, filename);
    
    if (!File.Exists(filePath))
    {
        return Results.NotFound();
    }
    
    return Results.File(filePath, "image/jpeg");
})
.WithName("GetImage");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}