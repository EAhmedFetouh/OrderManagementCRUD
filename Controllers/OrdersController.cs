using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.CRUD.Benchmarks;
using OrderManagement.CRUD.Models.DTOs;
using OrderManagement.CRUD.Services;

namespace OrderManagement.CRUD.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    IOrderService orderService,
    PerformanceBenchmark benchmark,
    ILogger<OrdersController> logger) : ControllerBase
{
    /// <summary>
    /// Demonstrates stock validation and multi-entity transactional writes in a CRUD service.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.CreateOrderAsync(request, cancellationToken);
        sw.Stop();
        logger.LogInformation("POST /api/orders executed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates expensive read orchestration from multiple table joins for one API response.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailsResponse>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.GetOrderByIdAsync(id, cancellationToken);
        sw.Stop();
        logger.LogInformation("GET /api/orders/{OrderId} executed in {ElapsedMs} ms", id, sw.ElapsedMilliseconds);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Demonstrates read growth pain for customer history queries in unindexed CRUD tables.
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<List<CustomerOrderSummaryResponse>>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.GetCustomerOrdersAsync(customerId, cancellationToken);
        sw.Stop();
        logger.LogInformation("GET /api/orders/customer/{CustomerId} executed in {ElapsedMs} ms", customerId, sw.ElapsedMilliseconds);
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates stale dashboard behavior when writes and cached reads happen in the same user flow.
    /// </summary>
    [HttpPost("with-cache-check")]
    public async Task<ActionResult<OrderWithDashboardResponse>> CreateOrderWithCacheCheck(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.CreateOrderAndReadCachedDashboardAsync(request, cancellationToken);
        sw.Stop();
        logger.LogInformation("POST /api/orders/with-cache-check executed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates full-scan aggregate dashboard cost and contention against order writes.
    /// </summary>
    [HttpGet("/api/dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.GetDashboardAsync(cancellationToken);
        sw.Stop();
        logger.LogInformation("GET /api/dashboard executed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates direct comparison of raw aggregation versus materialized view read latency.
    /// </summary>
    [HttpGet("/api/dashboard/compare")]
    public async Task<ActionResult<object>> CompareDashboard(CancellationToken cancellationToken)
    {
        var result = await orderService.CompareDashboardQueriesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates side-by-side latency impact for writes, reads, and mixed-load CRUD contention.
    /// </summary>
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

    /// <summary>
    /// Demonstrates benchmark deltas after introducing the dashboard materialized view.
    /// </summary>
    [HttpPost("/api/benchmarks/materialized-impact")]
    public async Task<ActionResult<object>> RunMaterializedViewImpactBenchmark(
        [FromQuery] double rawDashboardMs = 1840,
        [FromQuery] double rawCreateOrderMs = 180,
        [FromQuery] double rawMixedWriteMs = 320,
        CancellationToken cancellationToken = default)
    {
        var currentCreate = await benchmark.ConcurrentWritesOnly(cancellationToken);
        var currentDashboard = await benchmark.ConcurrentReadsOnly(cancellationToken);
        var mixedResult = await benchmark.MixedLoad(currentCreate, cancellationToken);
        var mixedWriteCurrent = mixedResult.AvgCreateOrderTimeMs;

        benchmark.PrintMaterializedViewImpact(
            rawDashboardMs,
            currentDashboard,
            rawCreateOrderMs,
            currentCreate,
            rawMixedWriteMs,
            mixedWriteCurrent);

        return Ok(new
        {
            RawQuery = new
            {
                GetDashboardMs = rawDashboardMs,
                CreateOrderMs = rawCreateOrderMs,
                MixedLoadWritesMs = rawMixedWriteMs
            },
            MaterializedView = new
            {
                GetDashboardMs = currentDashboard,
                CreateOrderMs = currentCreate,
                MixedLoadWritesMs = mixedWriteCurrent
            }
        });
    }
}
