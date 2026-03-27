using Microsoft.EntityFrameworkCore;
using OrderManagement.CRUD.Models.Entities;

namespace OrderManagement.CRUD.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasKey(x => x.Id);
        modelBuilder.Entity<Product>().HasKey(x => x.Id);
        modelBuilder.Entity<Order>().HasKey(x => x.Id);
        modelBuilder.Entity<OrderItem>().HasKey(x => x.Id);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Product)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductId);

        modelBuilder.Entity<Customer>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Customer>().Property(x => x.Email).HasMaxLength(200);
        modelBuilder.Entity<Product>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Product>().Property(x => x.Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var indexes = entityType.GetIndexes().ToList();
            foreach (var index in indexes)
            {
                entityType.RemoveIndex(index.Properties);
            }
        }
    }
}
