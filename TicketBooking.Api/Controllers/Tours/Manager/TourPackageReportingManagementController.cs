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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/package-reporting")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/package-reporting")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageReportingManagementController : TourPackageManagementControllerBase
{
    private readonly TourPackageAuditService _auditService;

    public TourPackageReportingManagementController(
        AppDbContext db,
        ITenantContext tenant,
        TourPackageAuditService auditService)
        : base(db, tenant)
    {
        _auditService = auditService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<TourPackageAuditOverviewView>> GetOverview(
        Guid tourId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? packageId = null,
        [FromQuery] Guid? scheduleId = null,
        [FromQuery] Guid? bookingId = null,
        [FromQuery] Guid? userId = null,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var result = await _auditService.GetOverviewAsync(
            tenantId,
            tourId,
            new TourPackageAuditOverviewRequest
            {
                From = from,
                To = to,
                PackageId = NormalizeGuid(packageId),
                ScheduleId = NormalizeGuid(scheduleId),
                BookingId = NormalizeGuid(bookingId),
                UserId = NormalizeGuid(userId)
            },
            ct);

        return Ok(result);
    }

    [HttpGet("source-breakdown")]
    public async Task<ActionResult<List<TourPackageAuditSourceBreakdownItemView>>> GetSourceBreakdown(
        Guid tourId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? packageId = null,
        [FromQuery] Guid? scheduleId = null,
        [FromQuery] Guid? bookingId = null,
        [FromQuery] Guid? userId = null,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var result = await _auditService.GetSourceBreakdownAsync(
            tenantId,
            tourId,
            new TourPackageAuditSourceBreakdownRequest
            {
                From = from,
                To = to,
                PackageId = NormalizeGuid(packageId),
                ScheduleId = NormalizeGuid(scheduleId),
                BookingId = NormalizeGuid(bookingId),
                UserId = NormalizeGuid(userId)
            },
            ct);

        return Ok(result);
    }

    [HttpGet("audit-events")]
    public async Task<ActionResult<TourPackageAuditEventPagedResponse>> ListAuditEvents(
        Guid tourId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? packageId = null,
        [FromQuery] Guid? scheduleId = null,
        [FromQuery] Guid? bookingId = null,
        [FromQuery] Guid? reservationId = null,
        [FromQuery] Guid? cancellationId = null,
        [FromQuery] Guid? refundId = null,
        [FromQuery] Guid? rescheduleId = null,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] TourPackageAuditSeverity? severity = null,
        [FromQuery] TourPackageSourceType? sourceType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var result = await _auditService.ListEventsAsync(
            tenantId,
            tourId,
            new TourPackageAuditEventListRequest
            {
                From = from,
                To = to,
                PackageId = NormalizeGuid(packageId),
                ScheduleId = NormalizeGuid(scheduleId),
                BookingId = NormalizeGuid(bookingId),
                ReservationId = NormalizeGuid(reservationId),
                CancellationId = NormalizeGuid(cancellationId),
                RefundId = NormalizeGuid(refundId),
                RescheduleId = NormalizeGuid(rescheduleId),
                ActorUserId = NormalizeGuid(actorUserId),
                EventType = NullIfWhiteSpace(eventType),
                Severity = severity,
                SourceType = sourceType,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(result);
    }
}
