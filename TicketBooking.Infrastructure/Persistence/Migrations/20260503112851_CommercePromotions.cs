using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CommercePromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromotionCampaigns",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerScope = table.Column<int>(type: "int", nullable: false),
                    ProductScope = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    GlobalUsageLimit = table.Column<int>(type: "int", nullable: true),
                    PerUserUsageLimit = table.Column<int>(type: "int", nullable: true),
                    PerTenantUsageLimit = table.Column<int>(type: "int", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RedemptionCount = table.Column<int>(type: "int", nullable: false),
                    DiscountGrantedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevenueAttributedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiresCode = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PromotionCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRedemptions",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionCampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PromotionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_PromotionRedemptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_Code",
                schema: "commerce",
                table: "PromotionCampaigns",
                column: "Code",
                unique: true,
                filter: "[TenantId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_OwnerScope_Status_StartsAt_EndsAt_IsDeleted",
                schema: "commerce",
                table: "PromotionCampaigns",
                columns: new[] { "OwnerScope", "Status", "StartsAt", "EndsAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_ProductScope",
                schema: "commerce",
                table: "PromotionCampaigns",
                column: "ProductScope");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_TenantId_Code",
                schema: "commerce",
                table: "PromotionCampaigns",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_TenantId_Status_IsDeleted",
                schema: "commerce",
                table: "PromotionCampaigns",
                columns: new[] { "TenantId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_OrderId_PromotionCampaignId_IsDeleted",
                schema: "commerce",
                table: "PromotionRedemptions",
                columns: new[] { "OrderId", "PromotionCampaignId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_PromotionCampaignId_Status_IsDeleted",
                schema: "commerce",
                table: "PromotionRedemptions",
                columns: new[] { "PromotionCampaignId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_TenantId_ProductType_Status_RedeemedAt",
                schema: "commerce",
                table: "PromotionRedemptions",
                columns: new[] { "TenantId", "ProductType", "Status", "RedeemedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_UserId_PromotionCampaignId_IsDeleted",
                schema: "commerce",
                table: "PromotionRedemptions",
                columns: new[] { "UserId", "PromotionCampaignId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionCampaigns",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "PromotionRedemptions",
                schema: "commerce");
        }
    }
}
