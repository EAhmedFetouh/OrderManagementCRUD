using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.CRUD.Models;
using OrderManagement.CRUD.Models.DTOs;
using OrderManagement.CRUD.Repositories;

namespace OrderManagement.CRUD.Services;

public interface IOrderService
{
    /// <summary>
    /// Demonstrates how a simple write workflow grows in latency under concurrent pressure.
    /// </summary>
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Demonstrates read complexity from multi-join detail retrieval over large datasets.
    /// </summary>
    Task<OrderDetailsResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Demonstrates list query slowdown in a CRUD model when customer history grows.
    /// </summary>
    Task<List<CustomerOrderSummaryResponse>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Demonstrates expensive aggregate reporting and optional read caching.
    /// </summary>
    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Demonstrates stale read behavior when write and cached read occur back to back.
    /// </summary>
    Task<OrderWithDashboardResponse> CreateOrderAndReadCachedDashboardAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Demonstrates side-by-side comparison between raw aggregations and materialized view reads.
    /// </summary>
    Task<object> CompareDashboardQueriesAsync(CancellationToken cancellationToken);
}

public class OrderService(
    IOrderRepository repository,
    ILogger<OrderService> logger,
    IMemoryCache memoryCache,
    IOptions<DemoSettings> demoSettingsOptions) : IOrderService
{
    private readonly DemoSettings _demoSettings = demoSettingsOptions.Value;

    /// <inheritdoc />
    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await repository.CreateOrderAsync(request, cancellationToken);
        sw.Stop();
        logger.LogInformation("CreateOrder completed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return result;
    }

    /// <inheritdoc />
    public async Task<OrderDetailsResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await repository.GetOrderByIdAsync(orderId, cancellationToken);
        sw.Stop();
        logger.LogInformation("GetOrderById completed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return result;
    }

    /// <inheritdoc />
    public async Task<List<CustomerOrderSummaryResponse>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await repository.GetCustomerOrdersAsync(customerId, cancellationToken);
        sw.Stop();
        logger.LogInformation("GetCustomerOrders completed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return result;
    }

    /// <inheritdoc />
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        if (_demoSettings.EnableDashboardCaching &&
            memoryCache.TryGetValue("dashboard", out DashboardResponse? cached) &&
            cached is not null)
        {
            logger.LogInformation("CACHE HIT - 2ms");
            return cached;
        }

        var sw = Stopwatch.StartNew();
        var dashboard = await repository.GetDashboardFromMaterializedViewAsync(_demoSettings.SimulateSlowQueries, cancellationToken);
        sw.Stop();

        if (_demoSettings.EnableDashboardCaching)
        {
            memoryCache.Set("dashboard", dashboard, TimeSpan.FromSeconds(30));
            logger.LogInformation("CACHE MISS - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }

        logger.LogInformation("MATERIALIZED VIEW - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        logger.LogInformation("GetDashboard completed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return dashboard;
    }

    /// <inheritdoc />
    public async Task<OrderWithDashboardResponse> CreateOrderAndReadCachedDashboardAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Cache solves read speed but introduces stale data
        // Cache invalidation on every write defeats the purpose
        // We still haven't solved the root problem: one DB for reads and writes
        var orderResult = await CreateOrderAsync(request, cancellationToken);
        var dashboard = await GetDashboardAsync(cancellationToken);

        return new OrderWithDashboardResponse
        {
            Order = orderResult,
            Dashboard = dashboard
        };
    }

    /// <inheritdoc />
    public async Task<object> CompareDashboardQueriesAsync(CancellationToken cancellationToken)
    {
        var rawSw = Stopwatch.StartNew();
        var raw = await repository.GetDashboardRawAggregationAsync(_demoSettings.SimulateSlowQueries, cancellationToken);
        rawSw.Stop();

        var materializedSw = Stopwatch.StartNew();
        var materialized = await repository.GetDashboardFromMaterializedViewAsync(_demoSettings.SimulateSlowQueries, cancellationToken);
        materializedSw.Stop();

        var improvement = rawSw.ElapsedMilliseconds == 0
            ? 0
            : (1 - (materializedSw.Elapsed.TotalMilliseconds / rawSw.Elapsed.TotalMilliseconds)) * 100;

        // Materialized view is faster than raw aggregation
        // BUT: SQL Server refreshes it synchronously on every write
        // Heavy writes will still cause contention
        // AND: you can't control WHEN it refreshes - unlike our manual approach in Project 3
        // This is why manual denormalization gives us more control
        return new
        {
            rawQuery = new
            {
                data = raw,
                executionMs = rawSw.ElapsedMilliseconds
            },
            materializedView = new
            {
                data = materialized,
                executionMs = materializedSw.ElapsedMilliseconds
            },
            improvement = $"{Math.Round(improvement)}% faster"
        };
    }
}
