using OrderManagement.CRUD.Models.Entities;

namespace OrderManagement.CRUD.Models.DTOs;

public class DashboardResponse
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<TopSellingProductResponse> TopSellingProducts { get; set; } = new();
    public List<OrderStatusCountResponse> OrdersPerStatus { get; set; } = new();
}

public class TopSellingProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
}

public class OrderStatusCountResponse
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
}

public class OrderWithDashboardResponse
{
    public CreateOrderResponse Order { get; set; } = new();
    public DashboardResponse Dashboard { get; set; } = new();
    public string CacheWarning { get; set; } = "Dashboard data is up to 30 seconds old. New order may not be reflected.";
}
