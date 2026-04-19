using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseW_CustomerJourneyWa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerCheckoutDrafts",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    CheckoutKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResumeUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResumeCount = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCheckoutDrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRecentSearches",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    SearchKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QueryText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SummaryText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SearchUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SearchCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRecentSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRecentViews",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LocationText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PriceText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PriceValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRecentViews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCheckoutDrafts_UserId_CheckoutKey_IsDeleted",
                schema: "commerce",
                table: "CustomerCheckoutDrafts",
                columns: new[] { "UserId", "CheckoutKey", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCheckoutDrafts_UserId_LastActivityAt_IsDeleted",
                schema: "commerce",
                table: "CustomerCheckoutDrafts",
                columns: new[] { "UserId", "LastActivityAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRecentSearches_UserId_ProductType_SearchKey_IsDeleted",
                schema: "commerce",
                table: "CustomerRecentSearches",
                columns: new[] { "UserId", "ProductType", "SearchKey", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRecentSearches_UserId_SearchedAt_IsDeleted",
                schema: "commerce",
                table: "CustomerRecentSearches",
                columns: new[] { "UserId", "SearchedAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRecentViews_UserId_ProductType_TargetId_IsDeleted",
                schema: "commerce",
                table: "CustomerRecentViews",
                columns: new[] { "UserId", "ProductType", "TargetId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRecentViews_UserId_ProductType_TargetSlug_IsDeleted",
                schema: "commerce",
                table: "CustomerRecentViews",
                columns: new[] { "UserId", "ProductType", "TargetSlug", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRecentViews_UserId_ViewedAt_IsDeleted",
                schema: "commerce",
                table: "CustomerRecentViews",
                columns: new[] { "UserId", "ViewedAt", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerCheckoutDrafts",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerRecentSearches",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerRecentViews",
                schema: "commerce");
        }
    }
}
