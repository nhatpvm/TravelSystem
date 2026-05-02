using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Domain.Commerce;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Api.Controllers.Commerce;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/commerce")]
[Authorize(Policy = "perm:admin.commerce.read")]
public sealed class AdminCommerceController : ControllerBase
{
    private readonly CommerceBackofficeService _service;

    public AdminCommerceController(CommerceBackofficeService service)
    {
        _service = service;
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<AdminCommerceBookingListResponse>> ListBookings(
        [FromQuery] string? q = null,
        [FromQuery] CustomerOrderStatus? status = null,
        CancellationToken ct = default)
    {
        return Ok(await _service.ListBookingsAsync(q, status, ct));
    }

    [HttpGet("bookings/{orderId:guid}")]
    public async Task<ActionResult<AdminCommerceBookingDetailDto>> GetBooking(
        Guid orderId,
        CancellationToken ct = default)
    {
        return Ok(await _service.GetBookingDetailAsync(orderId, ct));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<AdminCommercePaymentListResponse>> ListPayments(
        [FromQuery] string? q = null,
        [FromQuery] CustomerPaymentStatus? status = null,
        CancellationToken ct = default)
    {
        return Ok(await _service.ListPaymentsAsync(null, q, status, ct));
    }

    [HttpGet("refunds")]
    public async Task<ActionResult<AdminCommerceRefundListResponse>> ListRefunds(
        [FromQuery] string? q = null,
        [FromQuery] CustomerRefundStatus? status = null,
        CancellationToken ct = default)
    {
        return Ok(await _service.ListRefundsAsync(null, q, status, ct));
    }

    [HttpPost("refunds/{refundId:guid}/approve")]
    public async Task<ActionResult<AdminCommerceRefundItemDto>> ApproveRefund(
        Guid refundId,
        [FromBody] ReviewRefundRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _service.ApproveRefundAsync(refundId, request, GetCurrentUserId(), ct));
    }

    [HttpPost("refunds/{refundId:guid}/reject")]
    public async Task<ActionResult<AdminCommerceRefundItemDto>> RejectRefund(
        Guid refundId,
        [FromBody] ReviewRefundRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _service.RejectRefundAsync(refundId, request, GetCurrentUserId(), ct));
    }

    [HttpPost("refunds/{refundId:guid}/complete")]
    public async Task<ActionResult<AdminCommerceRefundItemDto>> CompleteRefund(
        Guid refundId,
        [FromBody] CompleteRefundRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _service.CompleteRefundAsync(refundId, request, GetCurrentUserId(), ct));
    }

    [HttpGet("support-tickets")]
    public async Task<ActionResult<AdminCommerceSupportListResponse>> ListSupportTickets(
        [FromQuery] string? q = null,
        [FromQuery] CustomerSupportTicketStatus? status = null,
        CancellationToken ct = default)
    {
        return Ok(await _service.ListSupportTicketsAsync(null, q, status, ct));
    }

    [HttpPost("support-tickets/{ticketId:guid}/reply")]
    public async Task<ActionResult<AdminCommerceSupportTicketDto>> ReplySupportTicket(
        Guid ticketId,
        [FromBody] ReplySupportTicketRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _service.ReplySupportTicketAsync(ticketId, request, GetCurrentUserId(), ct));
    }

    [HttpGet("settlements")]
    public async Task<ActionResult<AdminSettlementDashboardDto>> GetSettlements(CancellationToken ct = default)
    {
        return Ok(await _service.GetSettlementDashboardAsync(ct));
    }

    [HttpPost("settlements/batches/generate")]
    public async Task<ActionResult<AdminSettlementDashboardDto>> GenerateBatch(
        [FromBody] GenerateSettlementBatchRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _service.GenerateSettlementBatchAsync(request, GetCurrentUserId(), ct));
    }

    [HttpPost("settlements/batches/{batchId:guid}/mark-paid")]
    public async Task<ActionResult<AdminSettlementDashboardDto>> MarkBatchPaid(
        Guid batchId,
        [FromBody] MarkSettlementBatchPaidRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _service.MarkSettlementBatchPaidAsync(batchId, request, GetCurrentUserId(), ct));
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
