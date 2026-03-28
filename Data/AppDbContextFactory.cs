using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderManagement.CRUD.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=OrderManagementCrudDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
        return new AppDbContext(optionsBuilder.Options);
    }
}
