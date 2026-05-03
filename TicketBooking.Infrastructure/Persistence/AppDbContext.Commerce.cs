using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;
using TicketBooking.Infrastructure.Persistence.Commerce;

namespace TicketBooking.Infrastructure.Persistence;

public sealed partial class AppDbContext
{
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<CustomerTicket> CustomerTickets => Set<CustomerTicket>();
    public DbSet<CustomerRefundRequest> CustomerRefundRequests => Set<CustomerRefundRequest>();
    public DbSet<CustomerSavedPassenger> CustomerSavedPassengers => Set<CustomerSavedPassenger>();
    public DbSet<CustomerWishlistItem> CustomerWishlistItems => Set<CustomerWishlistItem>();
    public DbSet<CustomerNotification> CustomerNotifications => Set<CustomerNotification>();
    public DbSet<CustomerVatInvoiceRequest> CustomerVatInvoiceRequests => Set<CustomerVatInvoiceRequest>();
    public DbSet<CustomerAccountPreference> CustomerAccountPreferences => Set<CustomerAccountPreference>();
    public DbSet<CustomerCheckoutDraft> CustomerCheckoutDrafts => Set<CustomerCheckoutDraft>();
    public DbSet<CustomerRecentView> CustomerRecentViews => Set<CustomerRecentView>();
    public DbSet<CustomerRecentSearch> CustomerRecentSearches => Set<CustomerRecentSearch>();
    public DbSet<CustomerSupportTicket> CustomerSupportTickets => Set<CustomerSupportTicket>();
    public DbSet<CustomerTenantPayoutAccount> CustomerTenantPayoutAccounts => Set<CustomerTenantPayoutAccount>();
    public DbSet<CustomerSettlementBatch> CustomerSettlementBatches => Set<CustomerSettlementBatch>();
    public DbSet<CustomerSettlementBatchLine> CustomerSettlementBatchLines => Set<CustomerSettlementBatchLine>();
    public DbSet<PromotionCampaign> PromotionCampaigns => Set<PromotionCampaign>();
    public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();
}

public static class AppDbContextCommerceModel
{
    public static void ApplyCommerceModel(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(
            typeof(CustomerOrderConfiguration).Assembly,
            type => type.Namespace == typeof(CustomerOrderConfiguration).Namespace);
    }
}
