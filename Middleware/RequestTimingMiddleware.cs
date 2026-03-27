using System.Diagnostics;

namespace OrderManagement.CRUD.Middleware;

public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
   
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
