using Microsoft.EntityFrameworkCore;
using OrderManagement.CRUD.Data;
using OrderManagement.CRUD.Models.DTOs;
using OrderManagement.CRUD.Models.Entities;

namespace OrderManagement.CRUD.Repositories;

public interface IOrderRepository
{
 
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);

  
    Task<OrderDetailsResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<List<CustomerOrderSummaryResponse>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken);

  
    Task<DashboardResponse> GetDashboardRawAggregationAsync(bool simulateSlowQueries, CancellationToken cancellationToken);
}

public class OrderRepository(AppDbContext context) : IOrderRepository
{
   
    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        var customerExists = await context.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new InvalidOperationException("Customer does not exist.");
        }

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new InvalidOperationException($"Product {item.ProductId} does not exist.");
            }

            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for product {product.Name}.");
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        var orderItems = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            product.StockQuantity -= item.Quantity;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        order.TotalAmount = orderItems.Sum(x => x.UnitPrice * x.Quantity);

        await context.Orders.AddAsync(order, cancellationToken);
        await context.OrderItems.AddRangeAsync(orderItems, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new CreateOrderResponse
        {
            OrderId = order.Id,
            TotalAmount = order.TotalAmount
        };
    }

    public async Task<OrderDetailsResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new OrderDetailsResponse
            {
                OrderId = o.Id,
                CustomerName = o.Customer!.Name,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Items = o.Items.Select(i => new OrderItemDetailsResponse
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product!.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

  
    public async Task<List<CustomerOrderSummaryResponse>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new CustomerOrderSummaryResponse
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Sum(x => x.Quantity)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardResponse> GetDashboardRawAggregationAsync(bool simulateSlowQueries, CancellationToken cancellationToken)
    {
        if (simulateSlowQueries)
        {
            Thread.Sleep(250);
        }

        var lastMonthStart = DateTime.UtcNow.AddMonths(-1);
        var lastMonthOrders = context.Orders.Where(o => o.OrderDate >= lastMonthStart);

        // Narrow the scans so the engine can benefit from OrderDate indexes when present.
        var totalOrders = await lastMonthOrders.CountAsync(cancellationToken);
        var totalRevenue = await lastMonthOrders.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken);
        var avgOrder = await lastMonthOrders.AverageAsync(o => (decimal?)o.TotalAmount, cancellationToken);
        var topProducts = await context.OrderItems
            .Where(oi => oi.Order!.OrderDate >= lastMonthStart)
            .GroupBy(x => new { x.ProductId, ProductName = x.Product!.Name })
            .Select(g => new TopSellingProductResponse
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(5)
            .ToListAsync(cancellationToken);
        var status = await lastMonthOrders
            .GroupBy(x => x.Status)
            .Select(g => new OrderStatusCountResponse
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);
        var revenueLastMonth = await context.Orders
            .Where(x => x.OrderDate >= lastMonthStart)
            .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken);

        return new DashboardResponse
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue ?? 0,
            AverageOrderValue = avgOrder ?? 0,
            TopSellingProducts = topProducts,
            OrdersPerStatus = status,
            RevenueLastMonth = revenueLastMonth ?? 0
        };
    }
}
