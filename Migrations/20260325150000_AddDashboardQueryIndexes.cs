using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.CRUD.Migrations
{
    /// <summary>
    /// Dashboard-aligned indexes: columns used by GetDashboardRawAggregationAsync
    /// (OrderDate range, Status grouping, OrderItems.ProductId for top products aggregation).
    /// No FK-only indexes on CustomerId/OrderId — fewer write-side index updates than misaligned FK indexes.
    /// </summary>
    public partial class AddDashboardQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate",
                table: "Orders");
        }
    }
}
