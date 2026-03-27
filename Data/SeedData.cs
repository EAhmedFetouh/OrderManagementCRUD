using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderManagement.CRUD.Models;
using OrderManagement.CRUD.Models.Entities;

namespace OrderManagement.CRUD.Data;

public static class SeedData
{
 
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var demoSettings = scope.ServiceProvider.GetRequiredService<IOptions<DemoSettings>>().Value;

        await context.Database.MigrateAsync();

        if (await context.Orders.AnyAsync())
        {
            logger.LogInformation("Seed skipped because the database is not empty.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var random = new Random(42);

        logger.LogInformation("Seeding customers ({Count})...", demoSettings.SeedCustomers);
        var customers = Enumerable.Range(1, demoSettings.SeedCustomers)
            .Select(i => new Customer
            {
                Id = Guid.NewGuid(),
                Name = $"Customer {i}",
                Email = $"customer{i}@demo.local",
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 2_000))
            })
            .ToList();
        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeding products ({Count})...", demoSettings.SeedProducts);
        var products = Enumerable.Range(1, demoSettings.SeedProducts)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Price = Math.Round((decimal)(random.NextDouble() * 490 + 10), 2),
                StockQuantity = random.Next(20_000, 50_000)
            })
            .ToList();
        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeding orders and order items ({Count})...", demoSettings.SeedOrders);
        var batchSize = demoSettings.SeedBatchSize <= 0 ? 1_000 : demoSettings.SeedBatchSize;
        var customerIds = customers.Select(c => c.Id).ToArray();
        var minItems = Math.Max(1, demoSettings.SeedMinItemsPerOrder);
        var maxItems = Math.Max(minItems, demoSettings.SeedMaxItemsPerOrder);

        for (var batchStart = 0; batchStart < demoSettings.SeedOrders; batchStart += batchSize)
        {
            var remaining = Math.Min(batchSize, demoSettings.SeedOrders - batchStart);
            var orderBatch = new List<Order>(remaining);
            var itemBatch = new List<OrderItem>(remaining * 3);

            for (var i = 0; i < remaining; i++)
            {
                var orderId = Guid.NewGuid();
                var itemCount = random.Next(minItems, maxItems + 1);
                var orderItems = new List<OrderItem>(itemCount);

                for (var j = 0; j < itemCount; j++)
                {
                    var product = products[random.Next(products.Count)];
                    var qty = random.Next(1, 5);
                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        ProductId = product.Id,
                        Quantity = qty,
                        UnitPrice = product.Price
                    });
                }

                var total = orderItems.Sum(x => x.Quantity * x.UnitPrice);
                orderBatch.Add(new Order
                {
                    Id = orderId,
                    CustomerId = customerIds[random.Next(customerIds.Length)],
                    OrderDate = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                    Status = (OrderStatus)random.Next(1, 5),
                    TotalAmount = total
                });
                itemBatch.AddRange(orderItems);
            }

            await context.Orders.AddRangeAsync(orderBatch);
            await context.OrderItems.AddRangeAsync(itemBatch);
            await context.SaveChangesAsync();
            logger.LogInformation("Seed progress: {Count}/{Total} orders", batchStart + remaining, demoSettings.SeedOrders);
        }

        stopwatch.Stop();
        logger.LogInformation("Database seed completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }
}
