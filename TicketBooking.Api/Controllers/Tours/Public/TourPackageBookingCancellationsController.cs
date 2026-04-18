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
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/package-bookings/{bookingId:guid}")]
[Authorize]
public sealed class TourPackageBookingCancellationsController : ControllerBase
{
    private readonly TourPackageCancellationService _cancellationService;
    private readonly ITenantContext _tenantContext;

    public TourPackageBookingCancellationsController(
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

        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _cancellationService.CancelItemsAsync(
            tourId,
            bookingId,
            request,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
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
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _cancellationService.GetCancellationAsync(
            tourId,
            bookingId,
            id,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        return Ok(result);
    }

    [HttpGet("refunds")]
    public async Task<ActionResult<TourPackageRefundListView>> GetRefunds(
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _cancellationService.ListRefundsAsync(
            tourId,
            bookingId,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
