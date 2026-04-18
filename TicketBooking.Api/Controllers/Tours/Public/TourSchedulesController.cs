// FILE #268: TicketBooking.Api/Controllers/Tours/TourSchedulesController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Services.Tours;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/schedules")]
[AllowAnonymous]
public sealed class TourSchedulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TourBookabilityService _bookabilityService;
    private readonly TourLocalTimeService _tourLocalTimeService;

    public TourSchedulesController(
        AppDbContext db,
        TourBookabilityService bookabilityService,
        TourLocalTimeService tourLocalTimeService)
    {
        _db = db;
        _bookabilityService = bookabilityService;
        _tourLocalTimeService = tourLocalTimeService;
    }

    [HttpGet]
    public async Task<ActionResult<TourPublicSchedulePagedResponse>> List(
        Guid tourId,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] bool upcomingOnly = true,
        [FromQuery] bool onlyBookable = false,
        [FromQuery] bool includeClosed = false,
        [FromQuery] int pax = 1,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        if (pax <= 0)
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
        var effectiveFrom = fromDate ?? (upcomingOnly ? today : (DateOnly?)null);

        IQueryable<TourSchedule> query = _db.TourSchedules
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted);

        if (!includeClosed)
            query = query.Where(x => x.Status == TourScheduleStatus.Open);

        if (effectiveFrom.HasValue)
            query = query.Where(x => x.DepartureDate >= effectiveFrom.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.DepartureDate <= toDate.Value);

        var orderedQuery = query
            .OrderBy(x => x.DepartureDate)
            .ThenBy(x => x.DepartureTime)
            .ThenBy(x => x.Code);

        int total;
        List<TourPublicScheduleListItemDto> items;

        if (onlyBookable)
        {
            var schedules = await orderedQuery.ToListAsync(ct);
            var filteredItems = await BuildListItemsAsync(tour, schedules, pax, currentTime, onlyBookable: true, ct);
            total = filteredItems.Count;
            items = filteredItems
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

            items = await BuildListItemsAsync(tour, schedules, pax, currentTime, onlyBookable: false, ct);
        }

        return Ok(new TourPublicSchedulePagedResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{scheduleId:guid}")]
    public async Task<ActionResult<TourPublicScheduleDetailDto>> GetById(
        Guid tourId,
        Guid scheduleId,
        [FromQuery] int pax = 1,
        CancellationToken ct = default)
    {
        if (pax <= 0)
            return BadRequest(new { message = "pax must be greater than 0." });

        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        var schedule = await _db.TourSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == scheduleId &&
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (schedule is null)
            return NotFound(new { message = "Tour schedule not found." });

        var capacity = await _db.TourScheduleCapacities
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TourScheduleId == scheduleId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        var prices = await _db.TourSchedulePrices
            .AsNoTracking()
            .Where(x =>
                x.TourScheduleId == scheduleId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.PriceType)
            .ThenBy(x => x.Price)
            .ToListAsync(ct);

        var bookability = _bookabilityService.EvaluateSchedule(new TourBookabilityRequest
        {
            Schedule = schedule,
            Tour = tour,
            Capacity = capacity,
            RequestedPax = pax,
            Now = await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct)
        });

        return Ok(new TourPublicScheduleDetailDto
        {
            Id = schedule.Id,
            TourId = schedule.TourId,
            TourName = tour.Name,
            Code = schedule.Code,
            Name = schedule.Name,
            DepartureDate = schedule.DepartureDate,
            ReturnDate = schedule.ReturnDate,
            DepartureTime = schedule.DepartureTime,
            ReturnTime = schedule.ReturnTime,
            BookingOpenAt = schedule.BookingOpenAt,
            BookingCutoffAt = schedule.BookingCutoffAt,
            MeetingPointSummary = schedule.MeetingPointSummary,
            PickupSummary = schedule.PickupSummary,
            DropoffSummary = schedule.DropoffSummary,
            Notes = schedule.Notes,
            CancellationNotes = schedule.CancellationNotes,
            Status = schedule.Status,
            IsGuaranteedDeparture = schedule.IsGuaranteedDeparture,
            IsInstantConfirm = schedule.IsInstantConfirm,
            IsFeatured = schedule.IsFeatured,
            MinGuestsToOperate = schedule.MinGuestsToOperate,
            MaxGuests = schedule.MaxGuests,
            Capacity = capacity is null
                ? null
                : new TourPublicScheduleCapacityDto
                {
                    TotalSlots = capacity.TotalSlots,
                    SoldSlots = capacity.SoldSlots,
                    HeldSlots = capacity.HeldSlots,
                    BlockedSlots = capacity.BlockedSlots,
                    AvailableSlots = capacity.AvailableSlots,
                    MinGuestsToOperate = capacity.MinGuestsToOperate,
                    MaxGuestsPerBooking = capacity.MaxGuestsPerBooking,
                    WarningThreshold = capacity.WarningThreshold,
                    Status = capacity.Status,
                    AllowWaitlist = capacity.AllowWaitlist,
                    AutoCloseWhenFull = capacity.AutoCloseWhenFull
                },
            Prices = prices.Select(x => new TourPublicSchedulePriceDto
            {
                Id = x.Id,
                PriceType = x.PriceType,
                CurrencyCode = x.CurrencyCode,
                Price = x.Price,
                OriginalPrice = x.OriginalPrice,
                Taxes = x.Taxes,
                Fees = x.Fees,
                MinAge = x.MinAge,
                MaxAge = x.MaxAge,
                MinQuantity = x.MinQuantity,
                MaxQuantity = x.MaxQuantity,
                IsDefault = x.IsDefault,
                IsIncludedTax = x.IsIncludedTax,
                IsIncludedFee = x.IsIncludedFee,
                Label = x.Label,
                Notes = x.Notes
            }).ToList(),
            CanBook = bookability.CanBook,
            BookabilityReason = bookability.Reason
        });
    }

    private async Task<List<TourPublicScheduleListItemDto>> BuildListItemsAsync(
        Tour tour,
        IReadOnlyCollection<TourSchedule> schedules,
        int pax,
        DateTimeOffset now,
        bool onlyBookable,
        CancellationToken ct)
    {
        if (schedules.Count == 0)
            return new List<TourPublicScheduleListItemDto>();

        var scheduleIds = schedules.Select(x => x.Id).ToList();

        var capacities = await _db.TourScheduleCapacities
            .AsNoTracking()
            .Where(x => scheduleIds.Contains(x.TourScheduleId) && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.TourScheduleId, x => (TourScheduleCapacity?)x, ct);

        var prices = await _db.TourSchedulePrices
            .AsNoTracking()
            .Where(x =>
                scheduleIds.Contains(x.TourScheduleId) &&
                x.IsActive &&
                !x.IsDeleted)
            .ToListAsync(ct);

        var adultPriceBySchedule = prices
            .Where(x => x.PriceType == TourPriceType.Adult)
            .GroupBy(x => x.TourScheduleId)
            .ToDictionary(
                g => g.Key,
                g => TourPricingResolver.ResolvePriceForQuantity(g, TourPriceType.Adult, pax));

        var items = new List<TourPublicScheduleListItemDto>(schedules.Count);

        foreach (var schedule in schedules)
        {
            capacities.TryGetValue(schedule.Id, out var capacity);
            adultPriceBySchedule.TryGetValue(schedule.Id, out var adultPrice);

            var bookability = _bookabilityService.EvaluateSchedule(new TourBookabilityRequest
            {
                Schedule = schedule,
                Tour = tour,
                Capacity = capacity,
                RequestedPax = pax,
                Now = now
            });

            if (onlyBookable && !bookability.CanBook)
                continue;

            items.Add(new TourPublicScheduleListItemDto
            {
                Id = schedule.Id,
                Code = schedule.Code,
                Name = schedule.Name,
                DepartureDate = schedule.DepartureDate,
                ReturnDate = schedule.ReturnDate,
                DepartureTime = schedule.DepartureTime,
                ReturnTime = schedule.ReturnTime,
                BookingOpenAt = schedule.BookingOpenAt,
                BookingCutoffAt = schedule.BookingCutoffAt,
                Status = schedule.Status,
                IsGuaranteedDeparture = schedule.IsGuaranteedDeparture,
                IsInstantConfirm = schedule.IsInstantConfirm,
                IsFeatured = schedule.IsFeatured,
                MinGuestsToOperate = schedule.MinGuestsToOperate,
                MaxGuests = schedule.MaxGuests,
                AvailableSlots = capacity?.AvailableSlots,
                AllowWaitlist = capacity?.AllowWaitlist,
                CapacityStatus = capacity?.Status,
                AdultPrice = adultPrice?.Price,
                OriginalAdultPrice = adultPrice?.OriginalPrice,
                CurrencyCode = adultPrice?.CurrencyCode ?? tour.CurrencyCode,
                CanBook = bookability.CanBook,
                BookabilityReason = bookability.Reason
            });
        }

        return items;
    }

}

public sealed class TourPublicSchedulePagedResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPublicScheduleListItemDto> Items { get; set; } = new();
}

public sealed class TourPublicScheduleListItemDto
{
    public Guid Id { get; set; }
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
    public bool IsFeatured { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }
    public int? AvailableSlots { get; set; }
    public bool? AllowWaitlist { get; set; }
    public TourCapacityStatus? CapacityStatus { get; set; }
    public decimal? AdultPrice { get; set; }
    public decimal? OriginalAdultPrice { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool CanBook { get; set; }
    public string BookabilityReason { get; set; } = "";
}

public sealed class TourPublicScheduleDetailDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public string? MeetingPointSummary { get; set; }
    public string? PickupSummary { get; set; }
    public string? DropoffSummary { get; set; }
    public string? Notes { get; set; }
    public string? CancellationNotes { get; set; }
    public TourScheduleStatus Status { get; set; }
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public bool IsFeatured { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }
    public TourPublicScheduleCapacityDto? Capacity { get; set; }
    public List<TourPublicSchedulePriceDto> Prices { get; set; } = new();
    public bool CanBook { get; set; }
    public string BookabilityReason { get; set; } = "";
}

public sealed class TourPublicScheduleCapacityDto
{
    public int TotalSlots { get; set; }
    public int SoldSlots { get; set; }
    public int HeldSlots { get; set; }
    public int BlockedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuestsPerBooking { get; set; }
    public int? WarningThreshold { get; set; }
    public TourCapacityStatus Status { get; set; }
    public bool AllowWaitlist { get; set; }
    public bool AutoCloseWhenFull { get; set; }
}

public sealed class TourPublicSchedulePriceDto
{
    public Guid Id { get; set; }
    public TourPriceType PriceType { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefault { get; set; }
    public bool IsIncludedTax { get; set; }
    public bool IsIncludedFee { get; set; }
    public string? Label { get; set; }
    public string? Notes { get; set; }
}

