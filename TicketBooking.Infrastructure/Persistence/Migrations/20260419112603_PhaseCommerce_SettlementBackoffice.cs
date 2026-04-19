using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseCommerce_SettlementBackoffice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SettledAt",
                schema: "commerce",
                table: "CustomerOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementBatchId",
                schema: "commerce",
                table: "CustomerOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettlementStatus",
                schema: "commerce",
                table: "CustomerOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CustomerSettlementBatches",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    PeriodMonth = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TotalGrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommissionAdjustmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTenantNetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalNetPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TenantCount = table.Column<int>(type: "int", nullable: false),
                    LineCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSettlementBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSettlementBatchLines",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefundRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAdjustmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TenantNetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSettlementBatchLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerTenantPayoutAccounts",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountHolder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BankBranch = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTenantPayoutAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_TenantId_SettlementStatus_IsDeleted",
                schema: "commerce",
                table: "CustomerOrders",
                columns: new[] { "TenantId", "SettlementStatus", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettlementBatches_BatchCode",
                schema: "commerce",
                table: "CustomerSettlementBatches",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettlementBatches_PeriodYear_PeriodMonth_IsDeleted",
                schema: "commerce",
                table: "CustomerSettlementBatches",
                columns: new[] { "PeriodYear", "PeriodMonth", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettlementBatches_Status_IsDeleted_CreatedAt",
                schema: "commerce",
                table: "CustomerSettlementBatches",
                columns: new[] { "Status", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettlementBatchLines_BatchId_TenantId_IsDeleted",
                schema: "commerce",
                table: "CustomerSettlementBatchLines",
                columns: new[] { "BatchId", "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettlementBatchLines_OrderId_IsDeleted",
                schema: "commerce",
                table: "CustomerSettlementBatchLines",
                columns: new[] { "OrderId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettlementBatchLines_RefundRequestId_IsDeleted",
                schema: "commerce",
                table: "CustomerSettlementBatchLines",
                columns: new[] { "RefundRequestId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettlementBatchLines_Status_IsDeleted_CreatedAt",
                schema: "commerce",
                table: "CustomerSettlementBatchLines",
                columns: new[] { "Status", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTenantPayoutAccounts_TenantId_IsDefault_IsDeleted",
                schema: "commerce",
                table: "CustomerTenantPayoutAccounts",
                columns: new[] { "TenantId", "IsDefault", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTenantPayoutAccounts_TenantId_IsDeleted",
                schema: "commerce",
                table: "CustomerTenantPayoutAccounts",
                columns: new[] { "TenantId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSettlementBatches",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerSettlementBatchLines",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerTenantPayoutAccounts",
                schema: "commerce");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_TenantId_SettlementStatus_IsDeleted",
                schema: "commerce",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                schema: "commerce",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "SettlementBatchId",
                schema: "commerce",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "SettlementStatus",
                schema: "commerce",
                table: "CustomerOrders");
        }
    }
}
