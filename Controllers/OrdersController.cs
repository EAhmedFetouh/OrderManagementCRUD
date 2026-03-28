using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.CRUD.Benchmarks;
using OrderManagement.CRUD.Models;
using OrderManagement.CRUD.Models.DTOs;
using OrderManagement.CRUD.Repositories;

namespace OrderManagement.CRUD.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    IOrderRepository repository,
    IMemoryCache memoryCache,
    IOptions<DemoSettings> demoSettings,
    PerformanceBenchmark benchmark,
    ILogger<OrdersController> logger) : ControllerBase
{
    private const string DashboardCacheKey = "dashboard";

    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await repository.CreateOrderAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailsResponse>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await repository.GetOrderByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<List<CustomerOrderSummaryResponse>>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await repository.GetCustomerOrdersAsync(customerId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("with-cache-check")]
    public async Task<ActionResult<OrderWithDashboardResponse>> CreateOrderWithCacheCheck(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var orderResult = await repository.CreateOrderAsync(request, cancellationToken);
        var dashboard = await GetDashboardWithCacheAsync(cancellationToken);
        return Ok(new OrderWithDashboardResponse
        {
            Order = orderResult,
            Dashboard = dashboard
        });
    }

    [HttpGet("/api/dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await GetDashboardWithCacheAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("/api/benchmarks/run")]
    public async Task<ActionResult<object>> RunBenchmarks(CancellationToken cancellationToken)
    {
        var writeOnly = await benchmark.ConcurrentWritesOnly(cancellationToken);
        var readOnly = await benchmark.ConcurrentReadsOnly(cancellationToken);
        var mixed = await benchmark.MixedLoad(writeOnly, cancellationToken);

        return Ok(new
        {
            ConcurrentWritesOnlyMs = writeOnly,
            ConcurrentReadsOnlyMs = readOnly,
            MixedLoad = mixed
        });
    }

    private async Task<DashboardResponse> GetDashboardWithCacheAsync(CancellationToken cancellationToken)
    {
        var settings = demoSettings.Value;

        if (settings.EnableDashboardCaching &&
            memoryCache.TryGetValue(DashboardCacheKey, out DashboardResponse? cached) &&
            cached is not null)
        {
            logger.LogInformation("CACHE HIT");
            return cached;
        }

        var dashboard = await repository.GetDashboardRawAggregationAsync(settings.SimulateSlowQueries, cancellationToken);

        if (settings.EnableDashboardCaching)
        {
            memoryCache.Set(DashboardCacheKey, dashboard, TimeSpan.FromSeconds(30));
            logger.LogInformation("CACHE MISS");
        }

        return dashboard;
    }
}
