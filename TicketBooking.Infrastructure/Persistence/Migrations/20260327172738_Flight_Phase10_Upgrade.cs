// FILE: TicketBooking.Infrastructure/Persistence/Migrations/Flight_Phase10_Upgrade.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Flight_Phase10_Upgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Airports_TenantId_IataCode",
                schema: "flight",
                table: "Airports");

            migrationBuilder.DropIndex(
                name: "IX_Aircrafts_TenantId_Registration",
                schema: "flight",
                table: "Aircrafts");

            // Backfill old data BEFORE altering / adding constraints
            migrationBuilder.Sql(@"
UPDATE flight.CabinSeats
SET SeatType = LEFT(SeatType, 50)
WHERE SeatType IS NOT NULL AND LEN(SeatType) > 50;
");

            migrationBuilder.Sql(@"
UPDATE flight.CabinSeats
SET DeckIndex = 1
WHERE DeckIndex IS NULL OR DeckIndex <= 0;
");

            migrationBuilder.Sql(@"
UPDATE flight.CabinSeatMaps
SET DeckCount = 1
WHERE DeckCount IS NULL OR DeckCount <= 0;
");

            migrationBuilder.AlterColumn<string>(
                name: "SeatType",
                schema: "flight",
                table: "CabinSeats",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeckIndex",
                schema: "flight",
                table: "CabinSeats",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "DeckCount",
                schema: "flight",
                table: "CabinSeatMaps",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_OfferTaxFeeLines_Amount",
                schema: "flight",
                table: "OfferTaxFeeLines",
                sql: "[Amount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_OfferTaxFeeLines_SortOrder",
                schema: "flight",
                table: "OfferTaxFeeLines",
                sql: "[SortOrder] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_OfferSegments_AirlineId",
                schema: "flight",
                table: "OfferSegments",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferSegments_CabinSeatMapId",
                schema: "flight",
                table: "OfferSegments",
                column: "CabinSeatMapId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferSegments_FareClassId",
                schema: "flight",
                table: "OfferSegments",
                column: "FareClassId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferSegments_FlightId",
                schema: "flight",
                table: "OfferSegments",
                column: "FlightId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_OfferSegments_ArrivalAfterDeparture",
                schema: "flight",
                table: "OfferSegments",
                sql: "[ArrivalAt] > [DepartureAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_Offers_SeatsAvailable",
                schema: "flight",
                table: "Offers",
                sql: "[SeatsAvailable] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_Flights_ArrivalAfterDeparture",
                schema: "flight",
                table: "Flights",
                sql: "[ArrivalAt] > [DepartureAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_Flights_FromToDifferent",
                schema: "flight",
                table: "Flights",
                sql: "[FromAirportId] <> [ToAirportId]");

            migrationBuilder.CreateIndex(
                name: "IX_CabinSeats_CabinSeatMapId_DeckIndex_RowIndex_ColumnIndex",
                schema: "flight",
                table: "CabinSeats",
                columns: new[] { "CabinSeatMapId", "DeckIndex", "RowIndex", "ColumnIndex" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_CabinSeats_ColumnIndex",
                schema: "flight",
                table: "CabinSeats",
                sql: "[ColumnIndex] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_CabinSeats_DeckIndex",
                schema: "flight",
                table: "CabinSeats",
                sql: "[DeckIndex] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_CabinSeats_RowIndex",
                schema: "flight",
                table: "CabinSeats",
                sql: "[RowIndex] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_CabinSeatMaps_DeckCount",
                schema: "flight",
                table: "CabinSeatMaps",
                sql: "[DeckCount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_flight_AncillaryDefinitions_Price",
                schema: "flight",
                table: "AncillaryDefinitions",
                sql: "[Price] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Airports_TenantId_IataCode",
                schema: "flight",
                table: "Airports",
                columns: new[] { "TenantId", "IataCode" },
                unique: true,
                filter: "[IataCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Airports_TenantId_IcaoCode",
                schema: "flight",
                table: "Airports",
                columns: new[] { "TenantId", "IcaoCode" },
                unique: true,
                filter: "[IcaoCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Airlines_TenantId_IataCode",
                schema: "flight",
                table: "Airlines",
                columns: new[] { "TenantId", "IataCode" },
                unique: true,
                filter: "[IataCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Airlines_TenantId_IcaoCode",
                schema: "flight",
                table: "Airlines",
                columns: new[] { "TenantId", "IcaoCode" },
                unique: true,
                filter: "[IcaoCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Aircrafts_TenantId_Registration",
                schema: "flight",
                table: "Aircrafts",
                columns: new[] { "TenantId", "Registration" },
                unique: true,
                filter: "[Registration] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_OfferSegments_Airlines_AirlineId",
                schema: "flight",
                table: "OfferSegments",
                column: "AirlineId",
                principalSchema: "flight",
                principalTable: "Airlines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferSegments_CabinSeatMaps_CabinSeatMapId",
                schema: "flight",
                table: "OfferSegments",
                column: "CabinSeatMapId",
                principalSchema: "flight",
                principalTable: "CabinSeatMaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferSegments_FareClasses_FareClassId",
                schema: "flight",
                table: "OfferSegments",
                column: "FareClassId",
                principalSchema: "flight",
                principalTable: "FareClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferSegments_Flights_FlightId",
                schema: "flight",
                table: "OfferSegments",
                column: "FlightId",
                principalSchema: "flight",
                principalTable: "Flights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfferSegments_Airlines_AirlineId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_OfferSegments_CabinSeatMaps_CabinSeatMapId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_OfferSegments_FareClasses_FareClassId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_OfferSegments_Flights_FlightId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_OfferTaxFeeLines_Amount",
                schema: "flight",
                table: "OfferTaxFeeLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_OfferTaxFeeLines_SortOrder",
                schema: "flight",
                table: "OfferTaxFeeLines");

            migrationBuilder.DropIndex(
                name: "IX_OfferSegments_AirlineId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropIndex(
                name: "IX_OfferSegments_CabinSeatMapId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropIndex(
                name: "IX_OfferSegments_FareClassId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropIndex(
                name: "IX_OfferSegments_FlightId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_OfferSegments_ArrivalAfterDeparture",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_Offers_SeatsAvailable",
                schema: "flight",
                table: "Offers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_Flights_ArrivalAfterDeparture",
                schema: "flight",
                table: "Flights");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_Flights_FromToDifferent",
                schema: "flight",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_CabinSeats_CabinSeatMapId_DeckIndex_RowIndex_ColumnIndex",
                schema: "flight",
                table: "CabinSeats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_CabinSeats_ColumnIndex",
                schema: "flight",
                table: "CabinSeats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_CabinSeats_DeckIndex",
                schema: "flight",
                table: "CabinSeats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_CabinSeats_RowIndex",
                schema: "flight",
                table: "CabinSeats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_CabinSeatMaps_DeckCount",
                schema: "flight",
                table: "CabinSeatMaps");

            migrationBuilder.DropCheckConstraint(
                name: "CK_flight_AncillaryDefinitions_Price",
                schema: "flight",
                table: "AncillaryDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Airports_TenantId_IataCode",
                schema: "flight",
                table: "Airports");

            migrationBuilder.DropIndex(
                name: "IX_Airports_TenantId_IcaoCode",
                schema: "flight",
                table: "Airports");

            migrationBuilder.DropIndex(
                name: "IX_Airlines_TenantId_IataCode",
                schema: "flight",
                table: "Airlines");

            migrationBuilder.DropIndex(
                name: "IX_Airlines_TenantId_IcaoCode",
                schema: "flight",
                table: "Airlines");

            migrationBuilder.DropIndex(
                name: "IX_Aircrafts_TenantId_Registration",
                schema: "flight",
                table: "Aircrafts");

            migrationBuilder.AlterColumn<string>(
                name: "SeatType",
                schema: "flight",
                table: "CabinSeats",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeckIndex",
                schema: "flight",
                table: "CabinSeats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "DeckCount",
                schema: "flight",
                table: "CabinSeatMaps",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Airports_TenantId_IataCode",
                schema: "flight",
                table: "Airports",
                columns: new[] { "TenantId", "IataCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Aircrafts_TenantId_Registration",
                schema: "flight",
                table: "Aircrafts",
                columns: new[] { "TenantId", "Registration" });
        }
    }
}

