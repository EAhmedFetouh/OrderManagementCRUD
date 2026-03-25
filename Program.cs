using Microsoft.EntityFrameworkCore;
using OrderManagement.CRUD.Benchmarks;
using OrderManagement.CRUD.Data;
using OrderManagement.CRUD.Middleware;
using OrderManagement.CRUD.Models;
using OrderManagement.CRUD.Repositories;
using OrderManagement.CRUD.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<DemoSettings>(builder.Configuration.GetSection("DemoSettings"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<PerformanceBenchmark>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestTimingMiddleware>();
app.UseAuthorization();
app.MapControllers();

await SeedData.InitializeAsync(app.Services, app.Logger);

app.Run();
