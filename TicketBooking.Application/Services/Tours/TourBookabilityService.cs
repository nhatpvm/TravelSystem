// FILE #277: TicketBooking.Application/Services/Tours/TourBookabilityService.cs
using TicketBooking.Domain.Tours;

namespace TicketBooking.Application.Services.Tours;

public sealed class TourBookabilityService
{
    public TourBookabilityResult EvaluateSchedule(TourBookabilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Schedule);

        var now = request.Now ?? DateTimeOffset.UtcNow;
        var requestedPax = request.RequestedPax ?? 1;
        var effectiveMaxGuests = request.Capacity?.MaxGuestsPerBooking
            ?? request.Schedule.MaxGuests
            ?? request.Tour?.MaxGuests;

        if (requestedPax <= 0)
            throw new ArgumentException("RequestedPax must be greater than 0.", nameof(request));

        var result = new TourBookabilityResult
        {
            RequestedPax = requestedPax,
            AvailableSlots = request.Capacity?.AvailableSlots,
            AllowWaitlist = request.Capacity?.AllowWaitlist ?? false,
            MaxGuestsPerBooking = effectiveMaxGuests,
            BookingOpenAt = request.Schedule.BookingOpenAt,
            BookingCutoffAt = request.Schedule.BookingCutoffAt,
            ScheduleStatus = request.Schedule.Status,
            CapacityStatus = request.Capacity?.Status
        };

        if (!request.Schedule.IsActive || request.Schedule.IsDeleted)
            return Fail(result, TourBookabilityFailureCode.ScheduleInactive, "Schedule is inactive.");

        if (request.Schedule.Status != TourScheduleStatus.Open)
            return Fail(result, TourBookabilityFailureCode.ScheduleClosed, $"Schedule status is {request.Schedule.Status}.");

        if (request.Schedule.BookingOpenAt.HasValue && now < request.Schedule.BookingOpenAt.Value)
            return Fail(result, TourBookabilityFailureCode.BookingNotOpened, "Booking has not opened yet.");

        if (request.Schedule.BookingCutoffAt.HasValue && now > request.Schedule.BookingCutoffAt.Value)
            return Fail(result, TourBookabilityFailureCode.BookingClosedByCutoff, "Booking cutoff time has passed.");

        if (effectiveMaxGuests.HasValue && requestedPax > effectiveMaxGuests.Value)
        {
            return Fail(
                result,
                TourBookabilityFailureCode.ExceedsMaxGuestsPerBooking,
                $"Requested pax exceeds allowed max guests ({effectiveMaxGuests.Value}).");
        }

        if (request.Capacity is null)
            return Success(result, "Available.");

        if (!request.Capacity.IsActive || request.Capacity.IsDeleted)
            return Success(result, "Available.");

        if (request.Capacity.Status == TourCapacityStatus.Draft ||
            request.Capacity.Status == TourCapacityStatus.Closed ||
            request.Capacity.Status == TourCapacityStatus.Cancelled)
            return Fail(result, TourBookabilityFailureCode.CapacityClosed, $"Capacity status is {request.Capacity.Status}.");

        if (request.Capacity.Status == TourCapacityStatus.Full)
        {
            result.AvailableSlots = 0;

            if (request.Capacity.AllowWaitlist)
            {
                result.IsWaitlist = true;
                result.CanBook = true;
                result.FailureCode = null;
                result.Reason = "Capacity is full, but waitlist is allowed.";
                return result;
            }

            return Fail(result, TourBookabilityFailureCode.NotEnoughSlots, "Capacity is full.");
        }

        if (request.Capacity.AvailableSlots >= requestedPax)
            return Success(result, "Available.");

        if (request.Capacity.AllowWaitlist)
        {
            result.IsWaitlist = true;
            result.CanBook = true;
            result.FailureCode = null;
            result.Reason = "No immediate slots, but waitlist is allowed.";
            return result;
        }

        return Fail(
            result,
            TourBookabilityFailureCode.NotEnoughSlots,
            $"Not enough available slots. Requested {requestedPax}, available {request.Capacity.AvailableSlots}.");
    }

    public TourBookabilitySummary SummarizeSchedules(
        IReadOnlyCollection<TourSchedule> schedules,
        IReadOnlyDictionary<Guid, TourScheduleCapacity?> capacities,
        int requestedPax,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(capacities);

        if (requestedPax <= 0)
            throw new ArgumentException("requestedPax must be greater than 0.", nameof(requestedPax));

        var summary = new TourBookabilitySummary();

        foreach (var schedule in schedules)
        {
            capacities.TryGetValue(schedule.Id, out var capacity);

            var evaluation = EvaluateSchedule(new TourBookabilityRequest
            {
                Schedule = schedule,
                Capacity = capacity,
                RequestedPax = requestedPax,
                Now = now
            });

            summary.TotalSchedules++;

            if (evaluation.CanBook && !evaluation.IsWaitlist)
                summary.BookableCount++;
            else if (evaluation.CanBook && evaluation.IsWaitlist)
                summary.WaitlistCount++;
            else
                summary.UnavailableCount++;

            if (!summary.NextBookableScheduleId.HasValue && evaluation.CanBook)
                summary.NextBookableScheduleId = schedule.Id;

            summary.Items.Add(new TourBookabilityScheduleItem
            {
                ScheduleId = schedule.Id,
                Code = schedule.Code,
                DepartureDate = schedule.DepartureDate,
                DepartureTime = schedule.DepartureTime,
                CanBook = evaluation.CanBook,
                IsWaitlist = evaluation.IsWaitlist,
                Reason = evaluation.Reason,
                FailureCode = evaluation.FailureCode,
                AvailableSlots = evaluation.AvailableSlots
            });
        }

        return summary;
    }

    private static TourBookabilityResult Success(TourBookabilityResult result, string reason)
    {
        result.CanBook = true;
        result.IsWaitlist = false;
        result.FailureCode = null;
        result.Reason = reason;
        return result;
    }

    private static TourBookabilityResult Fail(
        TourBookabilityResult result,
        TourBookabilityFailureCode code,
        string reason)
    {
        result.CanBook = false;
        result.IsWaitlist = false;
        result.FailureCode = code;
        result.Reason = reason;
        return result;
    }
}

public sealed class TourBookabilityRequest
{
    public TourSchedule Schedule { get; set; } = null!;
    public Tour? Tour { get; set; }
    public TourScheduleCapacity? Capacity { get; set; }
    public int? RequestedPax { get; set; }
    public DateTimeOffset? Now { get; set; }
}

public sealed class TourBookabilityResult
{
    public bool CanBook { get; set; }
    public bool IsWaitlist { get; set; }
    public int RequestedPax { get; set; }
    public int? AvailableSlots { get; set; }
    public bool AllowWaitlist { get; set; }
    public int? MaxGuestsPerBooking { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public TourScheduleStatus ScheduleStatus { get; set; }
    public TourCapacityStatus? CapacityStatus { get; set; }
    public TourBookabilityFailureCode? FailureCode { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class TourBookabilitySummary
{
    public int TotalSchedules { get; set; }
    public int BookableCount { get; set; }
    public int WaitlistCount { get; set; }
    public int UnavailableCount { get; set; }
    public Guid? NextBookableScheduleId { get; set; }
    public List<TourBookabilityScheduleItem> Items { get; set; } = new();
}

public sealed class TourBookabilityScheduleItem
{
    public Guid ScheduleId { get; set; }
    public string Code { get; set; } = "";
    public DateOnly DepartureDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public bool CanBook { get; set; }
    public bool IsWaitlist { get; set; }
    public int? AvailableSlots { get; set; }
    public TourBookabilityFailureCode? FailureCode { get; set; }
    public string Reason { get; set; } = "";
}

public enum TourBookabilityFailureCode
{
    ScheduleInactive = 1,
    ScheduleClosed = 2,
    BookingNotOpened = 3,
    BookingClosedByCutoff = 4,
    CapacityClosed = 5,
    ExceedsMaxGuestsPerBooking = 6,
    NotEnoughSlots = 7
}

