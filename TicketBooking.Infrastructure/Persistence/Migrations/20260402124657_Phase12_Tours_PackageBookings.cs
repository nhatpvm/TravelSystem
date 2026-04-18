using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_Tours_PackageBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourPackageBookings",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HoldStrategy = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestedPax = table.Column<int>(type: "int", nullable: false),
                    ConfirmedCapacitySlots = table.Column<int>(type: "int", nullable: false),
                    PackageSubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageBookings_TourPackageReservations_TourPackageReservationId",
                        column: x => x.TourPackageReservationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageBookings_TourPackages_TourPackageId",
                        column: x => x.TourPackageId,
                        principalSchema: "tours",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageBookings_TourSchedules_TourScheduleId",
                        column: x => x.TourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageBookings_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageBookingItems",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageReservationItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageComponentOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentType = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceHoldToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageBookingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageBookingItems_TourPackageBookings_TourPackageBookingId",
                        column: x => x.TourPackageBookingId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookingItems_TenantId_SourceHoldToken",
                schema: "tours",
                table: "TourPackageBookingItems",
                columns: new[] { "TenantId", "SourceHoldToken" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookingItems_TenantId_SourceType_SourceEntityId",
                schema: "tours",
                table: "TourPackageBookingItems",
                columns: new[] { "TenantId", "SourceType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookingItems_TenantId_TourPackageBookingId_Status",
                schema: "tours",
                table: "TourPackageBookingItems",
                columns: new[] { "TenantId", "TourPackageBookingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookingItems_TenantId_TourPackageReservationItemId",
                schema: "tours",
                table: "TourPackageBookingItems",
                columns: new[] { "TenantId", "TourPackageReservationItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookingItems_TourPackageBookingId",
                schema: "tours",
                table: "TourPackageBookingItems",
                column: "TourPackageBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TenantId_Code",
                schema: "tours",
                table: "TourPackageBookings",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TenantId_TourPackageReservationId",
                schema: "tours",
                table: "TourPackageBookings",
                columns: new[] { "TenantId", "TourPackageReservationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TenantId_TourScheduleId_Status_IsDeleted",
                schema: "tours",
                table: "TourPackageBookings",
                columns: new[] { "TenantId", "TourScheduleId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TenantId_UserId_Status_IsDeleted",
                schema: "tours",
                table: "TourPackageBookings",
                columns: new[] { "TenantId", "UserId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TourId",
                schema: "tours",
                table: "TourPackageBookings",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TourPackageId",
                schema: "tours",
                table: "TourPackageBookings",
                column: "TourPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TourPackageReservationId",
                schema: "tours",
                table: "TourPackageBookings",
                column: "TourPackageReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageBookings_TourScheduleId",
                schema: "tours",
                table: "TourPackageBookings",
                column: "TourScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPackageBookingItems",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPackageBookings",
                schema: "tours");
        }
    }
}
