// FILE #274: TicketBooking.Api/Services/Tours/TourAvailabilityService.cs
using TicketBooking.Application.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourAvailabilityService
{
    private readonly TourBookabilityService _bookabilityService;

    public TourAvailabilityService(TourBookabilityService bookabilityService)
    {
        _bookabilityService = bookabilityService;
    }

    public TourAvailabilityBuildResult Build(TourAvailabilityBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Tour);
        ArgumentNullException.ThrowIfNull(request.Schedules);
        ArgumentNullException.ThrowIfNull(request.CapacitiesByScheduleId);
        ArgumentNullException.ThrowIfNull(request.AdultPricesByScheduleId);

        if (request.RequestedPax <= 0)
            throw new ArgumentException("RequestedPax must be greater than 0.", nameof(request));

        var result = new TourAvailabilityBuildResult
        {
            TourId = request.Tour.Id,
            TourName = request.Tour.Name,
            CurrencyCode = string.IsNullOrWhiteSpace(request.Tour.CurrencyCode)
                ? "VND"
                : request.Tour.CurrencyCode.Trim().ToUpperInvariant(),
            RequestedPax = request.RequestedPax
        };

        foreach (var schedule in request.Schedules
                     .OrderBy(x => x.DepartureDate)
                     .ThenBy(x => x.DepartureTime)
                     .ThenBy(x => x.Code))
        {
            request.CapacitiesByScheduleId.TryGetValue(schedule.Id, out var capacity);
            request.AdultPricesByScheduleId.TryGetValue(schedule.Id, out var adultPrice);

            var bookability = _bookabilityService.EvaluateSchedule(new TourBookabilityRequest
            {
                Schedule = schedule,
                Tour = request.Tour,
                Capacity = capacity,
                RequestedPax = request.RequestedPax,
                Now = request.Now
            });

            if (request.OnlyBookable && !bookability.CanBook)
                continue;

            var currencyCode = adultPrice?.CurrencyCode;
            if (string.IsNullOrWhiteSpace(currencyCode))
                currencyCode = result.CurrencyCode;
            else
                currencyCode = currencyCode.Trim().ToUpperInvariant();

            var item = new TourAvailabilityBuildItem
            {
                ScheduleId = schedule.Id,
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
                TotalSlots = capacity?.TotalSlots,
                SoldSlots = capacity?.SoldSlots,
                HeldSlots = capacity?.HeldSlots,
                BlockedSlots = capacity?.BlockedSlots,
                AvailableSlots = bookability.AvailableSlots,
                MaxGuestsPerBooking = bookability.MaxGuestsPerBooking,
                AllowWaitlist = bookability.AllowWaitlist,
                CapacityStatus = bookability.CapacityStatus,
                AdultPrice = adultPrice?.Price,
                OriginalAdultPrice = adultPrice?.OriginalPrice,
                CurrencyCode = currencyCode,
                CanBook = bookability.CanBook,
                IsWaitlist = bookability.IsWaitlist,
                BookabilityReason = bookability.Reason,
                FailureCode = bookability.FailureCode
            };

            result.Items.Add(item);

            if (item.CanBook)
            {
                if (item.IsWaitlist)
                    result.WaitlistCount++;
                else
                    result.BookableCount++;
            }
            else
            {
                result.UnavailableCount++;
            }

            if (!result.NextBookableScheduleId.HasValue && item.CanBook)
                result.NextBookableScheduleId = item.ScheduleId;
        }

        result.Total = result.Items.Count;
        return result;
    }
}

public sealed class TourAvailabilityBuildRequest
{
    public Tour Tour { get; set; } = null!;
    public int RequestedPax { get; set; } = 1;
    public bool OnlyBookable { get; set; }
    public DateTimeOffset? Now { get; set; }
    public IReadOnlyCollection<TourSchedule> Schedules { get; set; } = Array.Empty<TourSchedule>();
    public IReadOnlyDictionary<Guid, TourScheduleCapacity?> CapacitiesByScheduleId { get; set; }
        = new Dictionary<Guid, TourScheduleCapacity?>();
    public IReadOnlyDictionary<Guid, TourSchedulePrice?> AdultPricesByScheduleId { get; set; }
        = new Dictionary<Guid, TourSchedulePrice?>();
}

public sealed class TourAvailabilityBuildResult
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public int RequestedPax { get; set; }
    public int Total { get; set; }
    public int BookableCount { get; set; }
    public int WaitlistCount { get; set; }
    public int UnavailableCount { get; set; }
    public Guid? NextBookableScheduleId { get; set; }
    public List<TourAvailabilityBuildItem> Items { get; set; } = new();
}

public sealed class TourAvailabilityBuildItem
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
    public bool IsFeatured { get; set; }
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
    public bool IsWaitlist { get; set; }
    public string BookabilityReason { get; set; } = "";
    public TourBookabilityFailureCode? FailureCode { get; set; }
}

