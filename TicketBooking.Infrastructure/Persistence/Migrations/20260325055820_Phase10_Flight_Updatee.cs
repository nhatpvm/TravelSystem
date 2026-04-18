// FILE: TicketBooking.Infrastructure/Persistence/Migrations/Phase10_Flight_Updatee.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10_Flight_Updatee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_HoldToken",
                schema: "bus",
                table: "TripSeatHolds");

            // FIX: SQL Server cannot ALTER varbinary(max) -> rowversion/timestamp.
            // Must DROP old column then ADD new rowversion column.

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopTimes");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopTimes",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopPickupPoints");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopPickupPoints",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopDropoffPoints");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopDropoffPoints",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripSegmentPrices");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripSegmentPrices",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripSeatHolds");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripSeatHolds",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "Trips");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "Trips",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "StopPoints");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "StopPoints",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "RouteStops");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "RouteStops",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "Routes");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "Routes",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TenantId_HoldToken",
                schema: "bus",
                table: "TripSeatHolds",
                columns: new[] { "TenantId", "HoldToken" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_HoldToken",
                schema: "bus",
                table: "TripSeatHolds",
                columns: new[] { "TenantId", "TripId", "HoldToken" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_SeatId_Status_HoldExpiresAt",
                schema: "bus",
                table: "TripSeatHolds",
                columns: new[] { "TenantId", "TripId", "SeatId", "Status", "HoldExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_TenantId_HoldToken",
                schema: "bus",
                table: "TripSeatHolds");

            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_HoldToken",
                schema: "bus",
                table: "TripSeatHolds");

            migrationBuilder.DropIndex(
                name: "IX_TripSeatHolds_TenantId_TripId_SeatId_Status_HoldExpiresAt",
                schema: "bus",
                table: "TripSeatHolds");

            // Reverse FIX: drop rowversion, add back varbinary(max)

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopTimes");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopTimes",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopPickupPoints");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopPickupPoints",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopDropoffPoints");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripStopDropoffPoints",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripSegmentPrices");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripSegmentPrices",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "TripSeatHolds");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "TripSeatHolds",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "Trips");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "Trips",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "StopPoints");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "StopPoints",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "RouteStops");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "RouteStops",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "bus",
                table: "Routes");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "bus",
                table: "Routes",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_HoldToken",
                schema: "bus",
                table: "TripSeatHolds",
                column: "HoldToken");
        }
    }
}
