using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Tenancy;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Admin;

public sealed class AdminOpsService
{
    private const int AuditSourceLimit = 120;

    private readonly AppDbContext _db;
    private readonly PartnerOnboardingStore _onboardingStore;

    public AdminOpsService(AppDbContext db, PartnerOnboardingStore onboardingStore)
    {
        _db = db;
        _onboardingStore = onboardingStore;
    }

    public async Task<AdminOpsOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var orders = _db.CustomerOrders.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted);
        var payments = _db.CustomerPayments.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted);
        var refunds = _db.CustomerRefundRequests.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted);
        var supportTickets = _db.CustomerSupportTickets.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted);
        var tenants = _db.Tenants.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted);
        var onboarding = await _onboardingStore.ListAsync(ct: ct);

        var bookingCount = await orders.CountAsync(ct);
        var paidBookingCount = await orders.CountAsync(x =>
            x.PaymentStatus == CustomerPaymentStatus.Paid ||
            x.Status == CustomerOrderStatus.Paid ||
            x.Status == CustomerOrderStatus.TicketIssued ||
            x.Status == CustomerOrderStatus.Completed,
            ct);

        var paymentCount = await payments.CountAsync(ct);
        var failedPaymentCount = await payments.CountAsync(x =>
            x.Status == CustomerPaymentStatus.Failed ||
            x.Status == CustomerPaymentStatus.Cancelled ||
            x.Status == CustomerPaymentStatus.Expired,
            ct);

        var overview = new AdminOpsOverviewDto
        {
            BookingCount = bookingCount,
            PaidBookingCount = paidBookingCount,
            GmvAmount = await orders
                .Where(x => x.PaymentStatus == CustomerPaymentStatus.Paid)
                .SumAsync(x => x.PayableAmount, ct),
            FailedPaymentCount = failedPaymentCount,
            FailedPaymentRate = paymentCount == 0 ? 0m : Math.Round(failedPaymentCount * 100m / paymentCount, 2),
            PendingTenantCount = await tenants.CountAsync(x => x.Status == TenantStatus.Suspended, ct),
            PendingOnboardingCount = onboarding.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
            OpenSupportTicketCount = await supportTickets.CountAsync(x =>
                x.Status == CustomerSupportTicketStatus.Open ||
                x.Status == CustomerSupportTicketStatus.Processing,
                ct),
            PendingRefundCount = await refunds.CountAsync(x =>
                x.Status == CustomerRefundStatus.Requested ||
                x.Status == CustomerRefundStatus.UnderReview ||
                x.Status == CustomerRefundStatus.Approved ||
                x.Status == CustomerRefundStatus.Processing,
                ct),
            LastUpdatedAt = DateTimeOffset.Now
        };

        overview.RecentActivities = (await ListAuditEventsAsync(null, 1, 8, ct)).Items;
        return overview;
    }

    public async Task<AdminOpsAuditEventListResponse> ListAuditEventsAsync(
        string? q,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 100 : pageSize;

        var events = new List<AuditEventSeed>();

        await AddOrderEventsAsync(events, ct);
        await AddPaymentEventsAsync(events, ct);
        await AddRefundEventsAsync(events, ct);
        await AddSupportEventsAsync(events, ct);
        await AddSettlementEventsAsync(events, ct);
        await AddTenantEventsAsync(events, ct);
        await AddNotificationEventsAsync(events, ct);
        await AddPromoEventsAsync(events, ct);
        await AddOnboardingEventsAsync(events, ct);

        var tenantIds = events.Where(x => x.TenantId.HasValue).Select(x => x.TenantId!.Value).Distinct().ToArray();
        var actorIds = events.Where(x => x.ActorUserId.HasValue).Select(x => x.ActorUserId!.Value).Distinct().ToArray();

        var tenantNames = tenantIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _db.Tenants.IgnoreQueryFilters().AsNoTracking()
                .Where(x => tenantIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => $"{x.Code} - {x.Name}", ct);

        var actorNames = actorIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(x => actorIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.FullName ?? x.Email ?? x.UserName ?? x.Id.ToString(),
                    ct);

        var items = events
            .Select(x => MapAuditEvent(x, tenantNames, actorNames))
            .Where(x => MatchesAuditQuery(x, q))
            .OrderByDescending(x => x.OccurredAt)
            .ThenBy(x => x.EntityCode)
            .ToList();

        var paged = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new AdminOpsAuditEventListResponse
        {
            Summary = new AdminOpsAuditSummaryDto
            {
                TotalCount = items.Count,
                BookingEvents = items.Count(x => x.EntityType == "Booking"),
                PaymentEvents = items.Count(x => x.EntityType == "Payment"),
                RefundEvents = items.Count(x => x.EntityType == "Refund"),
                TenantEvents = items.Count(x => x.EntityType == "Tenant"),
                SupportEvents = items.Count(x => x.EntityType == "SupportTicket"),
                SettlementEvents = items.Count(x => x.EntityType == "SettlementBatch"),
                NotificationEvents = items.Count(x => x.EntityType == "Notification"),
                PromoEvents = items.Count(x => x.EntityType == "HotelPromoOverride" || x.EntityType == "PromotionCampaign"),
                OnboardingEvents = items.Count(x => x.EntityType == "TenantOnboarding")
            },
            Items = paged
        };
    }

    public async Task<AdminOpsOutboxListResponse> ListOutboxMessagesAsync(
        string? q,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 100 : pageSize;

        var query =
            from notification in _db.CustomerNotifications.IgnoreQueryFilters().AsNoTracking()
            join user in _db.Users.AsNoTracking() on notification.UserId equals user.Id
            join tenant in _db.Tenants.IgnoreQueryFilters().AsNoTracking() on notification.TenantId equals tenant.Id into tenantJoin
            from tenant in tenantJoin.DefaultIfEmpty()
            where !notification.IsDeleted
            select new
            {
                Notification = notification,
                RecipientName = user.FullName ?? user.Email ?? user.UserName ?? user.Id.ToString(),
                TenantName = tenant == null ? null : tenant.Name
            };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Notification.Title.Contains(keyword) ||
                x.Notification.Body.Contains(keyword) ||
                x.Notification.Category.Contains(keyword) ||
                x.RecipientName.Contains(keyword));
        }

        var rows = await query
            .OrderByDescending(x => x.Notification.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminOpsOutboxMessageDto
            {
                Id = x.Notification.Id,
                CreatedAt = x.Notification.CreatedAt,
                Status = x.Notification.Status.ToString(),
                Category = x.Notification.Category,
                Title = x.Notification.Title,
                Body = x.Notification.Body,
                ActionUrl = x.Notification.ActionUrl,
                ReferenceType = x.Notification.ReferenceType,
                ReferenceId = x.Notification.ReferenceId,
                UserId = x.Notification.UserId,
                RecipientName = x.RecipientName,
                TenantId = x.Notification.TenantId,
                TenantName = x.TenantName,
                ReadAt = x.Notification.ReadAt
            })
            .ToList();

        return new AdminOpsOutboxListResponse
        {
            Summary = new AdminOpsOutboxSummaryDto
            {
                TotalCount = rows.Count,
                InAppCount = rows.Count,
                UnreadCount = rows.Count(x => x.Notification.Status == CustomerNotificationStatus.Unread),
                ReadCount = rows.Count(x => x.Notification.Status == CustomerNotificationStatus.Read),
                TenantScopedCount = rows.Count(x => x.Notification.TenantId.HasValue)
            },
            Items = items
        };
    }

    public async Task<AdminOpsPromoReadinessDto> GetPromoReadinessAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTimeOffset.Now;

        var rows = await (
            from promo in _db.Set<PromoRateOverride>().IgnoreQueryFilters().AsNoTracking()
            join tenant in _db.Tenants.IgnoreQueryFilters().AsNoTracking() on promo.TenantId equals tenant.Id
            where !promo.IsDeleted && !tenant.IsDeleted
            select new
            {
                Promo = promo,
                TenantCode = tenant.Code,
                TenantName = tenant.Name
            })
            .OrderByDescending(x => x.Promo.CreatedAt)
            .ToListAsync(ct);

        var tenants = rows
            .GroupBy(x => new { x.Promo.TenantId, x.TenantCode, x.TenantName })
            .Select(x => new AdminOpsPromoTenantDto
            {
                TenantId = x.Key.TenantId,
                TenantCode = x.Key.TenantCode,
                TenantName = x.Key.TenantName,
                OverrideCount = x.Count(),
                ActiveCount = x.Count(y => y.Promo.IsActive && y.Promo.StartDate <= today && y.Promo.EndDate >= today),
                FirstStartDate = x.Min(y => y.Promo.StartDate),
                LastEndDate = x.Max(y => y.Promo.EndDate),
                LastUpdatedAt = x.Max(y => y.Promo.UpdatedAt ?? y.Promo.CreatedAt)
            })
            .OrderByDescending(x => x.ActiveCount)
            .ThenByDescending(x => x.OverrideCount)
            .Take(50)
            .ToList();

        var campaigns = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);

        var platformCampaigns = campaigns.Where(x => x.OwnerScope == PromotionOwnerScope.Platform).ToList();
        var tenantCampaigns = campaigns.Where(x => x.OwnerScope == PromotionOwnerScope.Tenant).ToList();
        var productPromoCount = campaigns.Count(x =>
            (x.ProductScope & (PromotionProductScope.Bus | PromotionProductScope.Train | PromotionProductScope.Flight | PromotionProductScope.Tour)) != 0);

        var redemptionSummary = await _db.PromotionRedemptions.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status != PromotionRedemptionStatus.Cancelled)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Discount = g.Sum(x => x.DiscountAmount),
                Revenue = g.Sum(x => x.OrderAmount)
            })
            .FirstOrDefaultAsync(ct);

        return new AdminOpsPromoReadinessDto
        {
            Summary = new AdminOpsPromoSummaryDto
            {
                HotelOverrideCount = rows.Count,
                ActiveHotelOverrideCount = rows.Count(x => x.Promo.IsActive && x.Promo.StartDate <= today && x.Promo.EndDate >= today),
                UpcomingHotelOverrideCount = rows.Count(x => x.Promo.IsActive && x.Promo.StartDate > today),
                ExpiredHotelOverrideCount = rows.Count(x => x.Promo.EndDate < today),
                PlatformPromotionCount = platformCampaigns.Count,
                ActivePlatformPromotionCount = platformCampaigns.Count(x => IsCampaignActive(x, now)),
                TenantPromotionCount = tenantCampaigns.Count,
                ActiveTenantPromotionCount = tenantCampaigns.Count(x => IsCampaignActive(x, now)),
                ProductPromotionCount = productPromoCount,
                PromotionRedemptionCount = redemptionSummary?.Count ?? 0,
                PromotionDiscountGrantedAmount = redemptionSummary?.Discount ?? 0m,
                PromotionRevenueAttributedAmount = redemptionSummary?.Revenue ?? 0m,
                TenantCount = tenants.Count
            },
            Tenants = tenants,
            Readiness =
            {
                new AdminOpsReadinessItemDto
                {
                    Area = "Hotel promo overrides",
                    Status = "Ready",
                    Note = "Da co API va du lieu that theo tenant/rate plan.",
                    ActionUrl = "/admin/hotels/promo-overrides"
                },
                new AdminOpsReadinessItemDto
                {
                    Area = "Platform promo engine",
                    Status = "Ready",
                    Note = "Da co entity/API coupon toan san, usage limit, scope theo module/tenant va so lieu ROI.",
                    ActionUrl = "/admin/promos"
                },
                new AdminOpsReadinessItemDto
                {
                    Area = "Bus/train/flight/tour promo",
                    Status = "Ready",
                    Note = "Da co API tenant promotions cho bus/train/flight/hotel/tour; tenant chi quan ly scope cua chinh minh.",
                    ActionUrl = "/tenant/promos"
                }
            }
        };
    }

    private async Task AddOrderEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.CustomerOrders.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.UserId,
                x.OrderCode,
                x.Status,
                x.PaymentStatus,
                x.CreatedAt,
                x.CreatedByUserId,
                x.PaidAt,
                x.TicketIssuedAt,
                x.CompletedAt,
                x.CancelledAt
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            Add(events, row.CreatedAt, row.CreatedByUserId ?? row.UserId, "BOOKING_CREATED", "Booking", row.Id.ToString(), row.OrderCode, row.TenantId, "Domain", "Info", $"Tao booking {row.OrderCode}.");
            if (row.PaidAt.HasValue)
                Add(events, row.PaidAt.Value, row.UserId, "BOOKING_PAID", "Booking", row.Id.ToString(), row.OrderCode, row.TenantId, "Domain", "Success", $"Booking {row.OrderCode} da thanh toan.");
            if (row.TicketIssuedAt.HasValue)
                Add(events, row.TicketIssuedAt.Value, row.UserId, "TICKET_ISSUED", "Booking", row.Id.ToString(), row.OrderCode, row.TenantId, "Domain", "Success", $"Booking {row.OrderCode} da xuat ve.");
            if (row.CompletedAt.HasValue)
                Add(events, row.CompletedAt.Value, row.UserId, "BOOKING_COMPLETED", "Booking", row.Id.ToString(), row.OrderCode, row.TenantId, "Domain", "Success", $"Booking {row.OrderCode} da hoan tat.");
            if (row.CancelledAt.HasValue)
                Add(events, row.CancelledAt.Value, row.UserId, "BOOKING_CANCELLED", "Booking", row.Id.ToString(), row.OrderCode, row.TenantId, "Domain", "Warning", $"Booking {row.OrderCode} da huy.");
        }
    }

    private async Task AddPaymentEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.CustomerPayments.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.UserId,
                x.PaymentCode,
                x.Status,
                x.CreatedAt,
                x.CreatedByUserId,
                x.PaidAt,
                x.FailedAt,
                x.WebhookReceivedAt
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            Add(events, row.CreatedAt, row.CreatedByUserId ?? row.UserId, "PAYMENT_CREATED", "Payment", row.Id.ToString(), row.PaymentCode, row.TenantId, "Domain", "Info", $"Tao giao dich {row.PaymentCode}.");
            if (row.PaidAt.HasValue)
                Add(events, row.PaidAt.Value, row.UserId, "PAYMENT_PAID", "Payment", row.Id.ToString(), row.PaymentCode, row.TenantId, "Payment", "Success", $"Giao dich {row.PaymentCode} da thanh toan.");
            if (row.FailedAt.HasValue)
                Add(events, row.FailedAt.Value, row.UserId, "PAYMENT_FAILED", "Payment", row.Id.ToString(), row.PaymentCode, row.TenantId, "Payment", "Error", $"Giao dich {row.PaymentCode} that bai.");
            if (row.WebhookReceivedAt.HasValue)
                Add(events, row.WebhookReceivedAt.Value, null, "PAYMENT_WEBHOOK_RECEIVED", "Payment", row.Id.ToString(), row.PaymentCode, row.TenantId, "Payment", "Info", $"Nhan webhook cho giao dich {row.PaymentCode}.");
        }
    }

    private async Task AddRefundEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.CustomerRefundRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.RequestedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.UserId,
                x.RefundCode,
                x.Status,
                x.RequestedAt,
                x.CreatedByUserId,
                x.ReviewedAt,
                x.CompletedAt,
                x.UpdatedByUserId
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            Add(events, row.RequestedAt, row.CreatedByUserId ?? row.UserId, "REFUND_REQUESTED", "Refund", row.Id.ToString(), row.RefundCode, row.TenantId, "Domain", "Warning", $"Khach yeu cau hoan tien {row.RefundCode}.");
            if (row.ReviewedAt.HasValue)
                Add(events, row.ReviewedAt.Value, row.UpdatedByUserId, $"REFUND_{row.Status.ToString().ToUpperInvariant()}", "Refund", row.Id.ToString(), row.RefundCode, row.TenantId, "Admin", row.Status == CustomerRefundStatus.Rejected ? "Error" : "Info", $"Admin review hoan tien {row.RefundCode}.");
            if (row.CompletedAt.HasValue)
                Add(events, row.CompletedAt.Value, row.UpdatedByUserId, "REFUND_COMPLETED", "Refund", row.Id.ToString(), row.RefundCode, row.TenantId, "Admin", "Success", $"Hoan tien {row.RefundCode} da hoan tat.");
        }
    }

    private async Task AddSupportEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.CustomerSupportTickets.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.UserId,
                x.TicketCode,
                x.Status,
                x.Subject,
                x.CreatedAt,
                x.CreatedByUserId,
                x.UpdatedAt,
                x.UpdatedByUserId,
                x.ResolvedAt
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            Add(events, row.CreatedAt, row.CreatedByUserId ?? row.UserId, "SUPPORT_TICKET_CREATED", "SupportTicket", row.Id.ToString(), row.TicketCode, row.TenantId, "Domain", "Warning", $"Tao ticket ho tro {row.TicketCode}: {row.Subject}");
            if (row.UpdatedAt.HasValue)
                Add(events, row.UpdatedAt.Value, row.UpdatedByUserId, $"SUPPORT_{row.Status.ToString().ToUpperInvariant()}", "SupportTicket", row.Id.ToString(), row.TicketCode, row.TenantId, "Admin", "Info", $"Cap nhat ticket ho tro {row.TicketCode}.");
            if (row.ResolvedAt.HasValue)
                Add(events, row.ResolvedAt.Value, row.UpdatedByUserId, "SUPPORT_RESOLVED", "SupportTicket", row.Id.ToString(), row.TicketCode, row.TenantId, "Admin", "Success", $"Xu ly xong ticket ho tro {row.TicketCode}.");
        }
    }

    private async Task AddSettlementEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.CustomerSettlementBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.BatchCode,
                x.Status,
                x.CreatedAt,
                x.CreatedByUserId,
                x.ApprovedAt,
                x.PaidAt,
                x.UpdatedByUserId
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            Add(events, row.CreatedAt, row.CreatedByUserId, "SETTLEMENT_BATCH_CREATED", "SettlementBatch", row.Id.ToString(), row.BatchCode, null, "Admin", "Info", $"Tao batch doi soat {row.BatchCode}.");
            if (row.ApprovedAt.HasValue)
                Add(events, row.ApprovedAt.Value, row.UpdatedByUserId, "SETTLEMENT_BATCH_APPROVED", "SettlementBatch", row.Id.ToString(), row.BatchCode, null, "Admin", "Info", $"Duyet batch doi soat {row.BatchCode}.");
            if (row.PaidAt.HasValue)
                Add(events, row.PaidAt.Value, row.UpdatedByUserId, "SETTLEMENT_BATCH_PAID", "SettlementBatch", row.Id.ToString(), row.BatchCode, null, "Admin", "Success", $"Da chuyen tien batch doi soat {row.BatchCode}.");
        }
    }

    private async Task AddTenantEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.Tenants.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.CreatedAt,
                x.CreatedByUserId,
                x.UpdatedAt,
                x.UpdatedByUserId
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            Add(events, row.CreatedAt, row.CreatedByUserId, "TENANT_CREATED", "Tenant", row.Id.ToString(), row.Code, row.Id, "Admin", "Info", $"Tao tenant {row.Code} - {row.Name}.");
            if (row.UpdatedAt.HasValue)
                Add(events, row.UpdatedAt.Value, row.UpdatedByUserId, "TENANT_UPDATED", "Tenant", row.Id.ToString(), row.Code, row.Id, "Admin", "Info", $"Cap nhat tenant {row.Code} - {row.Name}.");
        }
    }

    private async Task AddNotificationEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.CustomerNotifications.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.UserId,
                x.Title,
                x.Category,
                x.CreatedAt,
                x.CreatedByUserId
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            Add(events, row.CreatedAt, row.CreatedByUserId ?? row.UserId, "NOTIFICATION_CREATED", "Notification", row.Id.ToString(), row.Category, row.TenantId, "Notification", "Info", $"Tao thong bao: {row.Title}");
        }
    }

    private async Task AddPromoEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _db.Set<PromoRateOverride>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.PromoCode,
                x.CreatedAt,
                x.CreatedByUserId,
                x.UpdatedAt,
                x.UpdatedByUserId
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var code = string.IsNullOrWhiteSpace(row.PromoCode) ? row.Id.ToString("N")[..8].ToUpperInvariant() : row.PromoCode!;
            Add(events, row.CreatedAt, row.CreatedByUserId, "HOTEL_PROMO_OVERRIDE_CREATED", "HotelPromoOverride", row.Id.ToString(), code, row.TenantId, "Admin", "Info", $"Tao hotel promo override {code}.");
            if (row.UpdatedAt.HasValue)
                Add(events, row.UpdatedAt.Value, row.UpdatedByUserId, "HOTEL_PROMO_OVERRIDE_UPDATED", "HotelPromoOverride", row.Id.ToString(), code, row.TenantId, "Admin", "Info", $"Cap nhat hotel promo override {code}.");
        }

        var campaigns = await _db.PromotionCampaigns.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(AuditSourceLimit)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Code,
                x.OwnerScope,
                x.CreatedAt,
                x.CreatedByUserId,
                x.UpdatedAt,
                x.UpdatedByUserId
            })
            .ToListAsync(ct);

        foreach (var row in campaigns)
        {
            Add(events, row.CreatedAt, row.CreatedByUserId, "PROMOTION_CAMPAIGN_CREATED", "PromotionCampaign", row.Id.ToString(), row.Code, row.TenantId, row.OwnerScope.ToString(), "Info", $"Tao promotion campaign {row.Code}.");
            if (row.UpdatedAt.HasValue)
                Add(events, row.UpdatedAt.Value, row.UpdatedByUserId, "PROMOTION_CAMPAIGN_UPDATED", "PromotionCampaign", row.Id.ToString(), row.Code, row.TenantId, row.OwnerScope.ToString(), "Info", $"Cap nhat promotion campaign {row.Code}.");
        }
    }

    private async Task AddOnboardingEventsAsync(List<AuditEventSeed> events, CancellationToken ct)
    {
        var rows = await _onboardingStore.ListAsync(ct: ct);

        foreach (var row in rows.Take(AuditSourceLimit))
        {
            Add(events, row.SubmittedAt, null, "TENANT_ONBOARDING_SUBMITTED", "TenantOnboarding", row.TrackingCode, row.TrackingCode, row.TenantId, "FileStore", "Warning", $"Doi tac gui ho so onboarding {row.BusinessName}.");
            if (row.ReviewedAt.HasValue)
                Add(events, row.ReviewedAt.Value, null, $"TENANT_ONBOARDING_{row.Status.ToUpperInvariant()}", "TenantOnboarding", row.TrackingCode, row.TrackingCode, row.TenantId, "FileStore", row.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ? "Error" : "Info", $"Review ho so onboarding {row.BusinessName}.");
            if (row.ProvisionedAt.HasValue)
                Add(events, row.ProvisionedAt.Value, row.OwnerUserId, "TENANT_ONBOARDING_PROVISIONED", "TenantOnboarding", row.TrackingCode, row.TrackingCode, row.TenantId, "FileStore", "Success", $"Tao tenant tu ho so onboarding {row.BusinessName}.");
        }
    }

    private static void Add(
        List<AuditEventSeed> events,
        DateTimeOffset occurredAt,
        Guid? actorUserId,
        string action,
        string entityType,
        string entityId,
        string entityCode,
        Guid? tenantId,
        string source,
        string severity,
        string description)
    {
        events.Add(new AuditEventSeed(
            $"{entityType}:{entityId}:{action}:{occurredAt.ToUnixTimeMilliseconds()}",
            occurredAt,
            actorUserId,
            action,
            entityType,
            entityId,
            entityCode,
            tenantId,
            source,
            severity,
            description));
    }

    private static AdminOpsAuditEventDto MapAuditEvent(
        AuditEventSeed seed,
        IReadOnlyDictionary<Guid, string> tenantNames,
        IReadOnlyDictionary<Guid, string> actorNames)
    {
        return new AdminOpsAuditEventDto
        {
            Id = seed.Id,
            OccurredAt = seed.OccurredAt,
            ActorUserId = seed.ActorUserId,
            ActorName = seed.ActorUserId.HasValue && actorNames.TryGetValue(seed.ActorUserId.Value, out var actorName)
                ? actorName
                : "System",
            Action = seed.Action,
            EntityType = seed.EntityType,
            EntityId = seed.EntityId,
            EntityCode = seed.EntityCode,
            TenantId = seed.TenantId,
            TenantName = seed.TenantId.HasValue && tenantNames.TryGetValue(seed.TenantId.Value, out var tenantName)
                ? tenantName
                : null,
            Source = seed.Source,
            Severity = seed.Severity,
            Description = seed.Description
        };
    }

    private static bool MatchesAuditQuery(AdminOpsAuditEventDto item, string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return true;

        var keyword = q.Trim();
        return Contains(item.ActorName, keyword) ||
               Contains(item.Action, keyword) ||
               Contains(item.EntityType, keyword) ||
               Contains(item.EntityCode, keyword) ||
               Contains(item.TenantName, keyword) ||
               Contains(item.Description, keyword);
    }

    private static bool Contains(string? value, string keyword)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCampaignActive(PromotionCampaign campaign, DateTimeOffset now)
    {
        return campaign.Status == PromotionStatus.Active &&
               campaign.StartsAt <= now &&
               (!campaign.EndsAt.HasValue || campaign.EndsAt.Value >= now);
    }

    private sealed record AuditEventSeed(
        string Id,
        DateTimeOffset OccurredAt,
        Guid? ActorUserId,
        string Action,
        string EntityType,
        string EntityId,
        string EntityCode,
        Guid? TenantId,
        string Source,
        string Severity,
        string Description);
}
