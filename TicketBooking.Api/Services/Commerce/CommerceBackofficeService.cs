using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CommerceBackofficeService
{
    private static readonly CustomerRefundStatus[] OpenRefundStatuses =
    {
        CustomerRefundStatus.Requested,
        CustomerRefundStatus.UnderReview,
        CustomerRefundStatus.Approved,
        CustomerRefundStatus.Processing,
    };

    private readonly AppDbContext _db;
    private readonly CustomerNotificationService _notificationService;
    private readonly ITenantContext _tenantContext;

    public CommerceBackofficeService(
        AppDbContext db,
        CustomerNotificationService notificationService,
        ITenantContext tenantContext)
    {
        _db = db;
        _notificationService = notificationService;
        _tenantContext = tenantContext;
    }

    public async Task<AdminCommercePaymentListResponse> ListPaymentsAsync(
        Guid? tenantId,
        string? q,
        CustomerPaymentStatus? status,
        CancellationToken ct = default)
    {
        var query =
            from payment in _db.CustomerPayments.AsNoTracking()
            join order in _db.CustomerOrders.AsNoTracking() on payment.OrderId equals order.Id
            join user in _db.Users.AsNoTracking() on order.UserId equals user.Id
            join tenant in _db.Tenants.AsNoTracking() on order.TenantId equals tenant.Id
            where !payment.IsDeleted && !order.IsDeleted && !tenant.IsDeleted
            select new
            {
                Payment = payment,
                Order = order,
                CustomerName = user.FullName ?? user.Email ?? order.ContactFullName,
                TenantName = tenant.Name,
            };

        if (tenantId.HasValue)
            query = query.Where(x => x.Order.TenantId == tenantId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Payment.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Payment.PaymentCode.Contains(keyword) ||
                x.Order.OrderCode.Contains(keyword) ||
                x.Payment.ProviderInvoiceNumber.Contains(keyword) ||
                x.CustomerName.Contains(keyword));
        }

        var rows = await query
            .OrderByDescending(x => x.Payment.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        var items = rows.Select(x => new AdminCommercePaymentItemDto
        {
            Id = x.Payment.Id,
            OrderId = x.Order.Id,
            TenantId = x.Order.TenantId,
            TenantName = x.TenantName,
            PaymentCode = x.Payment.PaymentCode,
            OrderCode = x.Order.OrderCode,
            CustomerName = x.CustomerName,
            ServiceTitle = ExtractSnapshotTitle(x.Order.SnapshotJson, x.Order.OrderCode),
            ProductType = x.Order.ProductType,
            Provider = x.Payment.Provider,
            Method = x.Payment.Method,
            Status = x.Payment.Status,
            CurrencyCode = x.Payment.CurrencyCode,
            Amount = x.Payment.Amount,
            PaidAmount = x.Payment.PaidAmount,
            RefundedAmount = x.Payment.RefundedAmount,
            ProviderInvoiceNumber = x.Payment.ProviderInvoiceNumber,
            ProviderOrderId = x.Payment.ProviderOrderId,
            CreatedAt = x.Payment.CreatedAt,
            PaidAt = x.Payment.PaidAt,
            WebhookReceivedAt = x.Payment.WebhookReceivedAt,
        }).ToList();

        return new AdminCommercePaymentListResponse
        {
            Summary = new AdminCommercePaymentSummaryDto
            {
                TotalCount = items.Count,
                PaidCount = items.Count(x => x.Status == CustomerPaymentStatus.Paid),
                PendingCount = items.Count(x => x.Status == CustomerPaymentStatus.Pending),
                FailedCount = items.Count(x => x.Status is CustomerPaymentStatus.Failed or CustomerPaymentStatus.Cancelled or CustomerPaymentStatus.Expired),
                RefundedCount = items.Count(x => x.Status is CustomerPaymentStatus.RefundedPartial or CustomerPaymentStatus.RefundedFull || x.RefundedAmount > 0),
                TotalAmount = items.Sum(x => x.Amount),
            },
            Items = items,
        };
    }

    public async Task<AdminCommerceRefundListResponse> ListRefundsAsync(
        Guid? tenantId,
        string? q,
        CustomerRefundStatus? status,
        CancellationToken ct = default)
    {
        var query =
            from refund in _db.CustomerRefundRequests.AsNoTracking()
            join order in _db.CustomerOrders.AsNoTracking() on refund.OrderId equals order.Id
            join user in _db.Users.AsNoTracking() on order.UserId equals user.Id
            join tenant in _db.Tenants.AsNoTracking() on order.TenantId equals tenant.Id
            where !refund.IsDeleted && !order.IsDeleted && !tenant.IsDeleted
            select new
            {
                Refund = refund,
                Order = order,
                CustomerName = user.FullName ?? user.Email ?? order.ContactFullName,
                TenantName = tenant.Name,
            };

        if (tenantId.HasValue)
            query = query.Where(x => x.Order.TenantId == tenantId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Refund.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Refund.RefundCode.Contains(keyword) ||
                x.Order.OrderCode.Contains(keyword) ||
                x.CustomerName.Contains(keyword));
        }

        var rows = await query
            .OrderByDescending(x => x.Refund.RequestedAt)
            .Take(200)
            .ToListAsync(ct);

        var items = rows.Select(x => new AdminCommerceRefundItemDto
        {
            Id = x.Refund.Id,
            OrderId = x.Order.Id,
            PaymentId = x.Refund.PaymentId,
            TenantId = x.Order.TenantId,
            TenantName = x.TenantName,
            RefundCode = x.Refund.RefundCode,
            OrderCode = x.Order.OrderCode,
            CustomerName = x.CustomerName,
            ServiceTitle = ExtractSnapshotTitle(x.Order.SnapshotJson, x.Order.OrderCode),
            ProductType = x.Order.ProductType,
            Status = x.Refund.Status,
            SettlementStatus = x.Order.SettlementStatus,
            CurrencyCode = x.Refund.CurrencyCode,
            RequestedAmount = x.Refund.RequestedAmount,
            ApprovedAmount = x.Refund.ApprovedAmount,
            RefundedAmount = x.Refund.RefundedAmount,
            ReasonCode = x.Refund.ReasonCode,
            ReasonText = x.Refund.ReasonText,
            InternalNote = x.Refund.ReviewNote,
            RefundReference = x.Refund.RefundReference,
            RequestedAt = x.Refund.RequestedAt,
            ReviewedAt = x.Refund.ReviewedAt,
            CompletedAt = x.Refund.CompletedAt,
        }).ToList();

        return new AdminCommerceRefundListResponse
        {
            Summary = new AdminCommerceRefundSummaryDto
            {
                TotalCount = items.Count,
                PendingCount = items.Count(x => OpenRefundStatuses.Contains(x.Status)),
                ApprovedCount = items.Count(x => x.Status == CustomerRefundStatus.Approved),
                CompletedCount = items.Count(x => x.Status is CustomerRefundStatus.RefundedPartial or CustomerRefundStatus.RefundedFull),
                RejectedCount = items.Count(x => x.Status == CustomerRefundStatus.Rejected),
                PendingAmount = items.Where(x => OpenRefundStatuses.Contains(x.Status)).Sum(x => x.ApprovedAmount ?? x.RequestedAmount),
            },
            Items = items,
        };
    }

    public async Task<AdminCommerceRefundItemDto> ApproveRefundAsync(
        Guid refundId,
        ReviewRefundRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        request ??= new ReviewRefundRequest();

        var refund = await _db.CustomerRefundRequests
            .FirstOrDefaultAsync(x => x.Id == refundId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Khong tim thay yeu cau hoan tien.");

        var order = await _db.CustomerOrders
            .FirstOrDefaultAsync(x => x.Id == refund.OrderId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Khong tim thay don hang cua yeu cau hoan tien.");

        if (refund.Status is CustomerRefundStatus.Rejected or CustomerRefundStatus.RefundedPartial or CustomerRefundStatus.RefundedFull)
            throw new InvalidOperationException("Yeu cau hoan tien nay da o trang thai cuoi.");

        var remainingRefundable = Math.Max(0m, order.PayableAmount - order.RefundedAmount);
        var approvedAmount = request.ApprovedAmount.GetValueOrDefault(refund.RequestedAmount);
        if (approvedAmount <= 0 || approvedAmount > remainingRefundable)
            throw new InvalidOperationException("So tien duyet hoan khong hop le.");

        var now = DateTimeOffset.UtcNow;
        refund.Status = CustomerRefundStatus.Approved;
        refund.ApprovedAmount = approvedAmount;
        refund.ReviewNote = NormalizeInternalNote(request);
        refund.ReviewedAt = now;
        refund.UpdatedAt = now;
        refund.UpdatedByUserId = actorUserId;

        order.RefundStatus = CustomerRefundStatus.Approved;
        order.Status = CustomerOrderStatus.RefundRequested;
        order.UpdatedAt = now;
        order.UpdatedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        await NotifyRefundAsync(order, refund, "Yeu cau hoan tien da duoc duyet", "Yeu cau hoan tien cua ban da duoc duyet va dang cho hoan tien thu cong.", ct);

        return await GetRefundAsync(refund.Id, ct);
    }

    public async Task<AdminCommerceRefundItemDto> RejectRefundAsync(
        Guid refundId,
        ReviewRefundRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        request ??= new ReviewRefundRequest();

        var refund = await _db.CustomerRefundRequests
            .FirstOrDefaultAsync(x => x.Id == refundId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Khong tim thay yeu cau hoan tien.");

        var order = await _db.CustomerOrders
            .FirstOrDefaultAsync(x => x.Id == refund.OrderId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Khong tim thay don hang cua yeu cau hoan tien.");

        if (refund.Status is CustomerRefundStatus.Rejected or CustomerRefundStatus.RefundedPartial or CustomerRefundStatus.RefundedFull)
            throw new InvalidOperationException("Yeu cau hoan tien nay da o trang thai cuoi.");

        var now = DateTimeOffset.UtcNow;
        refund.Status = CustomerRefundStatus.Rejected;
        refund.ReviewNote = NormalizeInternalNote(request);
        refund.ReviewedAt = now;
        refund.UpdatedAt = now;
        refund.UpdatedByUserId = actorUserId;

        order.RefundStatus = await ResolveOrderRefundStatusAsync(order.Id, refund.Id, ct);
        order.Status = ResolveOrderStatusAfterRefund(order);
        order.UpdatedAt = now;
        order.UpdatedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        await NotifyRefundAsync(order, refund, "Yeu cau hoan tien bi tu choi", "Nen tang da tu choi yeu cau hoan tien cua ban. Vui long xem ghi chu xu ly de biet them chi tiet.", ct);

        return await GetRefundAsync(refund.Id, ct);
    }

    public async Task<AdminCommerceRefundItemDto> CompleteRefundAsync(
        Guid refundId,
        CompleteRefundRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        request ??= new CompleteRefundRequest();

        var refund = await _db.CustomerRefundRequests
            .FirstOrDefaultAsync(x => x.Id == refundId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Khong tim thay yeu cau hoan tien.");

        var order = await _db.CustomerOrders
            .FirstOrDefaultAsync(x => x.Id == refund.OrderId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Khong tim thay don hang cua yeu cau hoan tien.");

        var payment = refund.PaymentId.HasValue
            ? await _db.CustomerPayments.FirstOrDefaultAsync(x => x.Id == refund.PaymentId.Value && !x.IsDeleted, ct)
            : await _db.CustomerPayments.FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

        if (payment is null)
            throw new KeyNotFoundException("Khong tim thay giao dich thanh toan de cap nhat hoan tien.");

        if (refund.Status is CustomerRefundStatus.Rejected or CustomerRefundStatus.RefundedPartial or CustomerRefundStatus.RefundedFull)
            throw new InvalidOperationException("Yeu cau hoan tien nay da o trang thai cuoi.");

        if (refund.Status is not (CustomerRefundStatus.Approved or CustomerRefundStatus.Processing))
            throw new InvalidOperationException("Yeu cau hoan tien can duoc duyet truoc khi xac nhan hoan tien thu cong.");

        var remainingRefundable = Math.Max(0m, order.PayableAmount - order.RefundedAmount);
        var refundedAmount = request.RefundedAmount ?? refund.ApprovedAmount ?? refund.RequestedAmount;
        if (refundedAmount <= 0 || refundedAmount > remainingRefundable)
            throw new InvalidOperationException("So tien hoan thuc te khong hop le.");

        var refundReference = NormalizeRequired(request.RefundReference, "Vui long nhap ma tham chieu hoan tien.");

        var previousRefundedAmount = order.RefundedAmount;
        var totalRefundedAmount = previousRefundedAmount + refundedAmount;
        var isFullyRefunded = totalRefundedAmount >= order.PayableAmount && order.PayableAmount > 0;
        var now = DateTimeOffset.UtcNow;

        refund.ApprovedAmount ??= refundedAmount;
        refund.RefundedAmount = refundedAmount;
        refund.ReviewNote = NormalizeInternalNote(request) ?? refund.ReviewNote;
        refund.RefundReference = refundReference;
        refund.ReviewedAt ??= now;
        refund.CompletedAt = now;
        refund.Status = isFullyRefunded ? CustomerRefundStatus.RefundedFull : CustomerRefundStatus.RefundedPartial;
        refund.UpdatedAt = now;
        refund.UpdatedByUserId = actorUserId;

        order.RefundedAmount = totalRefundedAmount;
        order.RefundStatus = refund.Status;
        order.PaymentStatus = isFullyRefunded ? CustomerPaymentStatus.RefundedFull : CustomerPaymentStatus.RefundedPartial;
        order.Status = isFullyRefunded ? CustomerOrderStatus.RefundedFull : CustomerOrderStatus.RefundedPartial;
        order.SettlementStatus = order.SettlementStatus is CustomerSettlementStatus.Settled or CustomerSettlementStatus.InSettlement
            ? CustomerSettlementStatus.Adjusted
            : CustomerSettlementStatus.Unsettled;
        order.UpdatedAt = now;
        order.UpdatedByUserId = actorUserId;

        payment.RefundedAmount += refundedAmount;
        payment.Status = isFullyRefunded ? CustomerPaymentStatus.RefundedFull : CustomerPaymentStatus.RefundedPartial;
        payment.UpdatedAt = now;
        payment.UpdatedByUserId = actorUserId;

        var ticket = await _db.CustomerTickets.FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);
        if (ticket is not null && isFullyRefunded)
        {
            ticket.Status = CustomerTicketStatus.Refunded;
            ticket.CancelledAt ??= now;
            ticket.UpdatedAt = now;
            ticket.UpdatedByUserId = actorUserId;
        }

        await _db.SaveChangesAsync(ct);
        await NotifyRefundAsync(order, refund, "Da cap nhat hoan tien", "He thong da ghi nhan viec hoan tien va cap nhat lai don hang cua ban.", ct);

        return await GetRefundAsync(refund.Id, ct);
    }

    public async Task<AdminCommerceSupportListResponse> ListSupportTicketsAsync(
        Guid? tenantId,
        string? q,
        CustomerSupportTicketStatus? status,
        CancellationToken ct = default)
    {
        var query =
            from ticket in _db.CustomerSupportTickets.AsNoTracking()
            join user in _db.Users.AsNoTracking() on ticket.UserId equals user.Id
            join order in _db.CustomerOrders.AsNoTracking() on ticket.OrderId equals order.Id into orderJoin
            from order in orderJoin.DefaultIfEmpty()
            join tenant in _db.Tenants.AsNoTracking() on ticket.TenantId equals tenant.Id into tenantJoin
            from tenant in tenantJoin.DefaultIfEmpty()
            where !ticket.IsDeleted
            select new
            {
                Ticket = ticket,
                CustomerName = user.FullName ?? user.Email ?? ticket.ContactEmail ?? ticket.TicketCode,
                OrderCode = order != null && !order.IsDeleted ? order.OrderCode : null,
                TenantName = tenant != null && !tenant.IsDeleted ? tenant.Name : null,
            };

        if (tenantId.HasValue)
            query = query.Where(x => x.Ticket.TenantId == tenantId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Ticket.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Ticket.TicketCode.Contains(keyword) ||
                x.Ticket.Subject.Contains(keyword) ||
                x.CustomerName.Contains(keyword));
        }

        var rows = await query
            .OrderByDescending(x => x.Ticket.LastActivityAt ?? x.Ticket.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        var items = rows.Select(x => MapSupportTicket(x.Ticket, x.CustomerName, x.OrderCode, x.TenantName)).ToList();
        return new AdminCommerceSupportListResponse
        {
            Summary = new AdminCommerceSupportSummaryDto
            {
                OpenCount = items.Count(x => x.Status == CustomerSupportTicketStatus.Open),
                ProcessingCount = items.Count(x => x.Status == CustomerSupportTicketStatus.Processing),
                ResolvedCount = items.Count(x => x.Status == CustomerSupportTicketStatus.Resolved),
                HighPriorityCount = items.Count(x => string.Equals(x.Priority, "high", StringComparison.OrdinalIgnoreCase)),
            },
            Items = items,
        };
    }

    public async Task<AdminCommerceSupportTicketDto> ReplySupportTicketAsync(
        Guid ticketId,
        ReplySupportTicketRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        request ??= new ReplySupportTicketRequest();
        var message = NormalizeRequired(request.Message, "Vui long nhap noi dung phan hoi.");

        var ticket = await _db.CustomerSupportTickets
            .FirstOrDefaultAsync(x => x.Id == ticketId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Khong tim thay ticket ho tro.");

        var orderCode = ticket.OrderId.HasValue
            ? await _db.CustomerOrders.Where(x => x.Id == ticket.OrderId.Value && !x.IsDeleted).Select(x => x.OrderCode).FirstOrDefaultAsync(ct)
            : null;

        var now = DateTimeOffset.UtcNow;
        ticket.Status = request.MarkResolved ? CustomerSupportTicketStatus.Resolved : CustomerSupportTicketStatus.Processing;
        ticket.ResolutionNote = message;
        ticket.HasUnreadStaffReply = true;
        ticket.FirstResponseAt ??= now;
        ticket.ResolvedAt = request.MarkResolved ? now : null;
        ticket.LastActivityAt = now;
        ticket.UpdatedAt = now;
        ticket.UpdatedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);

        await _notificationService.CreateAsync(
            ticket.UserId,
            ticket.TenantId,
            "support",
            request.MarkResolved ? "Yeu cau ho tro da duoc cap nhat" : "Co phan hoi moi cho ticket ho tro",
            request.MarkResolved
                ? $"Ticket {ticket.TicketCode} da duoc xu ly. Vui long kiem tra chi tiet phan hoi moi nhat."
                : $"Ticket {ticket.TicketCode} da co phan hoi moi tu doi ngu ho tro.",
            "/support",
            "support-ticket",
            ticket.Id,
            ct: ct);

        var customerName = await _db.Users
            .Where(x => x.Id == ticket.UserId)
            .Select(x => x.FullName ?? x.Email ?? ticket.ContactEmail ?? ticket.TicketCode)
            .FirstOrDefaultAsync(ct) ?? ticket.TicketCode;

        var tenantName = ticket.TenantId.HasValue
            ? await _db.Tenants.Where(x => x.Id == ticket.TenantId.Value && !x.IsDeleted).Select(x => x.Name).FirstOrDefaultAsync(ct)
            : null;

        return MapSupportTicket(ticket, customerName, orderCode, tenantName);
    }

    public async Task<AdminSettlementDashboardDto> GetSettlementDashboardAsync(CancellationToken ct = default)
    {
        var batches = await _db.CustomerSettlementBatches
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.PeriodYear)
            .ThenByDescending(x => x.PeriodMonth)
            .ThenByDescending(x => x.CreatedAt)
            .Take(12)
            .Select(x => new AdminSettlementBatchDto
            {
                Id = x.Id,
                BatchCode = x.BatchCode,
                PeriodYear = x.PeriodYear,
                PeriodMonth = x.PeriodMonth,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                CurrencyCode = x.CurrencyCode,
                TotalGrossAmount = x.TotalGrossAmount,
                TotalCommissionAmount = x.TotalCommissionAmount,
                TotalCommissionAdjustmentAmount = x.TotalCommissionAdjustmentAmount,
                TotalTenantNetAmount = x.TotalTenantNetAmount,
                TotalRefundAmount = x.TotalRefundAmount,
                TotalNetPayoutAmount = x.TotalNetPayoutAmount,
                TenantCount = x.TenantCount,
                LineCount = x.LineCount,
                Notes = x.Notes,
                ApprovedAt = x.ApprovedAt,
                PaidAt = x.PaidAt,
            })
            .ToListAsync(ct);

        var batchIds = batches.Select(x => x.Id).ToList();
        var payoutRows = batchIds.Count == 0
            ? new List<AdminSettlementPayoutDto>()
            : await (
                from line in _db.CustomerSettlementBatchLines.AsNoTracking()
                join batch in _db.CustomerSettlementBatches.AsNoTracking() on line.BatchId equals batch.Id
                join tenant in _db.Tenants.AsNoTracking() on line.TenantId equals tenant.Id
                join payout in _db.CustomerTenantPayoutAccounts.AsNoTracking().Where(x => x.IsDefault && !x.IsDeleted)
                    on line.TenantId equals payout.TenantId into payoutJoin
                from payout in payoutJoin.DefaultIfEmpty()
                where batchIds.Contains(line.BatchId) && !line.IsDeleted && !batch.IsDeleted && !tenant.IsDeleted
                group new { line, batch, tenant, payout } by new
                {
                    line.BatchId,
                    line.TenantId,
                    tenant.Name,
                    line.CurrencyCode,
                    batch.Status,
                    batch.PaidAt,
                    payout.BankName,
                    payout.AccountNumber,
                    payout.AccountHolder,
                }
                into grouped
                select new AdminSettlementPayoutDto
                {
                    BatchId = grouped.Key.BatchId,
                    TenantId = grouped.Key.TenantId,
                    TenantName = grouped.Key.Name,
                    CurrencyCode = grouped.Key.CurrencyCode,
                    Amount = grouped.Sum(x => x.line.NetPayoutAmount),
                    LineCount = grouped.Count(),
                    Status = grouped.Key.Status == CustomerSettlementBatchStatus.Completed
                        ? CustomerSettlementStatus.Settled
                        : grouped.Any(x => x.line.Status == CustomerSettlementStatus.Adjusted)
                            ? CustomerSettlementStatus.Adjusted
                            : CustomerSettlementStatus.InSettlement,
                    BankName = grouped.Key.BankName,
                    AccountNumberMasked = MaskAccountNumber(grouped.Key.AccountNumber),
                    AccountHolder = grouped.Key.AccountHolder,
                    PaidAt = grouped.Key.PaidAt,
                })
            .OrderByDescending(x => x.PaidAt)
            .ThenBy(x => x.TenantName)
            .ToListAsync(ct);

        var allBatches = await _db.CustomerSettlementBatches
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);

        return new AdminSettlementDashboardDto
        {
            Summary = new AdminSettlementSummaryDto
            {
                TotalBatchAmount = allBatches.Sum(x => x.TotalNetPayoutAmount),
                ProcessingAmount = allBatches.Where(x => x.Status != CustomerSettlementBatchStatus.Completed).Sum(x => x.TotalNetPayoutAmount),
                PaidAmount = allBatches.Where(x => x.Status == CustomerSettlementBatchStatus.Completed).Sum(x => x.TotalNetPayoutAmount),
                TenantCount = payoutRows.Select(x => x.TenantId).Distinct().Count(),
            },
            Batches = batches,
            Payouts = payoutRows,
        };
    }

    public async Task<AdminSettlementDashboardDto> GenerateSettlementBatchAsync(
        GenerateSettlementBatchRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        request ??= new GenerateSettlementBatchRequest();

        var now = DateTimeOffset.UtcNow;
        var year = request.Year.GetValueOrDefault(now.Year);
        var month = request.Month.GetValueOrDefault(now.Month);
        if (year < 2000 || month is < 1 or > 12)
            throw new InvalidOperationException("Ky doi soat khong hop le.");

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        await ExecuteWithoutTenantScopeAsync(async () =>
        {
            var batch = await _db.CustomerSettlementBatches
                .FirstOrDefaultAsync(x =>
                    x.PeriodYear == year &&
                    x.PeriodMonth == month &&
                    !x.IsDeleted, ct);

            if (batch is null)
            {
                batch = new CustomerSettlementBatch
                {
                    Id = Guid.NewGuid(),
                    BatchCode = await GenerateSettlementBatchCodeAsync(year, month, ct),
                    PeriodYear = year,
                    PeriodMonth = month,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = CustomerSettlementBatchStatus.Draft,
                    CurrencyCode = "VND",
                    Notes = NormalizeOptional(request.Notes),
                    CreatedAt = now,
                    CreatedByUserId = actorUserId,
                };

                _db.CustomerSettlementBatches.Add(batch);
                await _db.SaveChangesAsync(ct);
            }
            else if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                batch.Notes = request.Notes.Trim();
                batch.UpdatedAt = now;
                batch.UpdatedByUserId = actorUserId;
            }

            if (batch.Status == CustomerSettlementBatchStatus.Completed)
                return;

            await AddRegularSettlementLinesAsync(batch, startDate, endDate, actorUserId, ct);
            await AddRefundAdjustmentLinesAsync(batch, startDate, endDate, actorUserId, ct);
            await _db.SaveChangesAsync(ct);
            await RecalculateBatchAsync(batch, actorUserId, ct);
            await _db.SaveChangesAsync(ct);
        });

        return await GetSettlementDashboardAsync(ct);
    }

    public async Task<AdminSettlementDashboardDto> MarkSettlementBatchPaidAsync(
        Guid batchId,
        MarkSettlementBatchPaidRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        request ??= new MarkSettlementBatchPaidRequest();

        await ExecuteWithoutTenantScopeAsync(async () =>
        {
            var batch = await _db.CustomerSettlementBatches
                .FirstOrDefaultAsync(x => x.Id == batchId && !x.IsDeleted, ct)
                ?? throw new KeyNotFoundException("Khong tim thay batch doi soat.");

            var lines = await _db.CustomerSettlementBatchLines
                .Where(x => x.BatchId == batchId && !x.IsDeleted)
                .ToListAsync(ct);

            var now = DateTimeOffset.UtcNow;
            batch.Status = CustomerSettlementBatchStatus.Completed;
            batch.ApprovedAt ??= now;
            batch.PaidAt = now;
            batch.Notes = NormalizeOptional(request.Notes) ?? batch.Notes;
            batch.UpdatedAt = now;
            batch.UpdatedByUserId = actorUserId;

            foreach (var line in lines)
            {
                line.Status = line.NetPayoutAmount < 0 ? CustomerSettlementStatus.Adjusted : CustomerSettlementStatus.Settled;
                line.SettledAt = now;
                line.UpdatedAt = now;
                line.UpdatedByUserId = actorUserId;
            }

            var orderIds = lines.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
            var orders = orderIds.Count == 0
                ? new List<CustomerOrder>()
                : await _db.CustomerOrders.Where(x => orderIds.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct);

            foreach (var order in orders)
            {
                var orderLines = lines.Where(x => x.OrderId == order.Id).ToList();
                var hasAdjustment = orderLines.Any(x => x.NetPayoutAmount < 0);
                var hasPositiveSettlement = orderLines.Any(x => x.NetPayoutAmount >= 0 || x.GrossAmount > 0);

                if (hasPositiveSettlement)
                    order.SettledAt ??= now;

                order.SettlementBatchId = batch.Id;
                order.SettlementStatus = hasAdjustment ? CustomerSettlementStatus.Adjusted : CustomerSettlementStatus.Settled;
                order.UpdatedAt = now;
                order.UpdatedByUserId = actorUserId;
            }

            await RecalculateBatchAsync(batch, actorUserId, ct);
            await _db.SaveChangesAsync(ct);
        });

        return await GetSettlementDashboardAsync(ct);
    }

    public async Task<TenantFinanceDashboardDto> GetTenantFinanceDashboardAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthStartDateTime = monthStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var nextMonthDateTime = monthStart.AddMonths(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var orders = await _db.CustomerOrders
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.PaidAt ?? x.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        var paidOrders = orders.Where(x => x.PaidAt.HasValue).ToList();
        var currentMonthOrders = paidOrders.Where(x => x.PaidAt >= monthStartDateTime && x.PaidAt < nextMonthDateTime).ToList();

        var payoutAccountEntity = await _db.CustomerTenantPayoutAccounts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefault && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var monthlySeries = Enumerable.Range(0, 12)
            .Select(offset =>
            {
                var pointDate = monthStart.AddMonths(-11 + offset);
                var pointStart = pointDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var pointEnd = pointDate.AddMonths(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var items = paidOrders.Where(x => x.PaidAt >= pointStart && x.PaidAt < pointEnd).ToList();
                return new TenantFinanceMonthlyPointDto
                {
                    Label = $"T{pointDate.Month}",
                    GrossAmount = items.Sum(x => x.GrossAmount),
                    NetAmount = items.Sum(GetOrderNetAfterRefund),
                };
            })
            .ToList();

        var transactions = BuildTenantTransactions(paidOrders)
            .OrderByDescending(x => x.OccurredAt)
            .Take(20)
            .ToList();

        var settledAmount = await _db.CustomerSettlementBatchLines
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == CustomerSettlementStatus.Settled)
            .SumAsync(x => (decimal?)x.NetPayoutAmount, ct) ?? 0m;

        var adjustedAmount = await _db.CustomerSettlementBatchLines
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == CustomerSettlementStatus.Adjusted)
            .SumAsync(x => (decimal?)Math.Abs(x.NetPayoutAmount), ct) ?? 0m;

        return new TenantFinanceDashboardDto
        {
            Summary = new TenantFinanceSummaryDto
            {
                CurrentMonthGrossAmount = currentMonthOrders.Sum(x => x.GrossAmount),
                CurrentMonthNetAmount = currentMonthOrders.Sum(GetOrderNetAfterRefund),
                PendingSettlementAmount = paidOrders
                    .Where(x => x.SettlementStatus is CustomerSettlementStatus.Unsettled or CustomerSettlementStatus.InSettlement)
                    .Sum(GetOrderNetAfterRefund),
                AdjustedAmount = adjustedAmount,
                SettledAmount = settledAmount,
                NextSettlementDate = monthEnd,
            },
            PayoutAccount = payoutAccountEntity is null ? null : MapPayoutAccount(payoutAccountEntity),
            MonthlySeries = monthlySeries,
            Transactions = transactions,
        };
    }

    public async Task<TenantPayoutAccountDto> UpsertTenantPayoutAccountAsync(
        Guid tenantId,
        UpsertTenantPayoutAccountRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        request ??= new UpsertTenantPayoutAccountRequest();

        var bankName = NormalizeRequired(request.BankName, "Vui long nhap ten ngan hang.");
        var accountNumber = NormalizeRequired(request.AccountNumber, "Vui long nhap so tai khoan.");
        var accountHolder = NormalizeRequired(request.AccountHolder, "Vui long nhap chu tai khoan.");
        var now = DateTimeOffset.UtcNow;

        var entity = await _db.CustomerTenantPayoutAccounts
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault && !x.IsDeleted, ct);

        if (entity is null)
        {
            entity = new CustomerTenantPayoutAccount
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                IsDefault = true,
                CreatedAt = now,
                CreatedByUserId = actorUserId,
            };

            _db.CustomerTenantPayoutAccounts.Add(entity);
        }

        var hasCriticalChange =
            !string.Equals(entity.BankName, bankName, StringComparison.Ordinal) ||
            !string.Equals(entity.AccountNumber, accountNumber, StringComparison.Ordinal) ||
            !string.Equals(entity.AccountHolder, accountHolder, StringComparison.Ordinal);

        entity.BankName = bankName;
        entity.AccountNumber = accountNumber;
        entity.AccountHolder = accountHolder;
        entity.BankBranch = NormalizeOptional(request.BankBranch);
        entity.Note = NormalizeOptional(request.Note);
        entity.IsDefault = true;
        entity.IsVerified = hasCriticalChange ? false : entity.IsVerified;
        entity.VerifiedAt = hasCriticalChange ? null : entity.VerifiedAt;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return MapPayoutAccount(entity);
    }

    private async Task<AdminCommerceRefundItemDto> GetRefundAsync(Guid refundId, CancellationToken ct)
    {
        var response = await ListRefundsAsync(null, null, null, ct);
        return response.Items.First(x => x.Id == refundId);
    }

    private async Task NotifyRefundAsync(
        CustomerOrder order,
        CustomerRefundRequest refund,
        string title,
        string body,
        CancellationToken ct)
    {
        await _notificationService.CreateAsync(
            order.UserId,
            order.TenantId,
            "refund",
            title,
            body,
            $"/my-account/bookings/{order.OrderCode}",
            "refund-request",
            refund.Id,
            ct: ct);
    }

    private async Task<CustomerRefundStatus> ResolveOrderRefundStatusAsync(
        Guid orderId,
        Guid currentRefundId,
        CancellationToken ct)
    {
        var hasOpenRefund = await _db.CustomerRefundRequests
            .AnyAsync(x =>
                x.OrderId == orderId &&
                x.Id != currentRefundId &&
                !x.IsDeleted &&
                OpenRefundStatuses.Contains(x.Status), ct);

        return hasOpenRefund ? CustomerRefundStatus.Requested : CustomerRefundStatus.None;
    }

    private static CustomerOrderStatus ResolveOrderStatusAfterRefund(CustomerOrder order)
    {
        if (order.RefundedAmount >= order.PayableAmount && order.PayableAmount > 0)
            return CustomerOrderStatus.RefundedFull;

        if (order.RefundedAmount > 0)
            return CustomerOrderStatus.RefundedPartial;

        if (order.CompletedAt.HasValue)
            return CustomerOrderStatus.Completed;

        if (order.TicketIssuedAt.HasValue || order.TicketStatus == CustomerTicketStatus.Issued)
            return CustomerOrderStatus.TicketIssued;

        if (order.PaidAt.HasValue || order.PaymentStatus == CustomerPaymentStatus.Paid)
            return CustomerOrderStatus.Paid;

        return order.Status;
    }

    private async Task AddRegularSettlementLinesAsync(
        CustomerSettlementBatch batch,
        DateOnly startDate,
        DateOnly endDate,
        Guid actorUserId,
        CancellationToken ct)
    {
        var existingOrderIds = await _db.CustomerSettlementBatchLines
            .AsNoTracking()
            .Where(x => x.OrderId.HasValue && x.RefundRequestId == null && !x.IsDeleted)
            .Select(x => x.OrderId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var start = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTimeOffset.UtcNow;

        var orders = await _db.CustomerOrders
            .Where(x =>
                x.PaidAt.HasValue &&
                x.PaidAt >= start &&
                x.PaidAt < endExclusive &&
                !x.IsDeleted &&
                !existingOrderIds.Contains(x.Id))
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            var line = new CustomerSettlementBatchLine
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                TenantId = order.TenantId,
                OrderId = order.Id,
                PaymentId = await _db.CustomerPayments
                    .Where(x => x.OrderId == order.Id && !x.IsDeleted)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(ct),
                Status = CustomerSettlementStatus.InSettlement,
                CurrencyCode = order.CurrencyCode,
                GrossAmount = order.GrossAmount,
                CommissionAmount = order.PlatformCommissionAmount,
                CommissionAdjustmentAmount = 0m,
                TenantNetAmount = order.TenantNetAmount,
                RefundAmount = 0m,
                NetPayoutAmount = order.TenantNetAmount,
                Description = $"Settlement for order {order.OrderCode}",
                MetadataJson = "{\"type\":\"order\"}",
                CreatedAt = now,
                CreatedByUserId = actorUserId,
            };

            _db.CustomerSettlementBatchLines.Add(line);

            if (order.SettlementStatus == CustomerSettlementStatus.Unsettled)
            {
                order.SettlementStatus = CustomerSettlementStatus.InSettlement;
                order.SettlementBatchId = batch.Id;
                order.UpdatedAt = now;
                order.UpdatedByUserId = actorUserId;
            }
        }
    }

    private async Task AddRefundAdjustmentLinesAsync(
        CustomerSettlementBatch batch,
        DateOnly startDate,
        DateOnly endDate,
        Guid actorUserId,
        CancellationToken ct)
    {
        var existingRefundIds = await _db.CustomerSettlementBatchLines
            .AsNoTracking()
            .Where(x => x.RefundRequestId.HasValue && !x.IsDeleted)
            .Select(x => x.RefundRequestId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var start = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTimeOffset.UtcNow;

        var refunds = await _db.CustomerRefundRequests
            .Where(x =>
                x.CompletedAt.HasValue &&
                x.CompletedAt >= start &&
                x.CompletedAt < endExclusive &&
                !x.IsDeleted &&
                !existingRefundIds.Contains(x.Id) &&
                (x.Status == CustomerRefundStatus.RefundedPartial || x.Status == CustomerRefundStatus.RefundedFull))
            .ToListAsync(ct);

        var orderIds = refunds.Select(x => x.OrderId).Distinct().ToList();
        var orders = orderIds.Count == 0
            ? new Dictionary<Guid, CustomerOrder>()
            : await _db.CustomerOrders
                .Where(x => orderIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, ct);

        foreach (var refund in refunds)
        {
            if (!orders.TryGetValue(refund.OrderId, out var order))
                continue;

            var refundedAmount = refund.RefundedAmount ?? refund.ApprovedAmount ?? refund.RequestedAmount;
            if (refundedAmount <= 0)
                continue;

            var commissionAdjustmentAmount = CalculateCommissionRefund(order.PlatformCommissionAmount, order.PayableAmount, refundedAmount, order.CurrencyCode);
            var tenantNetAdjustment = refundedAmount - commissionAdjustmentAmount;

            var line = new CustomerSettlementBatchLine
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                TenantId = order.TenantId,
                OrderId = order.Id,
                PaymentId = refund.PaymentId,
                RefundRequestId = refund.Id,
                Status = CustomerSettlementStatus.Adjusted,
                CurrencyCode = order.CurrencyCode,
                GrossAmount = 0m,
                CommissionAmount = 0m,
                CommissionAdjustmentAmount = commissionAdjustmentAmount,
                TenantNetAmount = 0m,
                RefundAmount = refundedAmount,
                NetPayoutAmount = -tenantNetAdjustment,
                Description = $"Refund adjustment for order {order.OrderCode}",
                MetadataJson = "{\"type\":\"refund-adjustment\"}",
                CreatedAt = now,
                CreatedByUserId = actorUserId,
            };

            _db.CustomerSettlementBatchLines.Add(line);

            order.SettlementStatus = order.SettlementStatus == CustomerSettlementStatus.Settled
                ? CustomerSettlementStatus.Adjusted
                : order.SettlementStatus;
            order.UpdatedAt = now;
            order.UpdatedByUserId = actorUserId;
        }
    }

    private async Task RecalculateBatchAsync(
        CustomerSettlementBatch batch,
        Guid actorUserId,
        CancellationToken ct)
    {
        var lines = await _db.CustomerSettlementBatchLines
            .Where(x => x.BatchId == batch.Id && !x.IsDeleted)
            .ToListAsync(ct);

        batch.TotalGrossAmount = lines.Sum(x => x.GrossAmount);
        batch.TotalCommissionAmount = lines.Sum(x => x.CommissionAmount);
        batch.TotalCommissionAdjustmentAmount = lines.Sum(x => x.CommissionAdjustmentAmount);
        batch.TotalTenantNetAmount = lines.Sum(x => x.TenantNetAmount);
        batch.TotalRefundAmount = lines.Sum(x => x.RefundAmount);
        batch.TotalNetPayoutAmount = lines.Sum(x => x.NetPayoutAmount);
        batch.TenantCount = lines.Select(x => x.TenantId).Distinct().Count();
        batch.LineCount = lines.Count;
        batch.UpdatedAt = DateTimeOffset.UtcNow;
        batch.UpdatedByUserId = actorUserId;
    }

    private async Task<string> GenerateSettlementBatchCodeAsync(int year, int month, CancellationToken ct)
    {
        var prefix = $"STL-{year}{month:00}";
        var existing = await _db.CustomerSettlementBatches
            .AsNoTracking()
            .Where(x => x.BatchCode.StartsWith(prefix))
            .CountAsync(ct);

        return $"{prefix}-{existing + 1:000}";
    }

    private static AdminCommerceSupportTicketDto MapSupportTicket(
        CustomerSupportTicket ticket,
        string customerName,
        string? orderCode,
        string? tenantName)
    {
        var messages = new List<AdminCommerceSupportMessageDto>
        {
            new()
            {
                From = "customer",
                Text = ticket.Content,
                At = ticket.CreatedAt,
            },
        };

        if (!string.IsNullOrWhiteSpace(ticket.ResolutionNote))
        {
            messages.Add(new AdminCommerceSupportMessageDto
            {
                From = "agent",
                Text = ticket.ResolutionNote,
                At = ticket.FirstResponseAt ?? ticket.UpdatedAt ?? ticket.LastActivityAt,
            });
        }

        return new AdminCommerceSupportTicketDto
        {
            Id = ticket.Id,
            TenantId = ticket.TenantId,
            TenantName = tenantName,
            TicketCode = ticket.TicketCode,
            Subject = ticket.Subject,
            Category = ticket.Category,
            Priority = ResolveSupportPriority(ticket),
            Status = ticket.Status,
            CustomerName = customerName,
            OrderCode = orderCode,
            Content = ticket.Content,
            ResolutionNote = ticket.ResolutionNote,
            ContactEmail = ticket.ContactEmail,
            ContactPhone = ticket.ContactPhone,
            HasUnreadStaffReply = ticket.HasUnreadStaffReply,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            FirstResponseAt = ticket.FirstResponseAt,
            ResolvedAt = ticket.ResolvedAt,
            LastActivityAt = ticket.LastActivityAt,
            Messages = messages,
        };
    }

    private static string ResolveSupportPriority(CustomerSupportTicket ticket)
    {
        var haystack = $"{ticket.Category} {ticket.Subject}".ToLowerInvariant();
        if (haystack.Contains("thanh toan") || haystack.Contains("hoan") || haystack.Contains("refund") || haystack.Contains("ve"))
            return "high";

        return ticket.OrderId.HasValue ? "medium" : "low";
    }

    private static TenantPayoutAccountDto MapPayoutAccount(CustomerTenantPayoutAccount entity)
    {
        return new TenantPayoutAccountDto
        {
            Id = entity.Id,
            BankName = entity.BankName,
            AccountNumber = entity.AccountNumber,
            AccountHolder = entity.AccountHolder,
            BankBranch = entity.BankBranch,
            Note = entity.Note,
            IsDefault = entity.IsDefault,
            IsVerified = entity.IsVerified,
            VerifiedAt = entity.VerifiedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    private static IEnumerable<TenantFinanceTransactionDto> BuildTenantTransactions(IEnumerable<CustomerOrder> orders)
    {
        foreach (var order in orders)
        {
            if (!order.PaidAt.HasValue)
                continue;

            yield return new TenantFinanceTransactionDto
            {
                Id = order.OrderCode,
                Type = "order",
                Description = ExtractSnapshotTitle(order.SnapshotJson, order.OrderCode),
                CurrencyCode = order.CurrencyCode,
                GrossAmount = order.GrossAmount,
                CommissionAmount = order.PlatformCommissionAmount,
                NetAmount = GetOrderNetAfterRefund(order),
                RefundAmount = order.RefundedAmount,
                SettlementStatus = order.SettlementStatus,
                OccurredAt = order.PaidAt.Value,
                OrderCode = order.OrderCode,
            };

            if (order.RefundedAmount > 0)
            {
                var commissionRefund = CalculateCommissionRefund(order.PlatformCommissionAmount, order.PayableAmount, order.RefundedAmount, order.CurrencyCode);
                yield return new TenantFinanceTransactionDto
                {
                    Id = $"{order.OrderCode}-RF",
                    Type = "refund",
                    Description = $"Refund adjustment for {order.OrderCode}",
                    CurrencyCode = order.CurrencyCode,
                    GrossAmount = 0m,
                    CommissionAmount = -commissionRefund,
                    NetAmount = -(order.RefundedAmount - commissionRefund),
                    RefundAmount = order.RefundedAmount,
                    SettlementStatus = order.SettlementStatus,
                    OccurredAt = order.UpdatedAt ?? order.PaidAt.Value,
                    OrderCode = order.OrderCode,
                };
            }
        }
    }

    private static decimal GetOrderNetAfterRefund(CustomerOrder order)
    {
        var deduction = order.RefundedAmount <= 0
            ? 0m
            : order.RefundedAmount - CalculateCommissionRefund(order.PlatformCommissionAmount, order.PayableAmount, order.RefundedAmount, order.CurrencyCode);
        return order.TenantNetAmount - deduction;
    }

    private static decimal CalculateCommissionRefund(decimal commissionAmount, decimal payableAmount, decimal refundedAmount, string currencyCode)
    {
        if (commissionAmount <= 0 || payableAmount <= 0 || refundedAmount <= 0)
            return 0m;

        var decimals = string.Equals(currencyCode, "VND", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        var raw = commissionAmount * refundedAmount / payableAmount;
        return decimal.Round(raw, decimals, MidpointRounding.AwayFromZero);
    }

    private static string ExtractSnapshotTitle(string? snapshotJson, string fallback)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
            return fallback;

        try
        {
            using var document = JsonDocument.Parse(snapshotJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("title", out var titleElement) &&
                titleElement.ValueKind == JsonValueKind.String)
            {
                return titleElement.GetString() ?? fallback;
            }
        }
        catch
        {
        }

        return fallback;
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? throw new InvalidOperationException(errorMessage);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeInternalNote(ReviewRefundRequest? request)
    {
        return NormalizeOptional(request?.InternalNote) ?? NormalizeOptional(request?.ReviewNote);
    }

    private static string? NormalizeInternalNote(CompleteRefundRequest? request)
    {
        return NormalizeOptional(request?.InternalNote) ?? NormalizeOptional(request?.ReviewNote);
    }

    private static string? MaskAccountNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
            return trimmed;

        return $"{new string('*', Math.Max(0, trimmed.Length - 4))}{trimmed[^4..]}";
    }

    private async Task ExecuteWithoutTenantScopeAsync(Func<Task> action)
    {
        var originalTenantId = _tenantContext.TenantId;
        var originalRequiresTenantForWrite = _tenantContext.RequiresTenantForWrite;
        try
        {
            _tenantContext.SetRequiresTenantForWrite(false);
            _tenantContext.SetTenant(null);
            await action();
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
            _tenantContext.SetRequiresTenantForWrite(originalRequiresTenantForWrite);
        }
    }
}
