using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
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
    IOptions<DemoSettings> demoSettings,
    ILogger<PerformanceBenchmark> logger)
{
   
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
        var avg = times.Average();
        logger.LogInformation("ConcurrentWritesOnly average: {AvgMs}ms", avg);
        return avg;
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
        var avg = times.Average();
        logger.LogInformation("ConcurrentReadsOnly average: {AvgMs}ms", avg);
        return avg;
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

        var avgWrite = writeTasks.Select(t => t.Result).Average();
        var avgRead = readTasks.Select(t => t.Result).Average();
        var degradationPercent = writesOnlyBaselineMs <= 0
            ? 0
            : ((avgWrite - writesOnlyBaselineMs) / writesOnlyBaselineMs) * 100;

        logger.LogWarning("=== Mixed Load Test ===");
        logger.LogWarning("Avg CreateOrder time:  {AvgWrite}ms", Math.Round(avgWrite));
        logger.LogWarning("Avg GetDashboard time: {AvgRead}ms", Math.Round(avgRead));
        logger.LogWarning("Writes-only baseline:  {Baseline}ms", Math.Round(writesOnlyBaselineMs));
        logger.LogWarning("Degradation: +{Deg}%", Math.Round(degradationPercent));

        return new MixedLoadBenchmarkResult
        {
            AvgCreateOrderTimeMs = avgWrite,
            AvgGetDashboardTimeMs = avgRead,
            WritesOnlyBaselineMs = writesOnlyBaselineMs,
            DegradationPercent = degradationPercent
        };
    }

    
    public void PrintIndexImpact(double beforeDashboardMs, double afterDashboardMs, double beforeCreateMs, double afterCreateMs, double beforeMixedWriteMs, double afterMixedWriteMs)
    {
        logger.LogWarning("=== Index Impact ===");
        logger.LogWarning("Before Index    After Index");
        logger.LogWarning("GetDashboard:        {BeforeDashboard}ms          {AfterDashboard}ms", Math.Round(beforeDashboardMs), Math.Round(afterDashboardMs));
        logger.LogWarning("CreateOrder:          {BeforeCreate}ms          {AfterCreate}ms", Math.Round(beforeCreateMs), Math.Round(afterCreateMs));
        logger.LogWarning("Mixed Load Writes:    {BeforeMixed}ms          {AfterMixed}ms", Math.Round(beforeMixedWriteMs), Math.Round(afterMixedWriteMs));
    }

    /// <summary>
    /// Demonstrates read-latency gains from materialized views and the write overhead they introduce.
    /// </summary>
    public void PrintMaterializedViewImpact(
        double rawDashboardMs,
        double materializedDashboardMs,
        double rawCreateMs,
        double materializedCreateMs,
        double rawMixedWriteMs,
        double materializedMixedWriteMs)
    {
        // Materialized views solve read speed
        // But writes pay the refresh cost
        // And reads + writes still compete on the same DB server
        // The architectural problem is still unsolved
        logger.LogWarning("=== Materialized View Impact ===");
        logger.LogWarning("                    Raw Query    Mat. View");
        logger.LogWarning("GetDashboard:         {RawDashboard}ms        {MatDashboard}ms", Math.Round(rawDashboardMs), Math.Round(materializedDashboardMs));
        logger.LogWarning("CreateOrder:           {RawCreate}ms       {MatCreate}ms", Math.Round(rawCreateMs), Math.Round(materializedCreateMs));
        logger.LogWarning("Mixed Load Writes:     {RawMixed}ms       {MatMixed}ms", Math.Round(rawMixedWriteMs), Math.Round(materializedMixedWriteMs));
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
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        await repo.GetDashboardRawAggregationAsync(demoSettings.Value.SimulateSlowQueries, cancellationToken);
    }
}
