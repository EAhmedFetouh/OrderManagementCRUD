using Microsoft.AspNetCore.Mvc;
using OrderManagement.CRUD.Benchmarks;
using OrderManagement.CRUD.Models.DTOs;
using OrderManagement.CRUD.Services;

namespace OrderManagement.CRUD.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    IOrderService orderService,
    PerformanceBenchmark benchmark) : ControllerBase
{
  
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await orderService.CreateOrderAsync(request, cancellationToken);
        return Ok(result);
    }

  
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailsResponse>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await orderService.GetOrderByIdAsync(id, cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

   
    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<List<CustomerOrderSummaryResponse>>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await orderService.GetCustomerOrdersAsync(customerId, cancellationToken);
        return Ok(result);
    }

   
    [HttpGet("/api/dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await orderService.GetDashboardAsync(cancellationToken);
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
