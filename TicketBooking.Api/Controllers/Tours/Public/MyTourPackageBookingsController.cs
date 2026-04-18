using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/my/tour-package-bookings")]
[Authorize]
public sealed class MyTourPackageBookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TourPackageBookingService _bookingService;

    public MyTourPackageBookingsController(
        AppDbContext db,
        TourPackageBookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<ActionResult<MyTourPackageBookingPagedResponse>> List(
        [FromQuery] string? q = null,
        [FromQuery] TourPackageBookingStatus? status = null,
        [FromQuery] bool upcomingOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "UserId claim is required." });

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<TourPackageBooking> query = _db.TourPackageBookings
            .AsNoTracking()
            .Include(x => x.Tour)
            .Include(x => x.TourSchedule)
            .Include(x => x.TourPackage)
            .Where(x =>
                x.UserId == userId.Value &&
                !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (upcomingOnly)
            query = query.Where(x =>
                x.TourSchedule != null &&
                x.TourSchedule.DepartureDate >= DateOnly.FromDateTime(DateTime.Today));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                (x.Tour != null && x.Tour.Name.Contains(qq)) ||
                (x.TourPackage != null && x.TourPackage.Name.Contains(qq)) ||
                (x.TourSchedule != null && (
                    x.TourSchedule.Code.Contains(qq) ||
                    (x.TourSchedule.Name != null && x.TourSchedule.Name.Contains(qq)))));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.ConfirmedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MyTourPackageBookingListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                TourName = x.Tour != null ? x.Tour.Name : string.Empty,
                TourSlug = x.Tour != null ? x.Tour.Slug : null,
                CoverImageUrl = x.Tour != null ? x.Tour.CoverImageUrl : null,
                TourProvince = x.Tour != null ? x.Tour.Province : null,
                TourCity = x.Tour != null ? x.Tour.City : null,
                PackageId = x.TourPackageId,
                PackageCode = x.TourPackage != null ? x.TourPackage.Code : string.Empty,
                PackageName = x.TourPackage != null ? x.TourPackage.Name : string.Empty,
                ScheduleId = x.TourScheduleId,
                ScheduleCode = x.TourSchedule != null ? x.TourSchedule.Code : string.Empty,
                ScheduleName = x.TourSchedule != null ? x.TourSchedule.Name : null,
                DepartureDate = x.TourSchedule != null ? x.TourSchedule.DepartureDate : null,
                ReturnDate = x.TourSchedule != null ? x.TourSchedule.ReturnDate : null,
                DepartureTime = x.TourSchedule != null ? x.TourSchedule.DepartureTime : null,
                ReturnTime = x.TourSchedule != null ? x.TourSchedule.ReturnTime : null,
                Status = x.Status,
                CurrencyCode = x.CurrencyCode,
                RequestedPax = x.RequestedPax,
                ConfirmedCapacitySlots = x.ConfirmedCapacitySlots,
                PackageSubtotalAmount = x.PackageSubtotalAmount,
                ConfirmedAt = x.ConfirmedAt,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new MyTourPackageBookingPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{bookingId:guid}")]
    public async Task<ActionResult<MyTourPackageBookingDetailDto>> GetById(
        Guid bookingId,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "UserId claim is required." });

        var booking = await _db.TourPackageBookings
            .AsNoTracking()
            .Include(x => x.Tour)
            .Include(x => x.TourSchedule)
            .Include(x => x.TourPackage)
            .FirstOrDefaultAsync(x =>
                x.Id == bookingId &&
                x.UserId == userId.Value &&
                !x.IsDeleted, ct);

        if (booking is null)
            return NotFound(new { message = "Tour package booking not found." });

        var detail = await _bookingService.GetAsync(
            booking.TourId,
            bookingId,
            userId.Value,
            false,
            ct);

        return Ok(new MyTourPackageBookingDetailDto
        {
            Id = detail.Id,
            TourId = detail.TourId,
            TourName = booking.Tour?.Name ?? string.Empty,
            TourSlug = booking.Tour?.Slug,
            CoverImageUrl = booking.Tour?.CoverImageUrl,
            TourProvince = booking.Tour?.Province,
            TourCity = booking.Tour?.City,
            PackageId = detail.PackageId,
            PackageCode = detail.PackageCode,
            PackageName = detail.PackageName,
            ScheduleId = detail.ScheduleId,
            ScheduleCode = detail.ScheduleCode,
            ScheduleName = detail.ScheduleName,
            DepartureDate = booking.TourSchedule?.DepartureDate,
            ReturnDate = booking.TourSchedule?.ReturnDate,
            DepartureTime = booking.TourSchedule?.DepartureTime,
            ReturnTime = booking.TourSchedule?.ReturnTime,
            ReservationId = detail.ReservationId,
            Code = detail.Code,
            Status = detail.Status,
            HoldStrategy = detail.HoldStrategy,
            CurrencyCode = detail.CurrencyCode,
            RequestedPax = detail.RequestedPax,
            ConfirmedCapacitySlots = detail.ConfirmedCapacitySlots,
            PackageSubtotalAmount = detail.PackageSubtotalAmount,
            ConfirmedAt = detail.ConfirmedAt,
            Notes = detail.Notes,
            FailureReason = detail.FailureReason,
            SnapshotJson = detail.SnapshotJson,
            IsDeleted = detail.IsDeleted,
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt,
            Items = detail.Items
        });
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}

public sealed class MyTourPackageBookingPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<MyTourPackageBookingListItemDto> Items { get; set; } = new();
}

public sealed class MyTourPackageBookingListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string? TourSlug { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? TourProvince { get; set; }
    public string? TourCity { get; set; }
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public Guid ScheduleId { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string? ScheduleName { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public TourPackageBookingStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public int RequestedPax { get; set; }
    public int ConfirmedCapacitySlots { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class MyTourPackageBookingDetailDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string? TourSlug { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? TourProvince { get; set; }
    public string? TourCity { get; set; }
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public Guid ScheduleId { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string? ScheduleName { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public Guid ReservationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public TourPackageBookingStatus Status { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public int RequestedPax { get; set; }
    public int ConfirmedCapacitySlots { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? Notes { get; set; }
    public string? FailureReason { get; set; }
    public string? SnapshotJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<TourPackageBookingItemView> Items { get; set; } = new();
}
