using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_Tours_PackageCancelRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourPackageCancellations",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsAdminOverride = table.Column<bool>(type: "bit", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PolicyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PolicyName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReasonText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OverrideNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BookingSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecisionSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageCancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageCancellations_TourPackageBookings_TourPackageBookingId",
                        column: x => x.TourPackageBookingId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageCancellationItems",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageCancellationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageBookingItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GrossLineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PolicyRuleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingItemSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_TourPackageCancellationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageCancellationItems_TourPackageBookingItems_TourPackageBookingItemId",
                        column: x => x.TourPackageBookingItemId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageCancellationItems_TourPackageCancellations_TourPackageCancellationId",
                        column: x => x.TourPackageCancellationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageRefunds",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageBookingItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageCancellationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageCancellationItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GrossLineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebhookState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastProviderError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PreparedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageRefunds_TourPackageBookingItems_TourPackageBookingItemId",
                        column: x => x.TourPackageBookingItemId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageRefunds_TourPackageBookings_TourPackageBookingId",
                        column: x => x.TourPackageBookingId,
                        principalSchema: "tours",
                        principalTable: "TourPackageBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageRefunds_TourPackageCancellationItems_TourPackageCancellationItemId",
                        column: x => x.TourPackageCancellationItemId,
                        principalSchema: "tours",
                        principalTable: "TourPackageCancellationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageRefunds_TourPackageCancellations_TourPackageCancellationId",
                        column: x => x.TourPackageCancellationId,
                        principalSchema: "tours",
                        principalTable: "TourPackageCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageRefundAttempts",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageRefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebhookState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastProviderError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageRefundAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageRefundAttempts_TourPackageRefunds_TourPackageRefundId",
                        column: x => x.TourPackageRefundId,
                        principalSchema: "tours",
                        principalTable: "TourPackageRefunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellationItems_TenantId_TourPackageBookingItemId_Status",
                schema: "tours",
                table: "TourPackageCancellationItems",
                columns: new[] { "TenantId", "TourPackageBookingItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellationItems_TenantId_TourPackageCancellationId_Status",
                schema: "tours",
                table: "TourPackageCancellationItems",
                columns: new[] { "TenantId", "TourPackageCancellationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellationItems_TourPackageBookingItemId",
                schema: "tours",
                table: "TourPackageCancellationItems",
                column: "TourPackageBookingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellationItems_TourPackageCancellationId",
                schema: "tours",
                table: "TourPackageCancellationItems",
                column: "TourPackageCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellations_TenantId_RequestedByUserId_Status_IsDeleted",
                schema: "tours",
                table: "TourPackageCancellations",
                columns: new[] { "TenantId", "RequestedByUserId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellations_TenantId_TourId_TourScheduleId_Status_IsDeleted",
                schema: "tours",
                table: "TourPackageCancellations",
                columns: new[] { "TenantId", "TourId", "TourScheduleId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellations_TenantId_TourPackageBookingId_Status",
                schema: "tours",
                table: "TourPackageCancellations",
                columns: new[] { "TenantId", "TourPackageBookingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageCancellations_TourPackageBookingId",
                schema: "tours",
                table: "TourPackageCancellations",
                column: "TourPackageBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefundAttempts_TenantId_Provider_Status_IsDeleted",
                schema: "tours",
                table: "TourPackageRefundAttempts",
                columns: new[] { "TenantId", "Provider", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefundAttempts_TenantId_TourPackageRefundId_Status",
                schema: "tours",
                table: "TourPackageRefundAttempts",
                columns: new[] { "TenantId", "TourPackageRefundId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefundAttempts_TourPackageRefundId",
                schema: "tours",
                table: "TourPackageRefundAttempts",
                column: "TourPackageRefundId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TenantId_Provider_Status_IsDeleted",
                schema: "tours",
                table: "TourPackageRefunds",
                columns: new[] { "TenantId", "Provider", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TenantId_TourPackageBookingId_Status",
                schema: "tours",
                table: "TourPackageRefunds",
                columns: new[] { "TenantId", "TourPackageBookingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TenantId_TourPackageBookingItemId_Status",
                schema: "tours",
                table: "TourPackageRefunds",
                columns: new[] { "TenantId", "TourPackageBookingItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TenantId_TourPackageCancellationItemId",
                schema: "tours",
                table: "TourPackageRefunds",
                columns: new[] { "TenantId", "TourPackageCancellationItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TourPackageBookingId",
                schema: "tours",
                table: "TourPackageRefunds",
                column: "TourPackageBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TourPackageBookingItemId",
                schema: "tours",
                table: "TourPackageRefunds",
                column: "TourPackageBookingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TourPackageCancellationId",
                schema: "tours",
                table: "TourPackageRefunds",
                column: "TourPackageCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageRefunds_TourPackageCancellationItemId",
                schema: "tours",
                table: "TourPackageRefunds",
                column: "TourPackageCancellationItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPackageRefundAttempts",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPackageRefunds",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPackageCancellationItems",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPackageCancellations",
                schema: "tours");
        }
    }
}
