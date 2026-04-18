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
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/package-bookings")]
[Authorize]
public sealed class TourPackageBookingsController : ControllerBase
{
    private readonly TourPackageBookingService _bookingService;
    private readonly ITenantContext _tenantContext;

    public TourPackageBookingsController(
        TourPackageBookingService bookingService,
        ITenantContext tenantContext)
    {
        _bookingService = bookingService;
        _tenantContext = tenantContext;
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<TourPackageBookingConfirmServiceResult>> Confirm(
        Guid tourId,
        [FromBody] TourPackageBookingConfirmRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _bookingService.ConfirmAsync(
            tourId,
            request,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        if (result.Reused)
            return Ok(result);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = result.Booking.Id },
            result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPackageBookingView>> GetById(
        Guid tourId,
        Guid id,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _bookingService.GetAsync(
            tourId,
            id,
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
