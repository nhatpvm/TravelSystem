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
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/package-reservations")]
[Authorize]
public sealed class TourPackageReservationsController : ControllerBase
{
    private readonly TourPackageReservationService _reservationService;
    private readonly ITenantContext _tenantContext;

    public TourPackageReservationsController(
        TourPackageReservationService reservationService,
        ITenantContext tenantContext)
    {
        _reservationService = reservationService;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    public async Task<ActionResult<TourPackageReservationHoldServiceResult>> Create(
        Guid tourId,
        [FromBody] TourPackageReservationCreateRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _reservationService.HoldAsync(
            tourId,
            request,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        if (result.Reused)
            return Ok(result);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = result.Reservation.Id },
            result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPackageReservationView>> GetById(
        Guid tourId,
        Guid id,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _reservationService.GetAsync(
            tourId,
            id,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        return Ok(result);
    }

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<TourPackageReservationView>> Release(
        Guid tourId,
        Guid id,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _reservationService.ReleaseAsync(
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
