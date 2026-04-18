using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_Tours_PackageFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourPackages",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    AutoRepriceBeforeConfirm = table.Column<bool>(type: "bit", nullable: false),
                    HoldStrategy = table.Column<int>(type: "int", nullable: false),
                    PricingRuleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackages_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageComponents",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ComponentType = table.Column<int>(type: "int", nullable: false),
                    SelectionMode = table.Column<int>(type: "int", nullable: false),
                    MinSelect = table.Column<int>(type: "int", nullable: true),
                    MaxSelect = table.Column<int>(type: "int", nullable: true),
                    DayOffsetFromDeparture = table.Column<int>(type: "int", nullable: true),
                    NightCount = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageComponents_TourPackages_TourPackageId",
                        column: x => x.TourPackageId,
                        principalSchema: "tours",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageComponentOptions",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    BindingMode = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SearchTemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PricingMode = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CostOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MarkupPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    MarkupAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityMode = table.Column<int>(type: "int", nullable: false),
                    DefaultQuantity = table.Column<int>(type: "int", nullable: false),
                    MinQuantity = table.Column<int>(type: "int", nullable: true),
                    MaxQuantity = table.Column<int>(type: "int", nullable: true),
                    IsDefaultSelected = table.Column<bool>(type: "bit", nullable: false),
                    IsFallback = table.Column<bool>(type: "bit", nullable: false),
                    IsDynamicCandidate = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageComponentOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageComponentOptions_TourPackageComponents_TourPackageComponentId",
                        column: x => x.TourPackageComponentId,
                        principalSchema: "tours",
                        principalTable: "TourPackageComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageScheduleOptionOverrides",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageComponentOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CostOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BoundSourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BoundSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuleOverrideJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageScheduleOptionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageScheduleOptionOverrides_TourPackageComponentOptions_TourPackageComponentOptionId",
                        column: x => x.TourPackageComponentOptionId,
                        principalSchema: "tours",
                        principalTable: "TourPackageComponentOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourPackageScheduleOptionOverrides_TourSchedules_TourScheduleId",
                        column: x => x.TourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponentOptions_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourPackageComponentOptions",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponentOptions_TenantId_SourceType_SourceEntityId",
                schema: "tours",
                table: "TourPackageComponentOptions",
                columns: new[] { "TenantId", "SourceType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponentOptions_TenantId_TourPackageComponentId_Code",
                schema: "tours",
                table: "TourPackageComponentOptions",
                columns: new[] { "TenantId", "TourPackageComponentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponentOptions_TenantId_TourPackageComponentId_SortOrder",
                schema: "tours",
                table: "TourPackageComponentOptions",
                columns: new[] { "TenantId", "TourPackageComponentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponentOptions_TourPackageComponentId",
                schema: "tours",
                table: "TourPackageComponentOptions",
                column: "TourPackageComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponents_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourPackageComponents",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponents_TenantId_TourPackageId_Code",
                schema: "tours",
                table: "TourPackageComponents",
                columns: new[] { "TenantId", "TourPackageId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponents_TenantId_TourPackageId_ComponentType",
                schema: "tours",
                table: "TourPackageComponents",
                columns: new[] { "TenantId", "TourPackageId", "ComponentType" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponents_TenantId_TourPackageId_SortOrder",
                schema: "tours",
                table: "TourPackageComponents",
                columns: new[] { "TenantId", "TourPackageId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageComponents_TourPackageId",
                schema: "tours",
                table: "TourPackageComponents",
                column: "TourPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_TenantId_Status_IsActive_IsDeleted",
                schema: "tours",
                table: "TourPackages",
                columns: new[] { "TenantId", "Status", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_TenantId_TourId_Code",
                schema: "tours",
                table: "TourPackages",
                columns: new[] { "TenantId", "TourId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_TenantId_TourId_IsDefault",
                schema: "tours",
                table: "TourPackages",
                columns: new[] { "TenantId", "TourId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_TourId",
                schema: "tours",
                table: "TourPackages",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageScheduleOptionOverrides_TenantId_BoundSourceEntityId",
                schema: "tours",
                table: "TourPackageScheduleOptionOverrides",
                columns: new[] { "TenantId", "BoundSourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageScheduleOptionOverrides_TenantId_TourScheduleId_Status_IsActive_IsDeleted",
                schema: "tours",
                table: "TourPackageScheduleOptionOverrides",
                columns: new[] { "TenantId", "TourScheduleId", "Status", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageScheduleOptionOverrides_TenantId_TourScheduleId_TourPackageComponentOptionId",
                schema: "tours",
                table: "TourPackageScheduleOptionOverrides",
                columns: new[] { "TenantId", "TourScheduleId", "TourPackageComponentOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageScheduleOptionOverrides_TourPackageComponentOptionId",
                schema: "tours",
                table: "TourPackageScheduleOptionOverrides",
                column: "TourPackageComponentOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageScheduleOptionOverrides_TourScheduleId",
                schema: "tours",
                table: "TourPackageScheduleOptionOverrides",
                column: "TourScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPackageScheduleOptionOverrides",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPackageComponentOptions",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPackageComponents",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPackages",
                schema: "tours");
        }
    }
}
