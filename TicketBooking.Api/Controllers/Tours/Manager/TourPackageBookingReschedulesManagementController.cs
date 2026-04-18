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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/package-bookings/{bookingId:guid}/reschedules")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/package-bookings/{bookingId:guid}/reschedules")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageBookingReschedulesManagementController : ControllerBase
{
    private readonly TourPackageRescheduleService _rescheduleService;
    private readonly ITenantContext _tenantContext;

    public TourPackageBookingReschedulesManagementController(
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
        var result = await _rescheduleService.ListAsync(
            tourId,
            bookingId,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
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

        var result = await _rescheduleService.HoldAsync(
            tourId,
            bookingId,
            request,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
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
        var result = await _rescheduleService.GetAsync(
            tourId,
            bookingId,
            id,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
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

        var result = await _rescheduleService.ConfirmAsync(
            tourId,
            bookingId,
            id,
            request,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
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
        var result = await _rescheduleService.ReleaseAsync(
            tourId,
            bookingId,
            id,
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
