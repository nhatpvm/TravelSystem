using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CommerceBackofficeService
{
    public async Task<AdminCommerceBookingListResponse> ListBookingsAsync(
        string? q,
        CustomerOrderStatus? status,
        CancellationToken ct = default)
    {
        var query =
            from order in _db.CustomerOrders.AsNoTracking()
            join user in _db.Users.AsNoTracking() on order.UserId equals user.Id
            join tenant in _db.Tenants.AsNoTracking() on order.TenantId equals tenant.Id
            where !order.IsDeleted && !tenant.IsDeleted
            select new
            {
                Order = order,
                CustomerName = user.FullName ?? user.Email ?? order.ContactFullName,
                TenantName = tenant.Name,
            };

        if (status.HasValue)
        {
            query = query.Where(x => x.Order.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Order.OrderCode.Contains(keyword) ||
                x.CustomerName.Contains(keyword) ||
                x.TenantName.Contains(keyword) ||
                x.Order.ContactEmail.Contains(keyword) ||
                x.Order.ContactPhone.Contains(keyword) ||
                _db.CustomerPayments.Any(payment =>
                    payment.OrderId == x.Order.Id &&
                    !payment.IsDeleted &&
                    (payment.PaymentCode.Contains(keyword) || payment.ProviderInvoiceNumber.Contains(keyword))) ||
                _db.CustomerTickets.Any(ticket =>
                    ticket.OrderId == x.Order.Id &&
                    !ticket.IsDeleted &&
                    ticket.TicketCode.Contains(keyword)) ||
                _db.CustomerRefundRequests.Any(refund =>
                    refund.OrderId == x.Order.Id &&
                    !refund.IsDeleted &&
                    refund.RefundCode.Contains(keyword)) ||
                _db.CustomerSupportTickets.Any(ticket =>
                    ticket.OrderId == x.Order.Id &&
                    !ticket.IsDeleted &&
                    (ticket.TicketCode.Contains(keyword) || ticket.Subject.Contains(keyword))));
        }

        var rows = await query
            .OrderByDescending(x => x.Order.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        var orderIds = rows.Select(x => x.Order.Id).ToList();
        if (orderIds.Count == 0)
        {
            return new AdminCommerceBookingListResponse();
        }

        var payments = await _db.CustomerPayments
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId) && !x.IsDeleted)
            .ToListAsync(ct);

        var tickets = await _db.CustomerTickets
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId) && !x.IsDeleted)
            .ToListAsync(ct);

        var refunds = await _db.CustomerRefundRequests
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId) && !x.IsDeleted)
            .ToListAsync(ct);

        var supportTickets = await _db.CustomerSupportTickets
            .AsNoTracking()
            .Where(x => x.OrderId.HasValue && orderIds.Contains(x.OrderId.Value) && !x.IsDeleted)
            .ToListAsync(ct);

        var paymentMap = payments
            .GroupBy(x => x.OrderId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(item => item.CreatedAt).First());

        var ticketMap = tickets
            .GroupBy(x => x.OrderId)
            .ToDictionary(
                x => x.Key,
                x => x
                    .OrderByDescending(item => item.IssuedAt)
                    .ThenByDescending(item => item.CreatedAt)
                    .First());

        var latestRefundMap = refunds
            .GroupBy(x => x.OrderId)
            .ToDictionary(
                x => x.Key,
                x => x
                    .OrderByDescending(item => item.RequestedAt)
                    .ThenByDescending(item => item.CreatedAt)
                    .First());

        var supportMap = supportTickets
            .GroupBy(x => x.OrderId!.Value)
            .ToDictionary(
                x => x.Key,
                x => new
                {
                    Total = x.Count(),
                    Open = x.Count(item => item.Status is CustomerSupportTicketStatus.Open or CustomerSupportTicketStatus.Processing),
                });

        var items = rows.Select(row =>
        {
            paymentMap.TryGetValue(row.Order.Id, out var payment);
            ticketMap.TryGetValue(row.Order.Id, out var ticket);
            latestRefundMap.TryGetValue(row.Order.Id, out var refund);
            supportMap.TryGetValue(row.Order.Id, out var support);

            return new AdminCommerceBookingItemDto
            {
                Id = row.Order.Id,
                TenantId = row.Order.TenantId,
                OrderCode = row.Order.OrderCode,
                CurrencyCode = row.Order.CurrencyCode,
                CustomerName = row.CustomerName,
                ContactEmail = row.Order.ContactEmail,
                ContactPhone = row.Order.ContactPhone,
                TenantName = row.TenantName,
                ServiceTitle = ExtractSnapshotTitle(row.Order.SnapshotJson, row.Order.OrderCode),
                ServiceSubtitle = ExtractSnapshotSubtitle(row.Order.SnapshotJson, row.TenantName),
                ProductType = row.Order.ProductType,
                Status = row.Order.Status,
                PaymentStatus = row.Order.PaymentStatus,
                TicketStatus = row.Order.TicketStatus,
                RefundStatus = row.Order.RefundStatus,
                SettlementStatus = row.Order.SettlementStatus,
                GrossAmount = row.Order.GrossAmount,
                PlatformCommissionAmount = row.Order.PlatformCommissionAmount,
                TenantNetAmount = row.Order.TenantNetAmount,
                PayableAmount = row.Order.PayableAmount,
                RefundedAmount = row.Order.RefundedAmount,
                CreatedAt = row.Order.CreatedAt,
                ExpiresAt = row.Order.ExpiresAt,
                PaidAt = row.Order.PaidAt,
                TicketIssuedAt = row.Order.TicketIssuedAt,
                CancelledAt = row.Order.CancelledAt,
                CompletedAt = row.Order.CompletedAt,
                FailureReason = row.Order.FailureReason,
                PaymentCode = payment?.PaymentCode,
                ProviderInvoiceNumber = payment?.ProviderInvoiceNumber,
                TicketCode = ticket?.TicketCode,
                LatestRefundCode = refund?.RefundCode,
                LatestRefundAmount = refund?.RefundedAmount ?? refund?.ApprovedAmount ?? refund?.RequestedAmount,
                SupportTicketCount = support?.Total ?? 0,
                OpenSupportTicketCount = support?.Open ?? 0,
            };
        }).ToList();

        return new AdminCommerceBookingListResponse
        {
            Summary = new AdminCommerceBookingSummaryDto
            {
                TotalCount = items.Count,
                PendingCount = items.Count(x => x.Status == CustomerOrderStatus.PendingPayment),
                PaidCount = items.Count(x =>
                    x.PaymentStatus is CustomerPaymentStatus.Paid or CustomerPaymentStatus.RefundedPartial or CustomerPaymentStatus.RefundedFull ||
                    x.Status is CustomerOrderStatus.Paid or CustomerOrderStatus.TicketIssued or CustomerOrderStatus.Completed),
                CancelledCount = items.Count(x => x.Status is CustomerOrderStatus.Cancelled or CustomerOrderStatus.Expired or CustomerOrderStatus.Failed),
                RefundAttentionCount = items.Count(x => x.RefundStatus is CustomerRefundStatus.Requested or CustomerRefundStatus.UnderReview or CustomerRefundStatus.Approved or CustomerRefundStatus.Processing or CustomerRefundStatus.RefundedPartial or CustomerRefundStatus.RefundedFull),
                TotalGrossAmount = items.Sum(x => x.GrossAmount),
                TotalCommissionAmount = items.Sum(x => x.PlatformCommissionAmount),
                TotalTenantNetAmount = items.Sum(x => x.TenantNetAmount),
                TotalRefundedAmount = items.Sum(x => x.RefundedAmount),
            },
            Items = items,
        };
    }

    private static string? ExtractSnapshotSubtitle(string? snapshotJson, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(snapshotJson))
        {
            try
            {
                using var document = JsonDocument.Parse(snapshotJson);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (document.RootElement.TryGetProperty("subtitle", out var subtitleElement) &&
                        subtitleElement.ValueKind == JsonValueKind.String)
                    {
                        var subtitle = subtitleElement.GetString();
                        if (!string.IsNullOrWhiteSpace(subtitle))
                        {
                            return subtitle;
                        }
                    }

                    if (document.RootElement.TryGetProperty("providerName", out var providerElement) &&
                        providerElement.ValueKind == JsonValueKind.String)
                    {
                        var provider = providerElement.GetString();
                        if (!string.IsNullOrWhiteSpace(provider))
                        {
                            return provider;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        return fallback;
    }
}
