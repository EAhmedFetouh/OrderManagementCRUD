using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderManagement.CRUD.Models.Entities;

namespace OrderManagement.CRUD.Data;

public static class SeedData
{
    /// <summary>
    /// Seeds a high-volume dataset once to demonstrate how basic CRUD patterns degrade as row counts grow.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        if (await context.Orders.AnyAsync())
        {
            logger.LogInformation("Seed skipped because the database is not empty.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var random = new Random(42);

        logger.LogInformation("Seeding customers...");
        var customers = Enumerable.Range(1, 1_000)
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

        logger.LogInformation("Seeding products...");
        var products = Enumerable.Range(1, 500)
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

        logger.LogInformation("Seeding orders and order items...");
        const int batchSize = 1000;
        var customerIds = customers.Select(c => c.Id).ToArray();

        for (var batchStart = 0; batchStart < 100_000; batchStart += batchSize)
        {
            var orderBatch = new List<Order>(batchSize);
            var itemBatch = new List<OrderItem>(batchSize * 3);

            for (var i = 0; i < batchSize; i++)
            {
                var orderId = Guid.NewGuid();
                var itemCount = random.Next(1, 6);
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
            logger.LogInformation("Seed progress: {Count}/100000 orders", batchStart + batchSize);
        }

        stopwatch.Stop();
        logger.LogInformation("Database seed completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }
}
