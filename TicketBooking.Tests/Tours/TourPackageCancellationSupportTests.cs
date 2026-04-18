using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageCancellationSupportTests
{
    [Fact]
    public void ResolveRule_MatchesConfiguredWindow()
    {
        var policyJson = """
        {"rules":[{"fromDay":7,"feePercent":0},{"fromDay":3,"toDay":6,"feePercent":30},{"sameDay":true,"feePercent":100}]}
        """;

        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);
        var departureDate = new DateOnly(2026, 4, 7);

        var result = TourPackageCancellationSupport.ResolveRule(policyJson, departureDate, now);

        Assert.Equal(30m, result.FeePercent);
        Assert.Contains("\"feePercent\":30", result.RawJson);
    }

    [Fact]
    public void CalculateAmounts_UsesMatchedRuleAndCapsPenalty()
    {
        var result = TourPackageCancellationSupport.CalculateAmounts(
            1_000_000m,
            new TourPackageCancellationMatchedRule
            {
                FeePercent = 30m
            });

        Assert.Equal(1_000_000m, result.GrossLineAmount);
        Assert.Equal(300_000m, result.PenaltyAmount);
        Assert.Equal(700_000m, result.RefundAmount);
    }

    [Fact]
    public void CalculateBookingStatus_ReturnsPartiallyCancelled_WhenActiveAndCancelledItemsCoexist()
    {
        var items = new[]
        {
            new TourPackageBookingItem
            {
                Status = TourPackageBookingItemStatus.RefundPending
            },
            new TourPackageBookingItem
            {
                Status = TourPackageBookingItemStatus.Confirmed
            }
        };

        var result = TourPackageCancellationSupport.CalculateBookingStatus(items);

        Assert.Equal(TourPackageBookingStatus.PartiallyCancelled, result);
    }
}
