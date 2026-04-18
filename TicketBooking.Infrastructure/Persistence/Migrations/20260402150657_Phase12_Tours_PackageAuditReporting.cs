using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_Tours_PackageAuditReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourPackageAuditEvents",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TourPackageReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TourPackageBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TourPackageBookingItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TourPackageCancellationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TourPackageRefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TourPackageRescheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "bit", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourPackageBookingItems_TourPackageBookingItemId",
                        column: x => x.TourPackageBookingItemId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourPackageBookings_TourPackageBookingId",
                        column: x => x.TourPackageBookingId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourPackageCancellations_TourPackageCancellationId",
                        column: x => x.TourPackageCancellationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourPackageRefunds_TourPackageRefundId",
                        column: x => x.TourPackageRefundId,
                        principalSchema: "tours",
                        principalTable: "TourPackageRefunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourPackageReschedules_TourPackageRescheduleId",
                        column: x => x.TourPackageRescheduleId,
                        principalSchema: "tours",
                        principalTable: "TourPackageReschedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourPackageReservations_TourPackageReservationId",
                        column: x => x.TourPackageReservationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourPackages_TourPackageId",
                        column: x => x.TourPackageId,
                        principalSchema: "tours",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_TourSchedules_TourScheduleId",
                        column: x => x.TourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageAuditEvents_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TenantId_EventType_CreatedAt",
                schema: "tours",
                table: "TourPackageAuditEvents",
                columns: new[] { "TenantId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TenantId_Severity_CreatedAt",
                schema: "tours",
                table: "TourPackageAuditEvents",
                columns: new[] { "TenantId", "Severity", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TenantId_TourId_CreatedAt",
                schema: "tours",
                table: "TourPackageAuditEvents",
                columns: new[] { "TenantId", "TourId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TenantId_TourPackageBookingId_CreatedAt",
                schema: "tours",
                table: "TourPackageAuditEvents",
                columns: new[] { "TenantId", "TourPackageBookingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TenantId_TourPackageRescheduleId_CreatedAt",
                schema: "tours",
                table: "TourPackageAuditEvents",
                columns: new[] { "TenantId", "TourPackageRescheduleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TenantId_TourPackageReservationId_CreatedAt",
                schema: "tours",
                table: "TourPackageAuditEvents",
                columns: new[] { "TenantId", "TourPackageReservationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourPackageBookingId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourPackageBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourPackageBookingItemId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourPackageBookingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourPackageCancellationId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourPackageCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourPackageId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourPackageRefundId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourPackageRefundId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourPackageRescheduleId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourPackageRescheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourPackageReservationId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourPackageReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageAuditEvents_TourScheduleId",
                schema: "tours",
                table: "TourPackageAuditEvents",
                column: "TourScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPackageAuditEvents",
                schema: "tours");
        }
    }
}
