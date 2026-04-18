using TicketBooking.Api.Services.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageOptimizationSupportTests
{
    [Fact]
    public void SelectBestCandidate_PrefersRecommendedCandidateMatchingPreferences()
    {
        var template = new TourPackageSearchTemplate
        {
            SelectionStrategy = TourPackageSourceSelectionStrategy.Recommended,
            PreferDirect = true,
            PreferRefundable = true,
            PreferredDepartureHourFrom = 8,
            PreferredDepartureHourTo = 11
        };

        var candidates = new[]
        {
            new Candidate
            {
                Code = "CHEAP-1STOP",
                Price = 90m,
                DepartureAt = new DateTimeOffset(2026, 4, 10, 6, 0, 0, TimeSpan.FromHours(7)),
                ArrivalAt = new DateTimeOffset(2026, 4, 10, 9, 30, 0, TimeSpan.FromHours(7)),
                StopCount = 1,
                Refundable = false
            },
            new Candidate
            {
                Code = "REC-DIRECT",
                Price = 110m,
                DepartureAt = new DateTimeOffset(2026, 4, 10, 9, 0, 0, TimeSpan.FromHours(7)),
                ArrivalAt = new DateTimeOffset(2026, 4, 10, 10, 30, 0, TimeSpan.FromHours(7)),
                StopCount = 0,
                Refundable = true
            }
        };

        var result = TourPackageOptimizationSupport.SelectBestCandidate(
            candidates,
            template,
            candidate => new TourPackageOptimizationCandidateContext
            {
                Label = candidate.Code,
                UnitPrice = candidate.Price,
                ServiceStartAt = candidate.DepartureAt,
                ServiceEndAt = candidate.ArrivalAt,
                StopCount = candidate.StopCount,
                IsRefundable = candidate.Refundable
            });

        Assert.Equal("REC-DIRECT", result.Candidate.Code);
        Assert.Equal("recommended", result.StrategyLabel);
        Assert.Contains("direct service", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("refundable option", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Candidate
    {
        public string Code { get; set; } = "";
        public decimal Price { get; set; }
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }
        public int StopCount { get; set; }
        public bool Refundable { get; set; }
    }
}
