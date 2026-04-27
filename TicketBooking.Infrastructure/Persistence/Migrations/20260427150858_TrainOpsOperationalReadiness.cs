using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrainOpsOperationalReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoardingGate",
                schema: "train",
                table: "TripStopTimes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoardingStatus",
                schema: "train",
                table: "TripStopTimes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlatformCode",
                schema: "train",
                table: "TripStopTimes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackCode",
                schema: "train",
                table: "TripStopTimes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FareClasses",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SeatType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DefaultModifier = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationalEvents",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OldDepartureAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NewDepartureAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OldArrivalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NewArrivalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OldPlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NewPlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    OldTrackCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NewTrackCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReasonText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InternalNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NotifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalEvents_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeatBlocks",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainCarSeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStopIndex = table.Column<int>(type: "int", nullable: false),
                    ToStopIndex = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReasonText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReleasedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatBlocks", x => x.Id);
                    table.CheckConstraint("CK_train_SeatBlocks_StopIndex", "[FromStopIndex] < [ToStopIndex]");
                    table.ForeignKey(
                        name: "FK_SeatBlocks_TrainCarSeats_TrainCarSeatId",
                        column: x => x.TrainCarSeatId,
                        principalSchema: "train",
                        principalTable: "TrainCarSeats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatBlocks_TripStopTimes_FromTripStopTimeId",
                        column: x => x.FromTripStopTimeId,
                        principalSchema: "train",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatBlocks_TripStopTimes_ToTripStopTimeId",
                        column: x => x.ToTripStopTimeId,
                        principalSchema: "train",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatBlocks_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FareRules",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FareClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStopIndex = table.Column<int>(type: "int", nullable: false),
                    ToStopIndex = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    BaseFare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxesFees = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareRules", x => x.Id);
                    table.CheckConstraint("CK_train_FareRules_StopIndex", "[FromStopIndex] < [ToStopIndex]");
                    table.ForeignKey(
                        name: "FK_FareRules_FareClasses_FareClassId",
                        column: x => x.FareClassId,
                        principalSchema: "train",
                        principalTable: "FareClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FareRules_Routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "train",
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FareRules_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_TenantId_Code",
                schema: "train",
                table: "FareClasses",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_TenantId_SeatType_IsActive",
                schema: "train",
                table: "FareClasses",
                columns: new[] { "TenantId", "SeatType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_FareClassId",
                schema: "train",
                table: "FareRules",
                column: "FareClassId");

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_RouteId",
                schema: "train",
                table: "FareRules",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_TenantId_IsActive",
                schema: "train",
                table: "FareRules",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_TenantId_RouteId_FareClassId_FromStopIndex_ToStopIndex",
                schema: "train",
                table: "FareRules",
                columns: new[] { "TenantId", "RouteId", "FareClassId", "FromStopIndex", "ToStopIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_TenantId_TripId_FareClassId_FromStopIndex_ToStopIndex",
                schema: "train",
                table: "FareRules",
                columns: new[] { "TenantId", "TripId", "FareClassId", "FromStopIndex", "ToStopIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_TripId",
                schema: "train",
                table: "FareRules",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalEvents_TenantId_TripId_CreatedAt",
                schema: "train",
                table: "OperationalEvents",
                columns: new[] { "TenantId", "TripId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalEvents_TenantId_Type_Status",
                schema: "train",
                table: "OperationalEvents",
                columns: new[] { "TenantId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalEvents_TripId",
                schema: "train",
                table: "OperationalEvents",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_FromTripStopTimeId",
                schema: "train",
                table: "SeatBlocks",
                column: "FromTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_TenantId_TripId_Status",
                schema: "train",
                table: "SeatBlocks",
                columns: new[] { "TenantId", "TripId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_ToTripStopTimeId",
                schema: "train",
                table: "SeatBlocks",
                column: "ToTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_TrainCarSeatId",
                schema: "train",
                table: "SeatBlocks",
                column: "TrainCarSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_TripId_FromStopIndex_ToStopIndex",
                schema: "train",
                table: "SeatBlocks",
                columns: new[] { "TripId", "FromStopIndex", "ToStopIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_TripId_TrainCarSeatId_Status_FromStopIndex_ToStopIndex",
                schema: "train",
                table: "SeatBlocks",
                columns: new[] { "TripId", "TrainCarSeatId", "Status", "FromStopIndex", "ToStopIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FareRules",
                schema: "train");

            migrationBuilder.DropTable(
                name: "OperationalEvents",
                schema: "train");

            migrationBuilder.DropTable(
                name: "SeatBlocks",
                schema: "train");

            migrationBuilder.DropTable(
                name: "FareClasses",
                schema: "train");

            migrationBuilder.DropColumn(
                name: "BoardingGate",
                schema: "train",
                table: "TripStopTimes");

            migrationBuilder.DropColumn(
                name: "BoardingStatus",
                schema: "train",
                table: "TripStopTimes");

            migrationBuilder.DropColumn(
                name: "PlatformCode",
                schema: "train",
                table: "TripStopTimes");

            migrationBuilder.DropColumn(
                name: "TrackCode",
                schema: "train",
                table: "TripStopTimes");
        }
    }
}
