using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Infrastructure.Persistence.Commerce;

public sealed class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> b)
    {
        b.ToTable("CustomerOrders", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.GrossAmount).HasPrecision(18, 2);
        b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        b.Property(x => x.ServiceFeeAmount).HasPrecision(18, 2);
        b.Property(x => x.PlatformCommissionAmount).HasPrecision(18, 2);
        b.Property(x => x.TenantNetAmount).HasPrecision(18, 2);
        b.Property(x => x.PayableAmount).HasPrecision(18, 2);
        b.Property(x => x.RefundedAmount).HasPrecision(18, 2);
        b.Property(x => x.SettlementStatus).HasDefaultValue(CustomerSettlementStatus.Unsettled);
        b.Property(x => x.ContactFullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.ContactPhone).HasMaxLength(50).IsRequired();
        b.Property(x => x.ContactEmail).HasMaxLength(200).IsRequired();
        b.Property(x => x.CustomerNote).HasMaxLength(4000);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.FailureReason).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => x.OrderCode).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Status, x.PaymentStatus, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.ProductType, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.SettlementStatus, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.SourceBookingId });
        b.HasIndex(x => new { x.TenantId, x.SourceReservationId });
    }
}

public sealed class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> b)
    {
        b.ToTable("CustomerPayments", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.PaymentCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.ProviderInvoiceNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.ProviderOrderId).HasMaxLength(100);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.PaidAmount).HasPrecision(18, 2);
        b.Property(x => x.RefundedAmount).HasPrecision(18, 2);
        b.Property(x => x.RequestPayloadJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ProviderResponseJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.LastWebhookJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.FailureReason).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => x.PaymentCode).IsUnique();
        b.HasIndex(x => x.ProviderInvoiceNumber).IsUnique();
        b.HasIndex(x => new { x.OrderId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsDeleted });
    }
}

public sealed class CustomerTicketConfiguration : IEntityTypeConfiguration<CustomerTicket>
{
    public void Configure(EntityTypeBuilder<CustomerTicket> b)
    {
        b.ToTable("CustomerTickets", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.TicketCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Subtitle).HasMaxLength(500);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => x.TicketCode).IsUnique();
        b.HasIndex(x => x.OrderId).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsDeleted });
    }
}

public sealed class CustomerRefundRequestConfiguration : IEntityTypeConfiguration<CustomerRefundRequest>
{
    public void Configure(EntityTypeBuilder<CustomerRefundRequest> b)
    {
        b.ToTable("CustomerRefundRequests", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.RefundCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.RequestedAmount).HasPrecision(18, 2);
        b.Property(x => x.ApprovedAmount).HasPrecision(18, 2);
        b.Property(x => x.RefundedAmount).HasPrecision(18, 2);
        b.Property(x => x.ReasonCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.ReasonText).HasMaxLength(2000);
        b.Property(x => x.ReviewNote).HasMaxLength(2000);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => x.RefundCode).IsUnique();
        b.HasIndex(x => new { x.OrderId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsDeleted });
    }
}

public sealed class CustomerSavedPassengerConfiguration : IEntityTypeConfiguration<CustomerSavedPassenger>
{
    public void Configure(EntityTypeBuilder<CustomerSavedPassenger> b)
    {
        b.ToTable("CustomerSavedPassengers", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Gender).HasMaxLength(30);
        b.Property(x => x.NationalityCode).HasMaxLength(10);
        b.Property(x => x.IdNumber).HasMaxLength(100);
        b.Property(x => x.PassportNumber).HasMaxLength(100);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.PhoneNumber).HasMaxLength(50);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.UserId, x.FullName, x.DateOfBirth, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.IsDefault, x.IsDeleted });
    }
}

public sealed class CustomerWishlistItemConfiguration : IEntityTypeConfiguration<CustomerWishlistItem>
{
    public void Configure(EntityTypeBuilder<CustomerWishlistItem> b)
    {
        b.ToTable("CustomerWishlistItems", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.TargetSlug).HasMaxLength(200);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Subtitle).HasMaxLength(500);
        b.Property(x => x.LocationText).HasMaxLength(300);
        b.Property(x => x.PriceText).HasMaxLength(100);
        b.Property(x => x.PriceValue).HasPrecision(18, 2);
        b.Property(x => x.CurrencyCode).HasMaxLength(10);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.TargetUrl).HasMaxLength(500);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.UserId, x.ProductType, x.TargetId, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.ProductType, x.TargetSlug, x.IsDeleted });
    }
}

public sealed class CustomerNotificationConfiguration : IEntityTypeConfiguration<CustomerNotification>
{
    public void Configure(EntityTypeBuilder<CustomerNotification> b)
    {
        b.ToTable("CustomerNotifications", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.Category).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.ActionUrl).HasMaxLength(500);
        b.Property(x => x.ReferenceType).HasMaxLength(100);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.UserId, x.Status, x.IsDeleted, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.ReferenceType, x.ReferenceId });
    }
}

public sealed class CustomerVatInvoiceRequestConfiguration : IEntityTypeConfiguration<CustomerVatInvoiceRequest>
{
    public void Configure(EntityTypeBuilder<CustomerVatInvoiceRequest> b)
    {
        b.ToTable("CustomerVatInvoiceRequests", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.RequestCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.CompanyName).HasMaxLength(300).IsRequired();
        b.Property(x => x.TaxCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.CompanyAddress).HasMaxLength(500).IsRequired();
        b.Property(x => x.InvoiceEmail).HasMaxLength(200).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.InvoiceNumber).HasMaxLength(100);
        b.Property(x => x.PdfUrl).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => x.RequestCode).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.OrderId, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsDeleted });
    }
}

public sealed class CustomerAccountPreferenceConfiguration : IEntityTypeConfiguration<CustomerAccountPreference>
{
    public void Configure(EntityTypeBuilder<CustomerAccountPreference> b)
    {
        b.ToTable("CustomerAccountPreferences", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.LanguageCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.ThemeMode).HasMaxLength(20).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.UserId, x.IsDeleted }).IsUnique();
    }
}

public sealed class CustomerCheckoutDraftConfiguration : IEntityTypeConfiguration<CustomerCheckoutDraft>
{
    public void Configure(EntityTypeBuilder<CustomerCheckoutDraft> b)
    {
        b.ToTable("CustomerCheckoutDrafts", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.CheckoutKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Subtitle).HasMaxLength(500);
        b.Property(x => x.ResumeUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.UserId, x.CheckoutKey, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.LastActivityAt, x.IsDeleted });
    }
}

public sealed class CustomerRecentViewConfiguration : IEntityTypeConfiguration<CustomerRecentView>
{
    public void Configure(EntityTypeBuilder<CustomerRecentView> b)
    {
        b.ToTable("CustomerRecentViews", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.TargetSlug).HasMaxLength(200);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Subtitle).HasMaxLength(500);
        b.Property(x => x.LocationText).HasMaxLength(300);
        b.Property(x => x.PriceText).HasMaxLength(100);
        b.Property(x => x.PriceValue).HasPrecision(18, 2);
        b.Property(x => x.CurrencyCode).HasMaxLength(10);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.TargetUrl).HasMaxLength(1000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.UserId, x.ProductType, x.TargetId, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.ProductType, x.TargetSlug, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.ViewedAt, x.IsDeleted });
    }
}

public sealed class CustomerRecentSearchConfiguration : IEntityTypeConfiguration<CustomerRecentSearch>
{
    public void Configure(EntityTypeBuilder<CustomerRecentSearch> b)
    {
        b.ToTable("CustomerRecentSearches", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.SearchKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.QueryText).HasMaxLength(300);
        b.Property(x => x.SummaryText).HasMaxLength(500);
        b.Property(x => x.SearchUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.CriteriaJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.UserId, x.ProductType, x.SearchKey, x.IsDeleted });
        b.HasIndex(x => new { x.UserId, x.SearchedAt, x.IsDeleted });
    }
}

public sealed class CustomerSupportTicketConfiguration : IEntityTypeConfiguration<CustomerSupportTicket>
{
    public void Configure(EntityTypeBuilder<CustomerSupportTicket> b)
    {
        b.ToTable("CustomerSupportTickets", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.TicketCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        b.Property(x => x.Content).HasMaxLength(4000).IsRequired();
        b.Property(x => x.ContactEmail).HasMaxLength(200);
        b.Property(x => x.ContactPhone).HasMaxLength(50);
        b.Property(x => x.ResolutionNote).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => x.TicketCode).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Status, x.IsDeleted, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.OrderId, x.IsDeleted });
    }
}

public sealed class CustomerTenantPayoutAccountConfiguration : IEntityTypeConfiguration<CustomerTenantPayoutAccount>
{
    public void Configure(EntityTypeBuilder<CustomerTenantPayoutAccount> b)
    {
        b.ToTable("CustomerTenantPayoutAccounts", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.BankName).HasMaxLength(200).IsRequired();
        b.Property(x => x.AccountNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.AccountHolder).HasMaxLength(200).IsRequired();
        b.Property(x => x.BankBranch).HasMaxLength(200);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.IsDefault, x.IsDeleted });
    }
}

public sealed class CustomerSettlementBatchConfiguration : IEntityTypeConfiguration<CustomerSettlementBatch>
{
    public void Configure(EntityTypeBuilder<CustomerSettlementBatch> b)
    {
        b.ToTable("CustomerSettlementBatches", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.BatchCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasDefaultValue(CustomerSettlementBatchStatus.Draft);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.TotalGrossAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalCommissionAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalCommissionAdjustmentAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalTenantNetAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalRefundAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalNetPayoutAmount).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => x.BatchCode).IsUnique();
        b.HasIndex(x => new { x.PeriodYear, x.PeriodMonth, x.IsDeleted });
        b.HasIndex(x => new { x.Status, x.IsDeleted, x.CreatedAt });
    }
}

public sealed class CustomerSettlementBatchLineConfiguration : IEntityTypeConfiguration<CustomerSettlementBatchLine>
{
    public void Configure(EntityTypeBuilder<CustomerSettlementBatchLine> b)
    {
        b.ToTable("CustomerSettlementBatchLines", "commerce");
        b.HasKey(x => x.Id);

        b.Property(x => x.Status).HasDefaultValue(CustomerSettlementStatus.InSettlement);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.GrossAmount).HasPrecision(18, 2);
        b.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        b.Property(x => x.CommissionAdjustmentAmount).HasPrecision(18, 2);
        b.Property(x => x.TenantNetAmount).HasPrecision(18, 2);
        b.Property(x => x.RefundAmount).HasPrecision(18, 2);
        b.Property(x => x.NetPayoutAmount).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.BatchId, x.TenantId, x.IsDeleted });
        b.HasIndex(x => new { x.OrderId, x.IsDeleted });
        b.HasIndex(x => new { x.RefundRequestId, x.IsDeleted });
        b.HasIndex(x => new { x.Status, x.IsDeleted, x.CreatedAt });
    }
}
