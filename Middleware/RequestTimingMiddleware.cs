using System.Diagnostics;

namespace OrderManagement.CRUD.Middleware;

public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    /// <summary>
    /// Demonstrates cross-cutting request timing visibility to reveal endpoint latency growth over time.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await next(context);
        sw.Stop();

        logger.LogInformation(
            "RequestTiming endpoint={Endpoint} durationMs={DurationMs} timestamp={Timestamp}",
            $"{context.Request.Method} {context.Request.Path}",
            sw.ElapsedMilliseconds,
            DateTimeOffset.UtcNow);
    }
}
