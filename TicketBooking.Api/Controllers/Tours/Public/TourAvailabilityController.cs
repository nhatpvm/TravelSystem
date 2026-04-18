// FILE #263: TicketBooking.Api/Controllers/Tours/TourAvailabilityController.cs
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
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/availability")]
[AllowAnonymous]
public sealed class TourAvailabilityController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TourAvailabilityService _availabilityService;
    private readonly TourLocalTimeService _tourLocalTimeService;

    public TourAvailabilityController(
        AppDbContext db,
        TourAvailabilityService availabilityService,
        TourLocalTimeService tourLocalTimeService)
    {
        _db = db;
        _availabilityService = availabilityService;
        _tourLocalTimeService = tourLocalTimeService;
    }

    [HttpGet]
    public async Task<ActionResult<TourAvailabilityResponse>> GetAvailability(
        Guid tourId,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? pax = null,
        [FromQuery] bool onlyBookable = false,
        [FromQuery] bool includeClosed = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 30 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        if (pax.HasValue && pax.Value <= 0)
            return BadRequest(new { message = "pax must be greater than 0." });

        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
            return BadRequest(new { message = "toDate cannot be earlier than fromDate." });

        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        var currentTime = await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct);
        var today = DateOnly.FromDateTime(currentTime.DateTime);
        var effectiveFrom = fromDate ?? today;
        var requestedPax = pax ?? 1;

        IQueryable<TourSchedule> query = _db.TourSchedules
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.DepartureDate >= effectiveFrom);

        if (!includeClosed)
            query = query.Where(x => x.Status == TourScheduleStatus.Open);

        if (toDate.HasValue)
            query = query.Where(x => x.DepartureDate <= toDate.Value);

        var orderedQuery = query
            .OrderBy(x => x.DepartureDate)
            .ThenBy(x => x.DepartureTime)
            .ThenBy(x => x.Code);

        int total;
        IReadOnlyCollection<TourAvailabilityBuildItem> items;
        string currencyCode;

        if (onlyBookable)
        {
            var schedules = await orderedQuery.ToListAsync(ct);
            var buildResult = await BuildAvailabilityAsync(tour, schedules, requestedPax, currentTime, onlyBookable: true, ct);
            total = buildResult.Total;
            currencyCode = buildResult.CurrencyCode;
            items = buildResult.Items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            total = await query.CountAsync(ct);

            var schedules = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var buildResult = await BuildAvailabilityAsync(tour, schedules, requestedPax, currentTime, onlyBookable: false, ct);
            currencyCode = buildResult.CurrencyCode;
            items = buildResult.Items;
        }

        return Ok(new TourAvailabilityResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            CurrencyCode = currencyCode,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(MapItem).ToList()
        });
    }

    private async Task<TourAvailabilityBuildResult> BuildAvailabilityAsync(
        Tour tour,
        IReadOnlyCollection<TourSchedule> schedules,
        int requestedPax,
        DateTimeOffset currentTime,
        bool onlyBookable,
        CancellationToken ct)
    {
        if (schedules.Count == 0)
        {
            return new TourAvailabilityBuildResult
            {
                TourId = tour.Id,
                TourName = tour.Name,
                CurrencyCode = string.IsNullOrWhiteSpace(tour.CurrencyCode) ? "VND" : tour.CurrencyCode.Trim().ToUpperInvariant(),
                RequestedPax = requestedPax,
                Total = 0
            };
        }

        var scheduleIds = schedules.Select(x => x.Id).ToList();

        var capacities = await _db.TourScheduleCapacities
            .AsNoTracking()
            .Where(x => scheduleIds.Contains(x.TourScheduleId) && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.TourScheduleId, x => (TourScheduleCapacity?)x, ct);

        var adultPrices = await _db.TourSchedulePrices
            .AsNoTracking()
            .Where(x =>
                scheduleIds.Contains(x.TourScheduleId) &&
                x.IsActive &&
                !x.IsDeleted &&
                x.PriceType == TourPriceType.Adult)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Price)
            .ToListAsync(ct);

        var adultPriceBySchedule = adultPrices
            .GroupBy(x => x.TourScheduleId)
            .ToDictionary(
                g => g.Key,
                g => TourPricingResolver.ResolvePriceForQuantity(g, TourPriceType.Adult, requestedPax));

        return _availabilityService.Build(new TourAvailabilityBuildRequest
        {
            Tour = tour,
            RequestedPax = requestedPax,
            OnlyBookable = onlyBookable,
            Now = currentTime,
            Schedules = schedules,
            CapacitiesByScheduleId = capacities,
            AdultPricesByScheduleId = adultPriceBySchedule
        });
    }

    private static TourAvailabilityItemDto MapItem(TourAvailabilityBuildItem item)
    {
        return new TourAvailabilityItemDto
        {
            ScheduleId = item.ScheduleId,
            Code = item.Code,
            Name = item.Name,
            DepartureDate = item.DepartureDate,
            ReturnDate = item.ReturnDate,
            DepartureTime = item.DepartureTime,
            ReturnTime = item.ReturnTime,
            BookingOpenAt = item.BookingOpenAt,
            BookingCutoffAt = item.BookingCutoffAt,
            Status = item.Status,
            IsGuaranteedDeparture = item.IsGuaranteedDeparture,
            IsInstantConfirm = item.IsInstantConfirm,
            MinGuestsToOperate = item.MinGuestsToOperate,
            MaxGuests = item.MaxGuests,
            TotalSlots = item.TotalSlots,
            SoldSlots = item.SoldSlots,
            HeldSlots = item.HeldSlots,
            BlockedSlots = item.BlockedSlots,
            AvailableSlots = item.AvailableSlots,
            MaxGuestsPerBooking = item.MaxGuestsPerBooking,
            AllowWaitlist = item.AllowWaitlist,
            CapacityStatus = item.CapacityStatus,
            AdultPrice = item.AdultPrice,
            OriginalAdultPrice = item.OriginalAdultPrice,
            CurrencyCode = item.CurrencyCode,
            CanBook = item.CanBook,
            BookabilityReason = item.BookabilityReason
        };
    }
}

public sealed class TourAvailabilityResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourAvailabilityItemDto> Items { get; set; } = new();
}

public sealed class TourAvailabilityItemDto
{
    public Guid ScheduleId { get; set; }
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public TourScheduleStatus Status { get; set; }
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }
    public int? TotalSlots { get; set; }
    public int? SoldSlots { get; set; }
    public int? HeldSlots { get; set; }
    public int? BlockedSlots { get; set; }
    public int? AvailableSlots { get; set; }
    public int? MaxGuestsPerBooking { get; set; }
    public bool AllowWaitlist { get; set; }
    public TourCapacityStatus? CapacityStatus { get; set; }
    public decimal? AdultPrice { get; set; }
    public decimal? OriginalAdultPrice { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool CanBook { get; set; }
    public string BookabilityReason { get; set; } = "";
}

