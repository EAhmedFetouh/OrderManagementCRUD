using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.CRUD.Migrations
{
    /// <summary>
    /// Groups the materialized data by day so it can answer the same last-month window as the raw query.
    /// </summary>
    public partial class ScopeDashboardViewsByDay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_DashboardSummary;");

            migrationBuilder.Sql(
                """
                EXEC('
                CREATE VIEW dbo.vw_DashboardSummary
                WITH SCHEMABINDING
                AS
                SELECT
                    CONVERT(date, o.OrderDate) AS OrderDay,
                    o.Status,
                    COUNT_BIG(*) AS OrdersCount,
                    SUM(CONVERT(decimal(19,2), o.TotalAmount)) AS Revenue
                FROM dbo.Orders AS o
                GROUP BY CONVERT(date, o.OrderDate), o.Status
                ');
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE CLUSTERED INDEX IX_vw_DashboardSummary_OrderDay_Status
                ON dbo.vw_DashboardSummary (OrderDay, Status);
                """);

            migrationBuilder.Sql(
                """
                EXEC('
                CREATE VIEW dbo.vw_DashboardTopProducts
                WITH SCHEMABINDING
                AS
                SELECT
                    CONVERT(date, o.OrderDate) AS OrderDay,
                    oi.ProductId,
                    COUNT_BIG(*) AS LinesCount,
                    SUM(CONVERT(bigint, oi.Quantity)) AS TotalQuantity
                FROM dbo.OrderItems AS oi
                INNER JOIN dbo.Orders AS o ON o.Id = oi.OrderId
                GROUP BY CONVERT(date, o.OrderDate), oi.ProductId
                ');
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE CLUSTERED INDEX IX_vw_DashboardTopProducts_OrderDay_ProductId
                ON dbo.vw_DashboardTopProducts (OrderDay, ProductId);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_DashboardTopProducts;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_DashboardSummary;");

            migrationBuilder.Sql(
                """
                EXEC('
                CREATE VIEW dbo.vw_DashboardSummary
                WITH SCHEMABINDING
                AS
                SELECT
                    o.Status,
                    COUNT_BIG(*) AS OrdersPerStatus,
                    SUM(CONVERT(decimal(19,2), o.TotalAmount)) AS RevenuePerStatus
                FROM dbo.Orders AS o
                GROUP BY o.Status
                ');
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE CLUSTERED INDEX IX_vw_DashboardSummary_Status
                ON dbo.vw_DashboardSummary (Status);
                """);
        }
    }
}
