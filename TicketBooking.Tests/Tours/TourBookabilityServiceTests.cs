using TicketBooking.Application.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourBookabilityServiceTests
{
    private readonly TourBookabilityService _service = new();

    [Fact]
    public void EvaluateSchedule_UsesScheduleMaxGuestsBeforeTourLimit()
    {
        var result = _service.EvaluateSchedule(new TourBookabilityRequest
        {
            Schedule = CreateOpenSchedule(maxGuests: 4),
            Tour = new Tour { MaxGuests = 10 },
            RequestedPax = 5,
            Now = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)
        });

        Assert.False(result.CanBook);
        Assert.Equal(TourBookabilityFailureCode.ExceedsMaxGuestsPerBooking, result.FailureCode);
        Assert.Equal(4, result.MaxGuestsPerBooking);
    }

    [Fact]
    public void EvaluateSchedule_ReturnsWaitlistWhenCapacityIsFullButWaitlistAllowed()
    {
        var result = _service.EvaluateSchedule(new TourBookabilityRequest
        {
            Schedule = CreateOpenSchedule(),
            RequestedPax = 2,
            Now = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero),
            Capacity = new TourScheduleCapacity
            {
                Status = TourCapacityStatus.Full,
                TotalSlots = 10,
                SoldSlots = 10,
                AllowWaitlist = true,
                IsActive = true
            }
        });

        Assert.True(result.CanBook);
        Assert.True(result.IsWaitlist);
        Assert.Null(result.FailureCode);
        Assert.Equal(0, result.AvailableSlots);
    }

    [Theory]
    [InlineData(TourCapacityStatus.Draft)]
    [InlineData(TourCapacityStatus.Closed)]
    [InlineData(TourCapacityStatus.Cancelled)]
    public void EvaluateSchedule_BlocksClosedCapacityStates(TourCapacityStatus capacityStatus)
    {
        var result = _service.EvaluateSchedule(new TourBookabilityRequest
        {
            Schedule = CreateOpenSchedule(),
            RequestedPax = 1,
            Now = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero),
            Capacity = new TourScheduleCapacity
            {
                Status = capacityStatus,
                TotalSlots = 20,
                SoldSlots = 2,
                IsActive = true
            }
        });

        Assert.False(result.CanBook);
        Assert.Equal(TourBookabilityFailureCode.CapacityClosed, result.FailureCode);
    }

    [Fact]
    public void EvaluateSchedule_FallsBackToTourMaxGuestsWhenScheduleAndCapacityDoNotDefineIt()
    {
        var result = _service.EvaluateSchedule(new TourBookabilityRequest
        {
            Schedule = CreateOpenSchedule(),
            Tour = new Tour { MaxGuests = 6 },
            RequestedPax = 7,
            Now = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)
        });

        Assert.False(result.CanBook);
        Assert.Equal(TourBookabilityFailureCode.ExceedsMaxGuestsPerBooking, result.FailureCode);
        Assert.Equal(6, result.MaxGuestsPerBooking);
    }

    private static TourSchedule CreateOpenSchedule(int? maxGuests = null)
    {
        return new TourSchedule
        {
            Id = Guid.NewGuid(),
            TourId = Guid.NewGuid(),
            Code = "SCH-001",
            DepartureDate = new DateOnly(2026, 4, 10),
            ReturnDate = new DateOnly(2026, 4, 10),
            DepartureTime = new TimeOnly(8, 0),
            Status = TourScheduleStatus.Open,
            IsActive = true,
            MaxGuests = maxGuests,
            BookingOpenAt = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            BookingCutoffAt = new DateTimeOffset(2026, 4, 9, 23, 0, 0, TimeSpan.Zero)
        };
    }
}
