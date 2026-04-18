using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_Tours_PriceQuantityBands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourSchedulePrices_TenantId_TourScheduleId_PriceType",
                schema: "tours",
                table: "TourSchedulePrices");

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedulePrices_TenantId_TourScheduleId_PriceType",
                schema: "tours",
                table: "TourSchedulePrices",
                columns: new[] { "TenantId", "TourScheduleId", "PriceType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourSchedulePrices_TenantId_TourScheduleId_PriceType",
                schema: "tours",
                table: "TourSchedulePrices");

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedulePrices_TenantId_TourScheduleId_PriceType",
                schema: "tours",
                table: "TourSchedulePrices",
                columns: new[] { "TenantId", "TourScheduleId", "PriceType" },
                unique: true);
        }
    }
}
