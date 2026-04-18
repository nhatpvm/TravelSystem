using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageBookingSupportTests
{
    [Fact]
    public void CalculateBookingStatus_ReturnsConfirmed_WhenAllOutcomesSucceeded()
    {
        var results = new[]
        {
            new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Confirmed
            },
            new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Confirmed
            }
        };

        var status = TourPackageBookingSupport.CalculateBookingStatus(results, TourPackageHoldStrategy.AllOrNothing);

        Assert.Equal(TourPackageBookingStatus.Confirmed, status);
    }

    [Fact]
    public void CalculateBookingStatus_ReturnsPartiallyConfirmed_ForBestEffortPartialFailure()
    {
        var results = new[]
        {
            new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Confirmed
            },
            new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed
            }
        };

        var status = TourPackageBookingSupport.CalculateBookingStatus(results, TourPackageHoldStrategy.BestEffort);

        Assert.Equal(TourPackageBookingStatus.PartiallyConfirmed, status);
    }

    [Fact]
    public void CalculateBookingStatus_ReturnsFailed_ForAllOrNothingPartialFailure()
    {
        var results = new[]
        {
            new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Confirmed
            },
            new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed
            }
        };

        var status = TourPackageBookingSupport.CalculateBookingStatus(results, TourPackageHoldStrategy.AllOrNothing);

        Assert.Equal(TourPackageBookingStatus.Failed, status);
    }
}
