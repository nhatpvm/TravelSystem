using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseCustomer_CommerceCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "commerce");

            migrationBuilder.CreateTable(
                name: "CustomerNotifications",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrders",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformCommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TenantNetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    TicketStatus = table.Column<int>(type: "int", nullable: false),
                    RefundStatus = table.Column<int>(type: "int", nullable: false),
                    ContactFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VatInvoiceRequested = table.Column<bool>(type: "bit", nullable: false),
                    CustomerNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TicketIssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_CustomerOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPayments",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderInvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequestPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastWebhookJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WebhookReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRefundRequests",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefundCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReasonText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_CustomerRefundRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSavedPassengers",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PassengerType = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    NationalityCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PassportNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSavedPassengers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerTickets",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TicketCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerVatInvoiceRequests",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InvoiceEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVatInvoiceRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerWishlistItems",
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
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CustomerWishlistItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotifications_TenantId_ReferenceType_ReferenceId",
                schema: "commerce",
                table: "CustomerNotifications",
                columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotifications_UserId_Status_IsDeleted_CreatedAt",
                schema: "commerce",
                table: "CustomerNotifications",
                columns: new[] { "UserId", "Status", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_OrderCode",
                schema: "commerce",
                table: "CustomerOrders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_TenantId_ProductType_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerOrders",
                columns: new[] { "TenantId", "ProductType", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_TenantId_SourceBookingId",
                schema: "commerce",
                table: "CustomerOrders",
                columns: new[] { "TenantId", "SourceBookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_TenantId_SourceReservationId",
                schema: "commerce",
                table: "CustomerOrders",
                columns: new[] { "TenantId", "SourceReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_UserId_Status_PaymentStatus_IsDeleted",
                schema: "commerce",
                table: "CustomerOrders",
                columns: new[] { "UserId", "Status", "PaymentStatus", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_OrderId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerPayments",
                columns: new[] { "OrderId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_PaymentCode",
                schema: "commerce",
                table: "CustomerPayments",
                column: "PaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_ProviderInvoiceNumber",
                schema: "commerce",
                table: "CustomerPayments",
                column: "ProviderInvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_TenantId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerPayments",
                columns: new[] { "TenantId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_UserId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerPayments",
                columns: new[] { "UserId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefundRequests_OrderId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerRefundRequests",
                columns: new[] { "OrderId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefundRequests_RefundCode",
                schema: "commerce",
                table: "CustomerRefundRequests",
                column: "RefundCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefundRequests_TenantId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerRefundRequests",
                columns: new[] { "TenantId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefundRequests_UserId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerRefundRequests",
                columns: new[] { "UserId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSavedPassengers_UserId_FullName_DateOfBirth_IsDeleted",
                schema: "commerce",
                table: "CustomerSavedPassengers",
                columns: new[] { "UserId", "FullName", "DateOfBirth", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSavedPassengers_UserId_IsDefault_IsDeleted",
                schema: "commerce",
                table: "CustomerSavedPassengers",
                columns: new[] { "UserId", "IsDefault", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTickets_OrderId",
                schema: "commerce",
                table: "CustomerTickets",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTickets_TenantId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerTickets",
                columns: new[] { "TenantId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTickets_TicketCode",
                schema: "commerce",
                table: "CustomerTickets",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTickets_UserId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerTickets",
                columns: new[] { "UserId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVatInvoiceRequests_OrderId_IsDeleted",
                schema: "commerce",
                table: "CustomerVatInvoiceRequests",
                columns: new[] { "OrderId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVatInvoiceRequests_RequestCode",
                schema: "commerce",
                table: "CustomerVatInvoiceRequests",
                column: "RequestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVatInvoiceRequests_TenantId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerVatInvoiceRequests",
                columns: new[] { "TenantId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVatInvoiceRequests_UserId_Status_IsDeleted",
                schema: "commerce",
                table: "CustomerVatInvoiceRequests",
                columns: new[] { "UserId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerWishlistItems_UserId_ProductType_TargetId_IsDeleted",
                schema: "commerce",
                table: "CustomerWishlistItems",
                columns: new[] { "UserId", "ProductType", "TargetId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerWishlistItems_UserId_ProductType_TargetSlug_IsDeleted",
                schema: "commerce",
                table: "CustomerWishlistItems",
                columns: new[] { "UserId", "ProductType", "TargetSlug", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerNotifications",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerOrders",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerPayments",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerRefundRequests",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerSavedPassengers",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerTickets",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerVatInvoiceRequests",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "CustomerWishlistItems",
                schema: "commerce");
        }
    }
}
