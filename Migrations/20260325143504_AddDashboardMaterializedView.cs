using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.CRUD.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardMaterializedView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_PADDING ON;
                SET ANSI_WARNINGS ON;
                SET CONCAT_NULL_YIELDS_NULL ON;
                SET ARITHABORT ON;
                SET NUMERIC_ROUNDABORT OFF;

                IF OBJECT_ID('dbo.vw_DashboardSummary', 'V') IS NULL
                BEGIN
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
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_vw_DashboardSummary_Status'
                      AND object_id = OBJECT_ID('dbo.vw_DashboardSummary')
                )
                BEGIN
                    CREATE UNIQUE CLUSTERED INDEX IX_vw_DashboardSummary_Status
                    ON dbo.vw_DashboardSummary (Status);
                END
                """);
        }

     
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_vw_DashboardSummary_Status'
                      AND object_id = OBJECT_ID('dbo.vw_DashboardSummary')
                )
                BEGIN
                    DROP INDEX IX_vw_DashboardSummary_Status ON dbo.vw_DashboardSummary;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID('dbo.vw_DashboardSummary', 'V') IS NOT NULL
                BEGIN
                    DROP VIEW dbo.vw_DashboardSummary;
                END
                """);
        }
    }
}
