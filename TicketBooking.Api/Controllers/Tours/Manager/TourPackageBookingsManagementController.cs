using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/package-bookings")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/package-bookings")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageBookingsManagementController : TourPackageManagementControllerBase
{
    private readonly TourPackageBookingOpsService _opsService;

    public TourPackageBookingsManagementController(
        AppDbContext db,
        ITenantContext tenant,
        TourPackageBookingOpsService opsService)
        : base(db, tenant)
    {
        _opsService = opsService;
    }

    [HttpGet]
    public async Task<ActionResult<TourPackageBookingOpsPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] Guid? packageId = null,
        [FromQuery] Guid? scheduleId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] TourPackageBookingStatus? status = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        [FromQuery] DateTimeOffset? confirmedFrom = null,
        [FromQuery] DateTimeOffset? confirmedTo = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var result = await _opsService.ListAsync(
            tenantId,
            tourId,
            new TourPackageBookingOpsListRequest
            {
                Q = q,
                PackageId = NormalizeGuid(packageId),
                ScheduleId = NormalizeGuid(scheduleId),
                UserId = NormalizeGuid(userId),
                Status = status,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                ConfirmedFrom = confirmedFrom,
                ConfirmedTo = confirmedTo,
                IncludeDeleted = includeDeleted,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(result);
    }

    [HttpGet("{bookingId:guid}")]
    public async Task<ActionResult<TourPackageBookingOpsDetailView>> GetById(
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var result = await _opsService.GetAsync(tenantId, tourId, bookingId, ct);
        return Ok(result);
    }

    [HttpGet("{bookingId:guid}/timeline")]
    public async Task<ActionResult<List<TourPackageBookingOpsTimelineEventView>>> GetTimeline(
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var result = await _opsService.GetTimelineAsync(tenantId, tourId, bookingId, ct);
        return Ok(result);
    }
}
