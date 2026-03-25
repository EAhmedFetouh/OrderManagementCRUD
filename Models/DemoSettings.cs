namespace OrderManagement.CRUD.Models;

public class DemoSettings
{
    public bool ShowPerformanceWarnings { get; set; } = true;
    public bool SimulateSlowQueries { get; set; } = false;
    public bool EnableDashboardCaching { get; set; } = false;

    public int SeedCustomers { get; set; } = 1_000;
    public int SeedProducts { get; set; } = 500;
    public int SeedOrders { get; set; } = 100_000;
    public int SeedBatchSize { get; set; } = 1_000;

    public int SeedMinItemsPerOrder { get; set; } = 1;
    public int SeedMaxItemsPerOrder { get; set; } = 5;
}
