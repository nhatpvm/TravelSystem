using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/package-bookings/{bookingId:guid}")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/package-bookings/{bookingId:guid}")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageBookingAfterSalesManagementController : ControllerBase
{
    private readonly TourPackageCancellationService _cancellationService;
    private readonly ITenantContext _tenantContext;

    public TourPackageBookingAfterSalesManagementController(
        TourPackageCancellationService cancellationService,
        ITenantContext tenantContext)
    {
        _cancellationService = cancellationService;
        _tenantContext = tenantContext;
    }

    [HttpPost("cancellations/items")]
    public async Task<ActionResult<TourPackageCancellationExecutionResult>> CancelItems(
        Guid tourId,
        Guid bookingId,
        [FromBody] TourPackageCancellationCreateRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var result = await _cancellationService.CancelItemsAsync(
            tourId,
            bookingId,
            request,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        if (result.Reused)
            return Ok(result);

        return CreatedAtAction(
            nameof(GetCancellationById),
            new { version = "1.0", tourId, bookingId, id = result.Cancellation.Id },
            result);
    }

    [HttpPost("cancellations/bulk")]
    public async Task<ActionResult<TourPackageCancellationExecutionResult>> CancelRemaining(
        Guid tourId,
        Guid bookingId,
        [FromBody] TourPackageBulkCancellationRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var result = await _cancellationService.CancelRemainingAsync(
            tourId,
            bookingId,
            request,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        if (result.Reused)
            return Ok(result);

        return CreatedAtAction(
            nameof(GetCancellationById),
            new { version = "1.0", tourId, bookingId, id = result.Cancellation.Id },
            result);
    }

    [HttpGet("cancellations/{id:guid}")]
    public async Task<ActionResult<TourPackageCancellationView>> GetCancellationById(
        Guid tourId,
        Guid bookingId,
        Guid id,
        CancellationToken ct = default)
    {
        var result = await _cancellationService.GetCancellationAsync(
            tourId,
            bookingId,
            id,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        return Ok(result);
    }

    [HttpGet("refunds")]
    public async Task<ActionResult<TourPackageRefundListView>> GetRefunds(
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var result = await _cancellationService.ListRefundsAsync(
            tourId,
            bookingId,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        return Ok(result);
    }

    [HttpPost("refunds/{refundId:guid}/ready")]
    public async Task<ActionResult<TourPackageRefundView>> MarkRefundReady(
        Guid tourId,
        Guid bookingId,
        Guid refundId,
        [FromBody] TourPackageRefundStateChangeRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var result = await _cancellationService.MarkRefundReadyAsync(
            tourId,
            bookingId,
            refundId,
            request,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        return Ok(result);
    }

    [HttpPost("refunds/{refundId:guid}/reject")]
    public async Task<ActionResult<TourPackageRefundView>> RejectRefund(
        Guid tourId,
        Guid bookingId,
        Guid refundId,
        [FromBody] TourPackageRefundStateChangeRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var result = await _cancellationService.RejectRefundAsync(
            tourId,
            bookingId,
            refundId,
            request,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
