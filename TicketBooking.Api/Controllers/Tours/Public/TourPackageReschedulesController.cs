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
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/package-bookings/{bookingId:guid}/reschedules")]
[Authorize]
public sealed class TourPackageReschedulesController : ControllerBase
{
    private readonly TourPackageRescheduleService _rescheduleService;
    private readonly ITenantContext _tenantContext;

    public TourPackageReschedulesController(
        TourPackageRescheduleService rescheduleService,
        ITenantContext tenantContext)
    {
        _rescheduleService = rescheduleService;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<TourPackageRescheduleView>>> List(
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _rescheduleService.ListAsync(
            tourId,
            bookingId,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TourPackageRescheduleExecutionResult>> Hold(
        Guid tourId,
        Guid bookingId,
        [FromBody] TourPackageRescheduleHoldRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _rescheduleService.HoldAsync(
            tourId,
            bookingId,
            request,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        if (result.Reused)
            return Ok(result);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, bookingId, id = result.Reschedule.Id },
            result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPackageRescheduleView>> GetById(
        Guid tourId,
        Guid bookingId,
        Guid id,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _rescheduleService.GetAsync(
            tourId,
            bookingId,
            id,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<TourPackageRescheduleExecutionResult>> Confirm(
        Guid tourId,
        Guid bookingId,
        Guid id,
        [FromBody] TourPackageRescheduleConfirmRequest request,
        CancellationToken ct = default)
    {
        request ??= new TourPackageRescheduleConfirmRequest();

        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _rescheduleService.ConfirmAsync(
            tourId,
            bookingId,
            id,
            request,
            userId ?? _tenantContext.UserId,
            User.IsInRole(RoleNames.Admin),
            ct);

        return Ok(result);
    }

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<TourPackageRescheduleView>> Release(
        Guid tourId,
        Guid bookingId,
        Guid id,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue && !User.IsInRole(RoleNames.Admin))
            return Unauthorized(new { message = "UserId claim is required." });

        var result = await _rescheduleService.ReleaseAsync(
            tourId,
            bookingId,
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
