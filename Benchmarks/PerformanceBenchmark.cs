using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrderManagement.CRUD.Data;
using OrderManagement.CRUD.Models;
using OrderManagement.CRUD.Models.DTOs;
using OrderManagement.CRUD.Repositories;

namespace OrderManagement.CRUD.Benchmarks;

public class MixedLoadBenchmarkResult
{
    public double AvgCreateOrderTimeMs { get; set; }
    public double AvgGetDashboardTimeMs { get; set; }
    public double WritesOnlyBaselineMs { get; set; }
    public double DegradationPercent { get; set; }
}

public class PerformanceBenchmark(
    IServiceScopeFactory scopeFactory,
    AppDbContext dbContext,
    IMemoryCache memoryCache,
    IOptions<DemoSettings> demoSettings,
    ILogger<PerformanceBenchmark> logger)
{
    private const string DashboardCacheKey = "dashboard";
    public async Task<double> ConcurrentWritesOnly(CancellationToken cancellationToken = default)
    {
        var requests = await BuildRequestsAsync(50, cancellationToken);
        var tasks = requests.Select(async req =>
        {
            var sw = Stopwatch.StartNew();
            await ExecuteCreateOrderAsync(req, cancellationToken);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }).ToArray();

        var times = await Task.WhenAll(tasks);
        var avgMs = RoundMs(times.Average());
        logger.LogInformation("ConcurrentWritesOnly average: {AvgMs}ms", avgMs);
        return avgMs;
    }

    public async Task<double> ConcurrentReadsOnly(CancellationToken cancellationToken = default)
    {
        var tasks = Enumerable.Range(0, 50).Select(async _ =>
        {
            var sw = Stopwatch.StartNew();
            await ExecuteGetDashboardAsync(cancellationToken);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }).ToArray();

        var times = await Task.WhenAll(tasks);
        var avgMs = RoundMs(times.Average());
        logger.LogInformation("ConcurrentReadsOnly average: {AvgMs}ms", avgMs);
        return avgMs;
    }

    public async Task<MixedLoadBenchmarkResult> MixedLoad(double writesOnlyBaselineMs, CancellationToken cancellationToken = default)
    {
        var writeRequests = await BuildRequestsAsync(25, cancellationToken);

        var writeTasks = writeRequests.Select(async req =>
        {
            var sw = Stopwatch.StartNew();
            await ExecuteCreateOrderAsync(req, cancellationToken);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }).ToList();

        var readTasks = Enumerable.Range(0, 25).Select(async _ =>
        {
            var sw = Stopwatch.StartNew();
            await ExecuteGetDashboardAsync(cancellationToken);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }).ToList();

        await Task.WhenAll(writeTasks.Concat(readTasks));

        var baselineMs = RoundMs(writesOnlyBaselineMs);
        var avgWriteMs = RoundMs(writeTasks.Select(t => t.Result).Average());
        var avgReadMs = RoundMs(readTasks.Select(t => t.Result).Average());
        var degradationPercent = baselineMs <= 0
            ? 0
            : RoundPercent(((avgWriteMs - baselineMs) / baselineMs) * 100);

        logger.LogWarning("=== Mixed Load Test ===");
        logger.LogWarning("Avg CreateOrder time:  {AvgWrite}ms", avgWriteMs);
        logger.LogWarning("Avg GetDashboard time: {AvgRead}ms", avgReadMs);
        logger.LogWarning("Writes-only baseline:  {Baseline}ms", baselineMs);
        logger.LogWarning("Degradation: +{Deg}%", degradationPercent);

        return new MixedLoadBenchmarkResult
        {
            AvgCreateOrderTimeMs = avgWriteMs,
            AvgGetDashboardTimeMs = avgReadMs,
            WritesOnlyBaselineMs = baselineMs,
            DegradationPercent = degradationPercent
        };
    }

    private async Task<List<CreateOrderRequest>> BuildRequestsAsync(int count, CancellationToken cancellationToken)
    {
        var customerIds = await dbContext.Customers.Select(x => x.Id).Take(1000).ToListAsync(cancellationToken);
        var products = await dbContext.Products.Select(x => new { x.Id, x.StockQuantity }).Take(500).ToListAsync(cancellationToken);
        var random = new Random();

        return Enumerable.Range(0, count).Select(_ =>
        {
            var itemCount = random.Next(1, 4);
            var items = Enumerable.Range(0, itemCount)
                .Select(__ =>
                {
                    var p = products[random.Next(products.Count)];
                    return new CreateOrderItemRequest
                    {
                        ProductId = p.Id,
                        Quantity = random.Next(1, 3)
                    };
                })
                .ToList();

            return new CreateOrderRequest
            {
                CustomerId = customerIds[random.Next(customerIds.Count)],
                Items = items
            };
        }).ToList();
    }

    private async Task ExecuteCreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        await repo.CreateOrderAsync(request, cancellationToken);
    }

    private async Task ExecuteGetDashboardAsync(CancellationToken cancellationToken)
    {
        var settings = demoSettings.Value;

        if (settings.EnableDashboardCaching &&
            memoryCache.TryGetValue(DashboardCacheKey, out DashboardResponse? cached) &&
            cached is not null)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var dashboard = await repo.GetDashboardRawAggregationAsync(settings.SimulateSlowQueries, cancellationToken);

        if (settings.EnableDashboardCaching)
        {
            memoryCache.Set(DashboardCacheKey, dashboard, TimeSpan.FromSeconds(30));
        }
    }

    private static double RoundMs(double milliseconds) =>
        Math.Round(milliseconds, 0, MidpointRounding.AwayFromZero);

    private static double RoundPercent(double percent) =>
        Math.Round(percent, 1, MidpointRounding.AwayFromZero);
}
