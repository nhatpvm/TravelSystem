using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrainOpsTicketExchangeBoardingTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketChangeRequests",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewHoldToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangeFeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FareDifferenceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayableDifferenceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReasonText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StaffNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuotedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketChangeRequests_CustomerOrders_OriginalOrderId",
                        column: x => x.OriginalOrderId,
                        principalSchema: "commerce",
                        principalTable: "CustomerOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketChangeRequests_Trips_NewTripId",
                        column: x => x.NewTripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketChangeRequests_Trips_OriginalTripId",
                        column: x => x.OriginalTripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketCheckIns",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainCarSeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TicketCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CarNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SeatNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PassengerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    GateCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DeviceCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CheckedInByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BoardedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BoardedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketCheckIns_CustomerOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "commerce",
                        principalTable: "CustomerOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketCheckIns_CustomerTickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "commerce",
                        principalTable: "CustomerTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketCheckIns_TrainCarSeats_TrainCarSeatId",
                        column: x => x.TrainCarSeatId,
                        principalSchema: "train",
                        principalTable: "TrainCarSeats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketCheckIns_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainSets",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_TrainSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainSetCarTemplates",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CarType = table.Column<int>(type: "int", nullable: false),
                    CabinClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TrainSetCarTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainSetCarTemplates_TrainSets_TrainSetId",
                        column: x => x.TrainSetId,
                        principalSchema: "train",
                        principalTable: "TrainSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainSetSeatTemplates",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainSetCarTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SeatType = table.Column<int>(type: "int", nullable: false),
                    CompartmentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompartmentIndex = table.Column<int>(type: "int", nullable: true),
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    ColumnIndex = table.Column<int>(type: "int", nullable: false),
                    IsWindow = table.Column<bool>(type: "bit", nullable: false),
                    IsAisle = table.Column<bool>(type: "bit", nullable: false),
                    SeatClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriceModifier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_TrainSetSeatTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainSetSeatTemplates_TrainSetCarTemplates_TrainSetCarTemplateId",
                        column: x => x.TrainSetCarTemplateId,
                        principalSchema: "train",
                        principalTable: "TrainSetCarTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeRequests_NewTripId",
                schema: "train",
                table: "TicketChangeRequests",
                column: "NewTripId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeRequests_OriginalOrderId",
                schema: "train",
                table: "TicketChangeRequests",
                column: "OriginalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeRequests_OriginalTripId",
                schema: "train",
                table: "TicketChangeRequests",
                column: "OriginalTripId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeRequests_TenantId_NewHoldToken",
                schema: "train",
                table: "TicketChangeRequests",
                columns: new[] { "TenantId", "NewHoldToken" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeRequests_TenantId_NewTripId",
                schema: "train",
                table: "TicketChangeRequests",
                columns: new[] { "TenantId", "NewTripId" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeRequests_TenantId_OriginalOrderId_Status",
                schema: "train",
                table: "TicketChangeRequests",
                columns: new[] { "TenantId", "OriginalOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_OrderId",
                schema: "train",
                table: "TicketCheckIns",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TenantId_OrderId",
                schema: "train",
                table: "TicketCheckIns",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TenantId_TicketCode",
                schema: "train",
                table: "TicketCheckIns",
                columns: new[] { "TenantId", "TicketCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TenantId_TripId_Status",
                schema: "train",
                table: "TicketCheckIns",
                columns: new[] { "TenantId", "TripId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TicketId",
                schema: "train",
                table: "TicketCheckIns",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TrainCarSeatId",
                schema: "train",
                table: "TicketCheckIns",
                column: "TrainCarSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCheckIns_TripId",
                schema: "train",
                table: "TicketCheckIns",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainSetCarTemplates_TrainSetId_CarNumber",
                schema: "train",
                table: "TrainSetCarTemplates",
                columns: new[] { "TrainSetId", "CarNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainSetCarTemplates_TrainSetId_SortOrder",
                schema: "train",
                table: "TrainSetCarTemplates",
                columns: new[] { "TrainSetId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainSets_TenantId_Code",
                schema: "train",
                table: "TrainSets",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainSets_TenantId_Status_IsActive",
                schema: "train",
                table: "TrainSets",
                columns: new[] { "TenantId", "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainSetSeatTemplates_TrainSetCarTemplateId_CompartmentCode",
                schema: "train",
                table: "TrainSetSeatTemplates",
                columns: new[] { "TrainSetCarTemplateId", "CompartmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainSetSeatTemplates_TrainSetCarTemplateId_SeatNumber",
                schema: "train",
                table: "TrainSetSeatTemplates",
                columns: new[] { "TrainSetCarTemplateId", "SeatNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketChangeRequests",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TicketCheckIns",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TrainSetSeatTemplates",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TrainSetCarTemplates",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TrainSets",
                schema: "train");
        }
    }
}
