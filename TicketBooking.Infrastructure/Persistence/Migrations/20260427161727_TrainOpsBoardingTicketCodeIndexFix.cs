using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrainOpsBoardingTicketCodeIndexFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketCheckIns_TenantId_TicketCode",
                schema: "train",
                table: "TicketCheckIns");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TenantId_TicketCode",
                schema: "train",
                table: "TicketCheckIns",
                columns: new[] { "TenantId", "TicketCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TenantId_TicketCode_TrainCarSeatId",
                schema: "train",
                table: "TicketCheckIns",
                columns: new[] { "TenantId", "TicketCode", "TrainCarSeatId" },
                unique: true,
                filter: "[TrainCarSeatId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketCheckIns_TenantId_TicketCode",
                schema: "train",
                table: "TicketCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_TicketCheckIns_TenantId_TicketCode_TrainCarSeatId",
                schema: "train",
                table: "TicketCheckIns");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TenantId_TicketCode",
                schema: "train",
                table: "TicketCheckIns",
                columns: new[] { "TenantId", "TicketCode" },
                unique: true);
        }
    }
}
