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

    public async Task<AdminCommerceBookingDetailDto> GetBookingDetailAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        var row = await (
            from order in _db.CustomerOrders.AsNoTracking()
            join user in _db.Users.AsNoTracking() on order.UserId equals user.Id
            join tenant in _db.Tenants.AsNoTracking() on order.TenantId equals tenant.Id
            where order.Id == orderId && !order.IsDeleted && !tenant.IsDeleted
            select new
            {
                Order = order,
                CustomerName = user.FullName ?? user.Email ?? order.ContactFullName,
                TenantName = tenant.Name,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Khong tim thay booking.");

        var payments = await _db.CustomerPayments
            .AsNoTracking()
            .Where(x => x.OrderId == row.Order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var tickets = await _db.CustomerTickets
            .AsNoTracking()
            .Where(x => x.OrderId == row.Order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var refunds = await _db.CustomerRefundRequests
            .AsNoTracking()
            .Where(x => x.OrderId == row.Order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var supportTickets = await _db.CustomerSupportTickets
            .AsNoTracking()
            .Where(x => x.OrderId == row.Order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.LastActivityAt ?? x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync(ct);

        var settlementLines = await _db.CustomerSettlementBatchLines
            .AsNoTracking()
            .Where(x => x.OrderId == row.Order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.SettledAt ?? x.CreatedAt)
            .ToListAsync(ct);

        var batchIds = settlementLines.Select(x => x.BatchId).Distinct().ToList();
        var batches = await _db.CustomerSettlementBatches
            .AsNoTracking()
            .Where(x => batchIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct);

        var paymentMap = payments
            .GroupBy(x => x.OrderId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(item => item.CreatedAt).First());

        var ticketMap = tickets
            .GroupBy(x => x.OrderId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(item => item.IssuedAt).ThenByDescending(item => item.CreatedAt).First());

        var latestRefundMap = refunds
            .GroupBy(x => x.OrderId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(item => item.RequestedAt).ThenByDescending(item => item.CreatedAt).First());

        var supportSummary = supportTickets.Count == 0
            ? null
            : new
            {
                Total = supportTickets.Count,
                Open = supportTickets.Count(item => item.Status is CustomerSupportTicketStatus.Open or CustomerSupportTicketStatus.Processing),
            };

        paymentMap.TryGetValue(row.Order.Id, out var latestPayment);
        ticketMap.TryGetValue(row.Order.Id, out var latestTicket);
        latestRefundMap.TryGetValue(row.Order.Id, out var latestRefund);

        var booking = MapBookingItem(
            row.Order,
            row.CustomerName,
            row.TenantName,
            latestPayment,
            latestTicket,
            latestRefund,
            supportSummary?.Total ?? 0,
            supportSummary?.Open ?? 0);

        var paymentItems = payments.Select(payment => new AdminCommercePaymentItemDto
        {
            Id = payment.Id,
            OrderId = row.Order.Id,
            TenantId = row.Order.TenantId,
            TenantName = row.TenantName,
            PaymentCode = payment.PaymentCode,
            OrderCode = row.Order.OrderCode,
            CustomerName = row.CustomerName,
            ServiceTitle = booking.ServiceTitle,
            ProductType = row.Order.ProductType,
            Provider = payment.Provider,
            Method = payment.Method,
            Status = payment.Status,
            CurrencyCode = payment.CurrencyCode,
            Amount = payment.Amount,
            PaidAmount = payment.PaidAmount,
            RefundedAmount = payment.RefundedAmount,
            ProviderInvoiceNumber = payment.ProviderInvoiceNumber,
            ProviderOrderId = payment.ProviderOrderId,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt,
            WebhookReceivedAt = payment.WebhookReceivedAt,
        }).ToList();

        var ticketItems = tickets.Select(ticket => new AdminCommerceBookingTicketDto
        {
            Id = ticket.Id,
            TenantId = ticket.TenantId,
            OrderId = ticket.OrderId,
            TicketCode = ticket.TicketCode,
            Title = ticket.Title,
            Subtitle = ticket.Subtitle,
            ProductType = ticket.ProductType,
            Status = ticket.Status,
            IssuedAt = ticket.IssuedAt,
            CancelledAt = ticket.CancelledAt,
        }).ToList();

        var refundItems = refunds.Select(refund => new AdminCommerceRefundItemDto
        {
            Id = refund.Id,
            OrderId = row.Order.Id,
            PaymentId = refund.PaymentId,
            TenantId = row.Order.TenantId,
            TenantName = row.TenantName,
            RefundCode = refund.RefundCode,
            OrderCode = row.Order.OrderCode,
            CustomerName = row.CustomerName,
            ServiceTitle = booking.ServiceTitle,
            ProductType = row.Order.ProductType,
            Status = refund.Status,
            SettlementStatus = row.Order.SettlementStatus,
            CurrencyCode = refund.CurrencyCode,
            PayableAmount = row.Order.PayableAmount,
            AlreadyRefundedAmount = row.Order.RefundedAmount,
            RemainingRefundableAmount = Math.Max(0m, row.Order.PayableAmount - row.Order.RefundedAmount),
            RequestedAmount = refund.RequestedAmount,
            ApprovedAmount = refund.ApprovedAmount,
            RefundedAmount = refund.RefundedAmount,
            ReasonCode = refund.ReasonCode,
            ReasonText = refund.ReasonText,
            InternalNote = refund.ReviewNote,
            RefundReference = refund.RefundReference,
            SettlementImpactNote = row.Order.SettlementStatus is CustomerSettlementStatus.Settled or CustomerSettlementStatus.InSettlement
                ? "Refund se tao dong dieu chinh trong settlement."
                : "Refund se tru vao payout chua doi soat.",
            RequestedAt = refund.RequestedAt,
            ReviewedAt = refund.ReviewedAt,
            CompletedAt = refund.CompletedAt,
        }).ToList();

        var settlementItems = settlementLines.Select(line =>
        {
            batches.TryGetValue(line.BatchId, out var batch);

            return new AdminCommerceSettlementLineDto
            {
                Id = line.Id,
                BatchId = line.BatchId,
                BatchCode = batch?.BatchCode ?? "",
                BatchStatus = batch?.Status ?? CustomerSettlementBatchStatus.Draft,
                TenantId = line.TenantId,
                TenantName = row.TenantName,
                PaymentId = line.PaymentId,
                RefundRequestId = line.RefundRequestId,
                Status = line.Status,
                CurrencyCode = line.CurrencyCode,
                GrossAmount = line.GrossAmount,
                CommissionAmount = line.CommissionAmount,
                CommissionAdjustmentAmount = line.CommissionAdjustmentAmount,
                TenantNetAmount = line.TenantNetAmount,
                RefundAmount = line.RefundAmount,
                NetPayoutAmount = line.NetPayoutAmount,
                Description = line.Description,
                SettledAt = line.SettledAt,
            };
        }).ToList();

        return new AdminCommerceBookingDetailDto
        {
            Booking = booking,
            Payments = paymentItems,
            Tickets = ticketItems,
            Refunds = refundItems,
            SupportTickets = supportTickets
                .Select(ticket => MapSupportTicket(ticket, row.CustomerName, row.Order.OrderCode, row.TenantName))
                .ToList(),
            SettlementLines = settlementItems,
            Timeline = BuildBookingTimeline(row.Order, payments, tickets, refunds, supportTickets, settlementItems),
        };
    }

    private static AdminCommerceBookingItemDto MapBookingItem(
        CustomerOrder order,
        string customerName,
        string tenantName,
        CustomerPayment? payment,
        CustomerTicket? ticket,
        CustomerRefundRequest? refund,
        int supportTicketCount,
        int openSupportTicketCount)
    {
        return new AdminCommerceBookingItemDto
        {
            Id = order.Id,
            TenantId = order.TenantId,
            OrderCode = order.OrderCode,
            CurrencyCode = order.CurrencyCode,
            CustomerName = customerName,
            ContactEmail = order.ContactEmail,
            ContactPhone = order.ContactPhone,
            TenantName = tenantName,
            ServiceTitle = ExtractSnapshotTitle(order.SnapshotJson, order.OrderCode),
            ServiceSubtitle = ExtractSnapshotSubtitle(order.SnapshotJson, tenantName),
            ProductType = order.ProductType,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            TicketStatus = order.TicketStatus,
            RefundStatus = order.RefundStatus,
            SettlementStatus = order.SettlementStatus,
            GrossAmount = order.GrossAmount,
            PlatformCommissionAmount = order.PlatformCommissionAmount,
            TenantNetAmount = order.TenantNetAmount,
            PayableAmount = order.PayableAmount,
            RefundedAmount = order.RefundedAmount,
            CreatedAt = order.CreatedAt,
            ExpiresAt = order.ExpiresAt,
            PaidAt = order.PaidAt,
            TicketIssuedAt = order.TicketIssuedAt,
            CancelledAt = order.CancelledAt,
            CompletedAt = order.CompletedAt,
            FailureReason = order.FailureReason,
            PaymentCode = payment?.PaymentCode,
            ProviderInvoiceNumber = payment?.ProviderInvoiceNumber,
            TicketCode = ticket?.TicketCode,
            LatestRefundCode = refund?.RefundCode,
            LatestRefundAmount = refund?.RefundedAmount ?? refund?.ApprovedAmount ?? refund?.RequestedAmount,
            SupportTicketCount = supportTicketCount,
            OpenSupportTicketCount = openSupportTicketCount,
        };
    }

    private static List<AdminCommerceBookingTimelineEventDto> BuildBookingTimeline(
        CustomerOrder order,
        IEnumerable<CustomerPayment> payments,
        IEnumerable<CustomerTicket> tickets,
        IEnumerable<CustomerRefundRequest> refunds,
        IEnumerable<CustomerSupportTicket> supportTickets,
        IEnumerable<AdminCommerceSettlementLineDto> settlementLines)
    {
        var events = new List<AdminCommerceBookingTimelineEventDto>
        {
            new()
            {
                Key = "created",
                Title = "Booking created",
                Description = order.OrderCode,
                OccurredAt = order.CreatedAt,
                Tone = "default",
            },
        };

        foreach (var payment in payments)
        {
            events.Add(new AdminCommerceBookingTimelineEventDto
            {
                Key = $"payment-{payment.Id}",
                Title = payment.PaidAt.HasValue ? "Payment paid" : "Payment created",
                Description = payment.PaymentCode,
                OccurredAt = payment.PaidAt ?? payment.CreatedAt,
                Tone = payment.Status == CustomerPaymentStatus.Paid ? "success" : payment.Status == CustomerPaymentStatus.Failed ? "danger" : "info",
            });
        }

        foreach (var ticket in tickets)
        {
            events.Add(new AdminCommerceBookingTimelineEventDto
            {
                Key = $"ticket-{ticket.Id}",
                Title = "Ticket issued",
                Description = ticket.TicketCode,
                OccurredAt = ticket.IssuedAt,
                Tone = ticket.Status == CustomerTicketStatus.Issued ? "success" : "default",
            });
        }

        foreach (var refund in refunds)
        {
            events.Add(new AdminCommerceBookingTimelineEventDto
            {
                Key = $"refund-request-{refund.Id}",
                Title = "Refund requested",
                Description = refund.RefundCode,
                OccurredAt = refund.RequestedAt,
                Tone = "warning",
            });

            if (refund.CompletedAt.HasValue)
            {
                events.Add(new AdminCommerceBookingTimelineEventDto
                {
                    Key = $"refund-completed-{refund.Id}",
                    Title = "Refund completed",
                    Description = refund.RefundReference ?? refund.RefundCode,
                    OccurredAt = refund.CompletedAt.Value,
                    Tone = "info",
                });
            }
        }

        foreach (var support in supportTickets)
        {
            events.Add(new AdminCommerceBookingTimelineEventDto
            {
                Key = $"support-{support.Id}",
                Title = "Support ticket",
                Description = support.TicketCode,
                OccurredAt = support.CreatedAt,
                Tone = support.Status == CustomerSupportTicketStatus.Resolved ? "success" : "info",
            });
        }

        foreach (var line in settlementLines)
        {
            events.Add(new AdminCommerceBookingTimelineEventDto
            {
                Key = $"settlement-{line.Id}",
                Title = line.RefundRequestId.HasValue ? "Settlement adjustment" : "Settlement line",
                Description = string.IsNullOrWhiteSpace(line.BatchCode) ? line.Description : $"{line.BatchCode} - {line.Description}",
                OccurredAt = line.SettledAt ?? order.SettledAt ?? order.UpdatedAt ?? order.CreatedAt,
                Tone = line.Status == CustomerSettlementStatus.Settled ? "success" : "info",
            });
        }

        if (order.CancelledAt.HasValue)
        {
            events.Add(new AdminCommerceBookingTimelineEventDto
            {
                Key = "cancelled",
                Title = "Booking cancelled",
                Description = order.FailureReason,
                OccurredAt = order.CancelledAt.Value,
                Tone = "danger",
            });
        }

        return events
            .OrderBy(x => x.OccurredAt)
            .ToList();
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
