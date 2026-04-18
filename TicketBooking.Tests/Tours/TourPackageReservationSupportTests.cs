using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageReservationSupportTests
{
    [Fact]
    public void CalculateReservationStatus_ReturnsHeld_WhenAllOutcomesSucceeded()
    {
        var results = new[]
        {
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Held
            },
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Validated
            }
        };

        var status = TourPackageReservationSupport.CalculateReservationStatus(results, TourPackageHoldStrategy.AllOrNothing);

        Assert.Equal(TourPackageReservationStatus.Held, status);
    }

    [Fact]
    public void CalculateReservationStatus_ReturnsPartiallyHeld_ForBestEffortPartialFailure()
    {
        var results = new[]
        {
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Held
            },
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed
            }
        };

        var status = TourPackageReservationSupport.CalculateReservationStatus(results, TourPackageHoldStrategy.BestEffort);

        Assert.Equal(TourPackageReservationStatus.PartiallyHeld, status);
    }

    [Fact]
    public void CalculateReservationStatus_ReturnsFailed_ForAllOrNothingPartialFailure()
    {
        var results = new[]
        {
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Held
            },
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed
            }
        };

        var status = TourPackageReservationSupport.CalculateReservationStatus(results, TourPackageHoldStrategy.AllOrNothing);

        Assert.Equal(TourPackageReservationStatus.Failed, status);
    }

    [Fact]
    public void ResolveReservationExpiry_UsesEarliestSuccessfulExpiry()
    {
        var now = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var results = new[]
        {
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Held,
                HoldExpiresAt = now.AddMinutes(12)
            },
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Validated,
                HoldExpiresAt = now.AddMinutes(8)
            },
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                HoldExpiresAt = now.AddMinutes(1)
            }
        };

        var expiry = TourPackageReservationSupport.ResolveReservationExpiry(now, results, 5);

        Assert.Equal(now.AddMinutes(8), expiry);
    }

    [Fact]
    public void ResolveReservationExpiry_FallsBackWhenNoSuccessfulExpiryExists()
    {
        var now = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var results = new[]
        {
            new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed
            }
        };

        var expiry = TourPackageReservationSupport.ResolveReservationExpiry(now, results, 7);

        Assert.Equal(now.AddMinutes(7), expiry);
    }
}
