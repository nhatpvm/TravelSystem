using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10_Flight_Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AirlineId",
                schema: "flight",
                table: "OfferSegments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaggagePolicyJson",
                schema: "flight",
                table: "OfferSegments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CabinClass",
                schema: "flight",
                table: "OfferSegments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CabinSeatMapId",
                schema: "flight",
                table: "OfferSegments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FareClassId",
                schema: "flight",
                table: "OfferSegments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FareRulesJson",
                schema: "flight",
                table: "OfferSegments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FlightId",
                schema: "flight",
                table: "OfferSegments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                schema: "flight",
                table: "OfferSegments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeckIndex",
                schema: "flight",
                table: "CabinSeats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SeatType",
                schema: "flight",
                table: "CabinSeats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeckCount",
                schema: "flight",
                table: "CabinSeatMaps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirlineId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "BaggagePolicyJson",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "CabinClass",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "CabinSeatMapId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "FareClassId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "FareRulesJson",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "FlightId",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                schema: "flight",
                table: "OfferSegments");

            migrationBuilder.DropColumn(
                name: "DeckIndex",
                schema: "flight",
                table: "CabinSeats");

            migrationBuilder.DropColumn(
                name: "SeatType",
                schema: "flight",
                table: "CabinSeats");

            migrationBuilder.DropColumn(
                name: "DeckCount",
                schema: "flight",
                table: "CabinSeatMaps");
        }
    }
}
