using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_Tours_PackageReschedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourPackageReschedules",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTourPackageBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTourPackageReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetTourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetTourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetTourPackageReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetTourPackageBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceTourPackageCancellationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HoldStrategy = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestedPax = table.Column<int>(type: "int", nullable: false),
                    SourcePackageSubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetPackageSubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceDifferenceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HoldExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReasonText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OverrideNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageReschedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourPackageBookings_SourceTourPackageBookingId",
                        column: x => x.SourceTourPackageBookingId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourPackageBookings_TargetTourPackageBookingId",
                        column: x => x.TargetTourPackageBookingId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourPackageCancellations_SourceTourPackageCancellationId",
                        column: x => x.SourceTourPackageCancellationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourPackageReservations_SourceTourPackageReservationId",
                        column: x => x.SourceTourPackageReservationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourPackageReservations_TargetTourPackageReservationId",
                        column: x => x.TargetTourPackageReservationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourPackages_SourceTourPackageId",
                        column: x => x.SourceTourPackageId,
                        principalSchema: "tours",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourPackages_TargetTourPackageId",
                        column: x => x.TargetTourPackageId,
                        principalSchema: "tours",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourSchedules_SourceTourScheduleId",
                        column: x => x.SourceTourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_TourSchedules_TargetTourScheduleId",
                        column: x => x.TargetTourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageReschedules_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_SourceTourPackageBookingId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "SourceTourPackageBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_SourceTourPackageCancellationId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "SourceTourPackageCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_SourceTourPackageId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "SourceTourPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_SourceTourPackageReservationId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "SourceTourPackageReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_SourceTourScheduleId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "SourceTourScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TargetTourPackageBookingId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "TargetTourPackageBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TargetTourPackageId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "TargetTourPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TargetTourPackageReservationId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "TargetTourPackageReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TargetTourScheduleId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "TargetTourScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TenantId_Code",
                schema: "tours",
                table: "TourPackageReschedules",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TenantId_SourceTourPackageBookingId_ClientToken",
                schema: "tours",
                table: "TourPackageReschedules",
                columns: new[] { "TenantId", "SourceTourPackageBookingId", "ClientToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TenantId_SourceTourPackageBookingId_Status_IsDeleted",
                schema: "tours",
                table: "TourPackageReschedules",
                columns: new[] { "TenantId", "SourceTourPackageBookingId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TenantId_TargetTourPackageBookingId",
                schema: "tours",
                table: "TourPackageReschedules",
                columns: new[] { "TenantId", "TargetTourPackageBookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TenantId_TargetTourPackageReservationId",
                schema: "tours",
                table: "TourPackageReschedules",
                columns: new[] { "TenantId", "TargetTourPackageReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TenantId_TargetTourScheduleId_Status_HoldExpiresAt",
                schema: "tours",
                table: "TourPackageReschedules",
                columns: new[] { "TenantId", "TargetTourScheduleId", "Status", "HoldExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageReschedules_TourId",
                schema: "tours",
                table: "TourPackageReschedules",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPackageReschedules",
                schema: "tours");
        }
    }
}
