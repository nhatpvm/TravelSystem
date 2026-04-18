using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Train_HoldTokenIndexes_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_HoldToken",
                schema: "train",
                table: "TripSeatHolds");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TenantId_HoldToken",
                schema: "train",
                table: "TripSeatHolds",
                columns: new[] { "TenantId", "HoldToken" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_HoldToken",
                schema: "train",
                table: "TripSeatHolds",
                columns: new[] { "TenantId", "TripId", "HoldToken" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_TrainCarSeatId_Status_HoldExpiresAt",
                schema: "train",
                table: "TripSeatHolds",
                columns: new[] { "TenantId", "TripId", "TrainCarSeatId", "Status", "HoldExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_TenantId_HoldToken",
                schema: "train",
                table: "TripSeatHolds");

            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_HoldToken",
                schema: "train",
                table: "TripSeatHolds");

            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_TrainCarSeatId_Status_HoldExpiresAt",
                schema: "train",
                table: "TripSeatHolds");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_HoldToken",
                schema: "train",
                table: "TripSeatHolds",
                column: "HoldToken",
                unique: true);
        }
    }
}
