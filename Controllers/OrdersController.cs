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

    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.CreateOrderAsync(request, cancellationToken);
        sw.Stop();
        logger.LogInformation("POST /api/orders executed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return Ok(result);
    }

  
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailsResponse>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.GetOrderByIdAsync(id, cancellationToken);
        sw.Stop();
        logger.LogInformation("GET /api/orders/{OrderId} executed in {ElapsedMs} ms", id, sw.ElapsedMilliseconds);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<List<CustomerOrderSummaryResponse>>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.GetCustomerOrdersAsync(customerId, cancellationToken);
        sw.Stop();
        logger.LogInformation("GET /api/orders/customer/{CustomerId} executed in {ElapsedMs} ms", customerId, sw.ElapsedMilliseconds);
        return Ok(result);
    }

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

    [HttpGet("/api/dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await orderService.GetDashboardAsync(cancellationToken);
        sw.Stop();
        logger.LogInformation("GET /api/dashboard executed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
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
}
