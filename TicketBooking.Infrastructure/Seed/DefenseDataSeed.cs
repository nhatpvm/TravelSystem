using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketBooking.Domain.Commerce;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed;

public static class DefenseDataSeed
{
    private const string TenantCode = "NX001";
    private const string CustomerEmail = "customer@ticketbooking.local";

    private const string PaidOrderCode = "BK-DEF-PAID-001";
    private const string RefundOrderCode = "BK-DEF-REFUND-001";
    private const string PaidPaymentCode = "PAY-DEF-PAID-001";
    private const string RefundPaymentCode = "PAY-DEF-REFUND-001";
    private const string PaidTicketCode = "TKT-DEF-PAID-001";
    private const string RefundTicketCode = "TKT-DEF-REFUND-001";
    private const string RefundCode = "REF-DEF-PARTIAL-001";
    private const string SupportTicketCode = "SUP-DEF-001";
    private const string SettlementBatchCode = "SET-DEF-LOCKED-001";
    private const string OnboardingTrackingCode = "ONB-DEFENSE-PENDING-001";

    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<AppUser> userManager,
        ILogger logger,
        object tenantCtxObj,
        string contentRootPath,
        CancellationToken ct = default)
    {
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .Where(x => x.Code == TenantCode && !x.IsDeleted)
            .Select(x => new { x.Id, x.Code, x.Name })
            .FirstOrDefaultAsync(ct);

        if (tenant is null)
        {
            logger.LogWarning("Defense data seed skipped: tenant {TenantCode} not found.", TenantCode);
            return;
        }

        var customer = await userManager.FindByEmailAsync(CustomerEmail);
        if (customer is null)
        {
            logger.LogWarning("Defense data seed skipped: customer user {Email} not found.", CustomerEmail);
            return;
        }

        SetTenantContextForSeed(tenantCtxObj, tenant.Id, tenant.Code);
        try
        {
            await SeedCommerceDataAsync(db, tenant.Id, tenant.Name, customer, logger, ct);
        }
        finally
        {
            ClearTenantContextForSeed(tenantCtxObj);
        }

        await SeedPendingOnboardingAsync(contentRootPath, logger, ct);
    }

    private static async Task SeedCommerceDataAsync(
        AppDbContext db,
        Guid tenantId,
        string tenantName,
        AppUser customer,
        ILogger logger,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var paidAt = now.AddDays(-8);
        var refundPaidAt = now.AddDays(-6);
        var refundRequestedAt = now.AddDays(-4);
        var refundCompletedAt = now.AddDays(-2);
        var settledAt = now.AddDays(-1);

        var paidOrder = await EnsureOrderAsync(db, PaidOrderCode, ct);
        ApplyOrder(
            paidOrder,
            tenantId,
            customer,
            CustomerOrderStatus.Paid,
            CustomerPaymentStatus.Paid,
            CustomerTicketStatus.Issued,
            CustomerRefundStatus.None,
            CustomerSettlementStatus.Settled,
            grossAmount: 2_400_000m,
            commissionAmount: 240_000m,
            refundedAmount: 0m,
            title: "Vé xe NX001 Sài Gòn - Đà Lạt",
            subtitle: "Booking đã thanh toán cho buổi bảo vệ",
            paidAt,
            settledAt,
            now);

        var refundOrder = await EnsureOrderAsync(db, RefundOrderCode, ct);
        ApplyOrder(
            refundOrder,
            tenantId,
            customer,
            CustomerOrderStatus.RefundedPartial,
            CustomerPaymentStatus.RefundedPartial,
            CustomerTicketStatus.Issued,
            CustomerRefundStatus.RefundedPartial,
            CustomerSettlementStatus.Adjusted,
            grossAmount: 3_200_000m,
            commissionAmount: 320_000m,
            refundedAmount: 500_000m,
            title: "Vé xe NX001 Hà Nội - Sa Pa",
            subtitle: "Booking hoàn tiền một phần cho buổi bảo vệ",
            refundPaidAt,
            settledAt,
            now);

        var paidPayment = await EnsurePaymentAsync(db, PaidPaymentCode, ct);
        ApplyPayment(
            paidPayment,
            tenantId,
            customer.Id,
            paidOrder.Id,
            PaidPaymentCode,
            "INV-DEF-PAID-001",
            CustomerPaymentStatus.Paid,
            paidOrder.PayableAmount,
            paidAmount: paidOrder.PayableAmount,
            refundedAmount: 0m,
            paidAt,
            now);

        var refundPayment = await EnsurePaymentAsync(db, RefundPaymentCode, ct);
        ApplyPayment(
            refundPayment,
            tenantId,
            customer.Id,
            refundOrder.Id,
            RefundPaymentCode,
            "INV-DEF-REFUND-001",
            CustomerPaymentStatus.RefundedPartial,
            refundOrder.PayableAmount,
            paidAmount: refundOrder.PayableAmount,
            refundedAmount: refundOrder.RefundedAmount,
            refundPaidAt,
            now);

        var paidTicket = await EnsureTicketAsync(db, PaidTicketCode, ct);
        ApplyTicket(
            paidTicket,
            tenantId,
            customer.Id,
            paidOrder.Id,
            PaidTicketCode,
            "Vé xe NX001 Sài Gòn - Đà Lạt",
            "Ghế A01, A02 - đã xuất vé",
            paidAt.AddMinutes(5),
            now);

        var refundTicket = await EnsureTicketAsync(db, RefundTicketCode, ct);
        ApplyTicket(
            refundTicket,
            tenantId,
            customer.Id,
            refundOrder.Id,
            RefundTicketCode,
            "Vé xe NX001 Hà Nội - Sa Pa",
            "Ghế B05, B06 - hoàn tiền một phần",
            refundPaidAt.AddMinutes(5),
            now);

        var refund = await EnsureRefundAsync(db, RefundCode, ct);
        ApplyRefund(
            refund,
            tenantId,
            customer.Id,
            refundOrder.Id,
            refundPayment.Id,
            refundRequestedAt,
            refundCompletedAt,
            now);

        var supportTicket = await EnsureSupportTicketAsync(db, SupportTicketCode, ct);
        ApplySupportTicket(
            supportTicket,
            tenantId,
            customer,
            refundOrder.Id,
            refundRequestedAt.AddHours(2),
            now);

        var payoutAccount = await EnsurePayoutAccountAsync(db, tenantId, now, ct);
        ApplyPayoutAccount(payoutAccount, tenantId, tenantName, now);

        var batch = await EnsureSettlementBatchAsync(db, SettlementBatchCode, ct);
        ApplySettlementBatch(batch, now, settledAt);

        paidOrder.SettlementBatchId = batch.Id;
        refundOrder.SettlementBatchId = batch.Id;

        var paidLine = await EnsureSettlementLineAsync(db, batch.Id, paidOrder.Id, null, ct);
        ApplySettlementLine(
            paidLine,
            batch.Id,
            tenantId,
            paidOrder.Id,
            paidPayment.Id,
            refundRequestId: null,
            CustomerSettlementStatus.Settled,
            grossAmount: paidOrder.GrossAmount,
            commissionAmount: paidOrder.PlatformCommissionAmount,
            commissionAdjustmentAmount: 0m,
            tenantNetAmount: paidOrder.TenantNetAmount,
            refundAmount: 0m,
            netPayoutAmount: paidOrder.TenantNetAmount,
            "Doanh thu booking đã thanh toán",
            settledAt,
            now);

        var refundSaleLine = await EnsureSettlementLineAsync(db, batch.Id, refundOrder.Id, null, ct);
        ApplySettlementLine(
            refundSaleLine,
            batch.Id,
            tenantId,
            refundOrder.Id,
            refundPayment.Id,
            refundRequestId: null,
            CustomerSettlementStatus.Settled,
            grossAmount: refundOrder.GrossAmount,
            commissionAmount: refundOrder.PlatformCommissionAmount,
            commissionAdjustmentAmount: 0m,
            tenantNetAmount: refundOrder.TenantNetAmount,
            refundAmount: 0m,
            netPayoutAmount: refundOrder.TenantNetAmount,
            "Doanh thu gốc trước hoàn tiền một phần",
            settledAt,
            now);

        var refundAdjustmentLine = await EnsureSettlementLineAsync(db, batch.Id, refundOrder.Id, refund.Id, ct);
        ApplySettlementLine(
            refundAdjustmentLine,
            batch.Id,
            tenantId,
            refundOrder.Id,
            refundPayment.Id,
            refund.Id,
            CustomerSettlementStatus.Adjusted,
            grossAmount: 0m,
            commissionAmount: 0m,
            commissionAdjustmentAmount: 50_000m,
            tenantNetAmount: 0m,
            refundAmount: refund.RefundedAmount ?? 0m,
            netPayoutAmount: -450_000m,
            "Điều chỉnh hoàn tiền một phần",
            settledAt,
            now);

        RecalculateBatch(batch, paidLine, refundSaleLine, refundAdjustmentLine);

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Defense data seed completed: {PaidOrderCode}, {RefundOrderCode}, {BatchCode}, {SupportTicketCode}.",
            PaidOrderCode,
            RefundOrderCode,
            SettlementBatchCode,
            SupportTicketCode);
    }

    private static async Task<CustomerOrder> EnsureOrderAsync(AppDbContext db, string orderCode, CancellationToken ct)
    {
        var order = await db.CustomerOrders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrderCode == orderCode, ct);

        if (order is not null)
            return order;

        order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = orderCode
        };
        db.CustomerOrders.Add(order);
        return order;
    }

    private static void ApplyOrder(
        CustomerOrder order,
        Guid tenantId,
        AppUser customer,
        CustomerOrderStatus status,
        CustomerPaymentStatus paymentStatus,
        CustomerTicketStatus ticketStatus,
        CustomerRefundStatus refundStatus,
        CustomerSettlementStatus settlementStatus,
        decimal grossAmount,
        decimal commissionAmount,
        decimal refundedAmount,
        string title,
        string subtitle,
        DateTimeOffset paidAt,
        DateTimeOffset settledAt,
        DateTimeOffset now)
    {
        order.TenantId = tenantId;
        order.UserId = customer.Id;
        order.ProductType = CustomerProductType.Bus;
        order.CurrencyCode = "VND";
        order.GrossAmount = grossAmount;
        order.DiscountAmount = 0m;
        order.ServiceFeeAmount = 0m;
        order.PlatformCommissionAmount = commissionAmount;
        order.TenantNetAmount = grossAmount - commissionAmount;
        order.PayableAmount = grossAmount;
        order.RefundedAmount = refundedAmount;
        order.Status = status;
        order.PaymentStatus = paymentStatus;
        order.TicketStatus = ticketStatus;
        order.RefundStatus = refundStatus;
        order.SettlementStatus = settlementStatus;
        order.ContactFullName = string.IsNullOrWhiteSpace(customer.FullName) ? "Nguyễn Minh Anh" : customer.FullName;
        order.ContactPhone = string.IsNullOrWhiteSpace(customer.PhoneNumber) ? "0900000000" : customer.PhoneNumber;
        order.ContactEmail = string.IsNullOrWhiteSpace(customer.Email) ? CustomerEmail : customer.Email;
        order.VatInvoiceRequested = false;
        order.CustomerNote = "Dữ liệu ổn định dùng cho buổi bảo vệ dự án.";
        order.SnapshotJson = SerializeJson(new
        {
            title,
            subtitle,
            productType = "Bus",
            routeFrom = title.Contains("Sài Gòn", StringComparison.OrdinalIgnoreCase) ? "TP.HCM" : "Hà Nội",
            routeTo = title.Contains("Đà Lạt", StringComparison.OrdinalIgnoreCase) ? "Đà Lạt" : "Sa Pa",
            departureAt = paidAt.AddDays(10),
            passengers = 2,
            defenseLocked = true
        });
        order.MetadataJson = SerializeJson(new
        {
            defenseLocked = true,
            scenario = order.OrderCode == PaidOrderCode ? "booking-paid" : "booking-partial-refund"
        });
        order.FailureReason = null;
        order.ExpiresAt = paidAt.AddMinutes(30);
        order.PaidAt = paidAt;
        order.TicketIssuedAt = paidAt.AddMinutes(5);
        order.CancelledAt = null;
        order.CompletedAt = null;
        order.SettledAt = settledAt;
        order.IsDeleted = false;
        order.CreatedAt = order.CreatedAt == default ? paidAt.AddMinutes(-20) : order.CreatedAt;
        order.CreatedByUserId = customer.Id;
        order.UpdatedAt = now;
        order.UpdatedByUserId = customer.Id;
    }

    private static async Task<CustomerPayment> EnsurePaymentAsync(AppDbContext db, string paymentCode, CancellationToken ct)
    {
        var payment = await db.CustomerPayments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.PaymentCode == paymentCode, ct);

        if (payment is not null)
            return payment;

        payment = new CustomerPayment
        {
            Id = Guid.NewGuid(),
            PaymentCode = paymentCode
        };
        db.CustomerPayments.Add(payment);
        return payment;
    }

    private static void ApplyPayment(
        CustomerPayment payment,
        Guid tenantId,
        Guid userId,
        Guid orderId,
        string paymentCode,
        string invoiceNumber,
        CustomerPaymentStatus status,
        decimal amount,
        decimal paidAmount,
        decimal refundedAmount,
        DateTimeOffset paidAt,
        DateTimeOffset now)
    {
        payment.TenantId = tenantId;
        payment.UserId = userId;
        payment.OrderId = orderId;
        payment.Provider = CustomerPaymentProvider.SePay;
        payment.Method = CustomerPaymentMethod.BankTransfer;
        payment.Status = status;
        payment.PaymentCode = paymentCode;
        payment.ProviderInvoiceNumber = invoiceNumber;
        payment.ProviderOrderId = $"DEF-{paymentCode}";
        payment.CurrencyCode = "VND";
        payment.Amount = amount;
        payment.PaidAmount = paidAmount;
        payment.RefundedAmount = refundedAmount;
        payment.RequestPayloadJson = SerializeJson(new { defenseLocked = true, provider = "SePay" });
        payment.ProviderResponseJson = SerializeJson(new { defenseLocked = true, status = "paid" });
        payment.LastWebhookJson = SerializeJson(new { defenseLocked = true, eventName = "payment.paid" });
        payment.FailureReason = null;
        payment.ExpiresAt = paidAt.AddMinutes(30);
        payment.PaidAt = paidAt;
        payment.CancelledAt = null;
        payment.FailedAt = null;
        payment.LastSyncedAt = now;
        payment.WebhookReceivedAt = paidAt.AddMinutes(1);
        payment.IsDeleted = false;
        payment.CreatedAt = payment.CreatedAt == default ? paidAt.AddMinutes(-15) : payment.CreatedAt;
        payment.CreatedByUserId = userId;
        payment.UpdatedAt = now;
        payment.UpdatedByUserId = userId;
    }

    private static async Task<CustomerTicket> EnsureTicketAsync(AppDbContext db, string ticketCode, CancellationToken ct)
    {
        var ticket = await db.CustomerTickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TicketCode == ticketCode, ct);

        if (ticket is not null)
            return ticket;

        ticket = new CustomerTicket
        {
            Id = Guid.NewGuid(),
            TicketCode = ticketCode
        };
        db.CustomerTickets.Add(ticket);
        return ticket;
    }

    private static void ApplyTicket(
        CustomerTicket ticket,
        Guid tenantId,
        Guid userId,
        Guid orderId,
        string ticketCode,
        string title,
        string subtitle,
        DateTimeOffset issuedAt,
        DateTimeOffset now)
    {
        ticket.TenantId = tenantId;
        ticket.UserId = userId;
        ticket.OrderId = orderId;
        ticket.ProductType = CustomerProductType.Bus;
        ticket.Status = CustomerTicketStatus.Issued;
        ticket.TicketCode = ticketCode;
        ticket.Title = title;
        ticket.Subtitle = subtitle;
        ticket.SnapshotJson = SerializeJson(new
        {
            title,
            subtitle,
            qrPayload = ticketCode,
            defenseLocked = true
        });
        ticket.IssuedAt = issuedAt;
        ticket.CancelledAt = null;
        ticket.IsDeleted = false;
        ticket.CreatedAt = ticket.CreatedAt == default ? issuedAt : ticket.CreatedAt;
        ticket.CreatedByUserId = userId;
        ticket.UpdatedAt = now;
        ticket.UpdatedByUserId = userId;
    }

    private static async Task<CustomerRefundRequest> EnsureRefundAsync(AppDbContext db, string refundCode, CancellationToken ct)
    {
        var refund = await db.CustomerRefundRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.RefundCode == refundCode, ct);

        if (refund is not null)
            return refund;

        refund = new CustomerRefundRequest
        {
            Id = Guid.NewGuid(),
            RefundCode = refundCode
        };
        db.CustomerRefundRequests.Add(refund);
        return refund;
    }

    private static void ApplyRefund(
        CustomerRefundRequest refund,
        Guid tenantId,
        Guid userId,
        Guid orderId,
        Guid paymentId,
        DateTimeOffset requestedAt,
        DateTimeOffset completedAt,
        DateTimeOffset now)
    {
        refund.TenantId = tenantId;
        refund.UserId = userId;
        refund.OrderId = orderId;
        refund.PaymentId = paymentId;
        refund.RefundCode = RefundCode;
        refund.CurrencyCode = "VND";
        refund.Status = CustomerRefundStatus.RefundedPartial;
        refund.RequestedAmount = 500_000m;
        refund.ApprovedAmount = 500_000m;
        refund.RefundedAmount = 500_000m;
        refund.ReasonCode = "CUSTOMER_CHANGE_PLAN";
        refund.ReasonText = "Khách đổi kế hoạch, hoàn tiền một phần theo quy trình đối soát.";
        refund.RefundReference = "DEF-SEPAY-REFUND-001";
        refund.ReviewNote = "Đã duyệt hoàn tiền một phần cho bộ dữ liệu bảo vệ dự án.";
        refund.SnapshotJson = SerializeJson(new
        {
            defenseLocked = true,
            scenario = "partial-refund",
            approvedAmount = 500_000m
        });
        refund.RequestedAt = requestedAt;
        refund.ReviewedAt = requestedAt.AddHours(8);
        refund.CompletedAt = completedAt;
        refund.IsDeleted = false;
        refund.CreatedAt = refund.CreatedAt == default ? requestedAt : refund.CreatedAt;
        refund.CreatedByUserId = userId;
        refund.UpdatedAt = now;
        refund.UpdatedByUserId = userId;
    }

    private static async Task<CustomerSupportTicket> EnsureSupportTicketAsync(AppDbContext db, string ticketCode, CancellationToken ct)
    {
        var ticket = await db.CustomerSupportTickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TicketCode == ticketCode, ct);

        if (ticket is not null)
            return ticket;

        ticket = new CustomerSupportTicket
        {
            Id = Guid.NewGuid(),
            TicketCode = ticketCode
        };
        db.CustomerSupportTickets.Add(ticket);
        return ticket;
    }

    private static void ApplySupportTicket(
        CustomerSupportTicket ticket,
        Guid tenantId,
        AppUser customer,
        Guid orderId,
        DateTimeOffset openedAt,
        DateTimeOffset now)
    {
        ticket.UserId = customer.Id;
        ticket.TenantId = tenantId;
        ticket.OrderId = orderId;
        ticket.TicketCode = SupportTicketCode;
        ticket.Status = CustomerSupportTicketStatus.Processing;
        ticket.Category = "refund";
        ticket.Subject = "Kiểm tra khoản hoàn tiền một phần";
        ticket.Content = "Khách cần xác nhận khoản hoàn tiền một phần đã được xử lý và hiển thị trên booking.";
        ticket.ContactEmail = string.IsNullOrWhiteSpace(customer.Email) ? CustomerEmail : customer.Email;
        ticket.ContactPhone = string.IsNullOrWhiteSpace(customer.PhoneNumber) ? "0900000000" : customer.PhoneNumber;
        ticket.ResolutionNote = null;
        ticket.HasUnreadStaffReply = false;
        ticket.FirstResponseAt = openedAt.AddHours(3);
        ticket.ResolvedAt = null;
        ticket.LastActivityAt = now.AddHours(-12);
        ticket.IsDeleted = false;
        ticket.CreatedAt = ticket.CreatedAt == default ? openedAt : ticket.CreatedAt;
        ticket.CreatedByUserId = customer.Id;
        ticket.UpdatedAt = now;
        ticket.UpdatedByUserId = customer.Id;
    }

    private static async Task<CustomerTenantPayoutAccount> EnsurePayoutAccountAsync(
        AppDbContext db,
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var account = await db.CustomerTenantPayoutAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault, ct);

        if (account is not null)
            return account;

        account = new CustomerTenantPayoutAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now
        };
        db.CustomerTenantPayoutAccounts.Add(account);
        return account;
    }

    private static void ApplyPayoutAccount(
        CustomerTenantPayoutAccount account,
        Guid tenantId,
        string tenantName,
        DateTimeOffset now)
    {
        account.TenantId = tenantId;
        account.BankName = "Vietcombank";
        account.AccountNumber = "0011000012345";
        account.AccountHolder = string.IsNullOrWhiteSpace(tenantName) ? "Công ty Vận tải NX001" : tenantName;
        account.BankBranch = "CN TP.HCM";
        account.Note = "Tài khoản nhận đối soát cho buổi bảo vệ dự án.";
        account.IsDefault = true;
        account.IsVerified = true;
        account.VerifiedAt = now.AddDays(-10);
        account.IsDeleted = false;
        account.CreatedAt = account.CreatedAt == default ? now.AddDays(-10) : account.CreatedAt;
        account.UpdatedAt = now;
    }

    private static async Task<CustomerSettlementBatch> EnsureSettlementBatchAsync(
        AppDbContext db,
        string batchCode,
        CancellationToken ct)
    {
        var batch = await db.CustomerSettlementBatches
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.BatchCode == batchCode, ct);

        if (batch is not null)
            return batch;

        batch = new CustomerSettlementBatch
        {
            Id = Guid.NewGuid(),
            BatchCode = batchCode
        };
        db.CustomerSettlementBatches.Add(batch);
        return batch;
    }

    private static void ApplySettlementBatch(
        CustomerSettlementBatch batch,
        DateTimeOffset now,
        DateTimeOffset settledAt)
    {
        var localNow = now.ToLocalTime();
        var startDate = new DateOnly(localNow.Year, localNow.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        batch.BatchCode = SettlementBatchCode;
        batch.PeriodYear = startDate.Year;
        batch.PeriodMonth = startDate.Month;
        batch.StartDate = startDate;
        batch.EndDate = endDate;
        batch.Status = CustomerSettlementBatchStatus.Completed;
        batch.CurrencyCode = "VND";
        batch.Notes = "Kỳ đối soát đã khóa cho buổi bảo vệ dự án.";
        batch.ApprovedAt = settledAt.AddHours(-2);
        batch.PaidAt = settledAt;
        batch.IsDeleted = false;
        batch.CreatedAt = batch.CreatedAt == default ? now.AddDays(-2) : batch.CreatedAt;
        batch.UpdatedAt = now;
    }

    private static async Task<CustomerSettlementBatchLine> EnsureSettlementLineAsync(
        AppDbContext db,
        Guid batchId,
        Guid orderId,
        Guid? refundRequestId,
        CancellationToken ct)
    {
        var line = await db.CustomerSettlementBatchLines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.BatchId == batchId &&
                x.OrderId == orderId &&
                x.RefundRequestId == refundRequestId,
                ct);

        if (line is not null)
            return line;

        line = new CustomerSettlementBatchLine
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            OrderId = orderId,
            RefundRequestId = refundRequestId
        };
        db.CustomerSettlementBatchLines.Add(line);
        return line;
    }

    private static void ApplySettlementLine(
        CustomerSettlementBatchLine line,
        Guid batchId,
        Guid tenantId,
        Guid orderId,
        Guid paymentId,
        Guid? refundRequestId,
        CustomerSettlementStatus status,
        decimal grossAmount,
        decimal commissionAmount,
        decimal commissionAdjustmentAmount,
        decimal tenantNetAmount,
        decimal refundAmount,
        decimal netPayoutAmount,
        string description,
        DateTimeOffset settledAt,
        DateTimeOffset now)
    {
        line.BatchId = batchId;
        line.TenantId = tenantId;
        line.OrderId = orderId;
        line.PaymentId = paymentId;
        line.RefundRequestId = refundRequestId;
        line.Status = status;
        line.CurrencyCode = "VND";
        line.GrossAmount = grossAmount;
        line.CommissionAmount = commissionAmount;
        line.CommissionAdjustmentAmount = commissionAdjustmentAmount;
        line.TenantNetAmount = tenantNetAmount;
        line.RefundAmount = refundAmount;
        line.NetPayoutAmount = netPayoutAmount;
        line.Description = description;
        line.MetadataJson = SerializeJson(new { defenseLocked = true });
        line.SettledAt = settledAt;
        line.IsDeleted = false;
        line.CreatedAt = line.CreatedAt == default ? now.AddDays(-2) : line.CreatedAt;
        line.UpdatedAt = now;
    }

    private static void RecalculateBatch(
        CustomerSettlementBatch batch,
        params CustomerSettlementBatchLine[] lines)
    {
        batch.TotalGrossAmount = lines.Sum(x => x.GrossAmount);
        batch.TotalCommissionAmount = lines.Sum(x => x.CommissionAmount);
        batch.TotalCommissionAdjustmentAmount = lines.Sum(x => x.CommissionAdjustmentAmount);
        batch.TotalTenantNetAmount = lines.Sum(x => x.TenantNetAmount);
        batch.TotalRefundAmount = lines.Sum(x => x.RefundAmount);
        batch.TotalNetPayoutAmount = lines.Sum(x => x.NetPayoutAmount);
        batch.TenantCount = lines.Select(x => x.TenantId).Distinct().Count();
        batch.LineCount = lines.Length;
    }

    private static async Task SeedPendingOnboardingAsync(
        string contentRootPath,
        ILogger logger,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var onboardingDir = Path.Combine(contentRootPath, "App_Data", "partner-onboarding", OnboardingTrackingCode);
        Directory.CreateDirectory(onboardingDir);

        var legalDocumentFileName = "giay-phep-kinh-doanh-minh-nhat.pdf";
        var legalDocumentPath = Path.Combine(onboardingDir, legalDocumentFileName);
        if (!File.Exists(legalDocumentPath))
        {
            await File.WriteAllTextAsync(
                legalDocumentPath,
                "%PDF-1.4\n% Defense legal document placeholder.\n",
                ct);
        }

        var metadata = new
        {
            trackingCode = OnboardingTrackingCode,
            serviceType = "hotel",
            businessName = "Khách sạn Minh Nhật Riverside",
            taxCode = "0312345678",
            address = "01 Nguyễn Huệ, Quận 1, TP.HCM",
            contactEmail = "pending-partner@ticketbooking.local",
            contactPhone = "0909123456",
            status = "PendingReview",
            submittedAt = now.AddDays(-1),
            reviewedAt = (DateTimeOffset?)null,
            reviewedBy = (string?)null,
            reviewNote = (string?)null,
            reviewerNote = (string?)null,
            rejectReason = (string?)null,
            needMoreInfoReason = (string?)null,
            legalDocument = new
            {
                originalFileName = "Giay-phep-kinh-doanh-minh-nhat.pdf",
                storedFileName = legalDocumentFileName,
                contentType = "application/pdf",
                sizeBytes = new FileInfo(legalDocumentPath).Length
            }
        };

        var metadataPath = Path.Combine(onboardingDir, "metadata.json");
        await File.WriteAllTextAsync(metadataPath, SerializeJson(metadata, writeIndented: true), ct);

        logger.LogInformation(
            "Defense onboarding seed completed: {TrackingCode}.",
            OnboardingTrackingCode);
    }

    private static string SerializeJson(object value, bool writeIndented = false)
        => JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = writeIndented
            });

    private static void SetTenantContextForSeed(object tenantCtxObj, Guid tenantId, string tenantCode)
    {
        var t = tenantCtxObj.GetType();

        if (TryInvokeMethod(t, tenantCtxObj, new[] { "SetTenant", "SwitchTenant", "UseTenant", "SetCurrentTenant" },
                new object?[] { tenantId, tenantCode }))
            return;

        if (TryInvokeMethod(t, tenantCtxObj, new[] { "SetTenant", "SwitchTenant", "UseTenant", "SetCurrentTenant" },
                new object?[] { tenantId }))
        {
            TrySetProperty(t, tenantCtxObj, "TenantCode", tenantCode);
            TrySetProperty(t, tenantCtxObj, "HasTenant", true);
            return;
        }

        TrySetProperty(t, tenantCtxObj, "TenantId", tenantId);
        TrySetProperty(t, tenantCtxObj, "TenantCode", tenantCode);
        TrySetProperty(t, tenantCtxObj, "HasTenant", true);
    }

    private static void ClearTenantContextForSeed(object tenantCtxObj)
    {
        var t = tenantCtxObj.GetType();

        if (TryInvokeMethod(t, tenantCtxObj, new[] { "Clear", "ClearTenant", "Reset", "ResetTenant" }, Array.Empty<object?>()))
            return;

        TrySetProperty(t, tenantCtxObj, "HasTenant", false);
        TrySetProperty(t, tenantCtxObj, "TenantCode", null);

        var tenantIdProperty = t.GetProperty("TenantId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (tenantIdProperty is null || !tenantIdProperty.CanWrite)
            return;

        if (tenantIdProperty.PropertyType == typeof(Guid?))
            tenantIdProperty.SetValue(tenantCtxObj, null);
        else if (tenantIdProperty.PropertyType == typeof(Guid))
            tenantIdProperty.SetValue(tenantCtxObj, Guid.Empty);
    }

    private static bool TryInvokeMethod(Type type, object target, string[] methodNames, object?[] args)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var name in methodNames)
        {
            var methods = type.GetMethods(flags)
                .Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                    continue;

                var compatible = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (args[i] is null)
                        continue;

                    var argumentType = args[i]!.GetType();
                    var parameterType = parameters[i].ParameterType;
                    if (parameterType == argumentType || parameterType == typeof(Guid?) && argumentType == typeof(Guid) || parameterType.IsAssignableFrom(argumentType))
                        continue;

                    compatible = false;
                    break;
                }

                if (!compatible)
                    continue;

                method.Invoke(target, args);
                return true;
            }
        }

        return false;
    }

    private static void TrySetProperty(Type type, object target, string propertyName, object? value)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is null || !property.CanWrite)
            return;

        if (value is null)
        {
            property.SetValue(target, null);
            return;
        }

        if (property.PropertyType == value.GetType() || property.PropertyType.IsAssignableFrom(value.GetType()))
        {
            property.SetValue(target, value);
            return;
        }

        if (property.PropertyType == typeof(Guid?) && value is Guid guidValue)
            property.SetValue(target, guidValue);
    }
}
