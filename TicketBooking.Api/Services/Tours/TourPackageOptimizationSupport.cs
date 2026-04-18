using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPackageOptimizationSupport
{
    public static TourPackageOptimizationDecision<T> SelectBestCandidate<T>(
        IReadOnlyCollection<T> candidates,
        TourPackageSearchTemplate template,
        Func<T, TourPackageOptimizationCandidateContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(contextFactory);

        if (candidates.Count == 0)
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));

        var evaluations = candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Context = contextFactory(candidate)
            })
            .ToList();

        var priceRange = BuildDecimalRange(evaluations.Select(x => x.Context.UnitPrice));
        var durationRange = BuildDecimalRange(evaluations.Select(x => x.Context.DurationMinutes));
        var startRange = BuildDateRange(evaluations.Select(x => x.Context.ServiceStartAt));
        var stopRange = BuildDecimalRange(evaluations.Select(x => x.Context.StopCount.HasValue ? (decimal?)x.Context.StopCount.Value : null));

        var strategy = TourPackageSearchTemplateSupport.ResolveStrategy(template);
        var scored = evaluations
            .Select(x =>
            {
                var priceScore = ScoreLowValuePreferred(x.Context.UnitPrice, priceRange);
                var durationScore = ScoreLowValuePreferred(x.Context.DurationMinutes, durationRange);
                var startScore = ScoreEarlyValuePreferred(x.Context.ServiceStartAt, startRange);
                var stopScore = ScoreLowValuePreferred(
                    x.Context.StopCount.HasValue ? x.Context.StopCount.Value : null,
                    stopRange);

                var weightedScore = strategy switch
                {
                    TourPackageSourceSelectionStrategy.Earliest =>
                        (startScore * 0.65m) + (priceScore * 0.15m) + (durationScore * 0.10m) + (stopScore * 0.10m),
                    TourPackageSourceSelectionStrategy.ShortestDuration =>
                        (durationScore * 0.65m) + (priceScore * 0.15m) + (startScore * 0.10m) + (stopScore * 0.10m),
                    TourPackageSourceSelectionStrategy.BestValue =>
                        (priceScore * 0.45m) + (durationScore * 0.20m) + (stopScore * 0.15m) + (startScore * 0.10m),
                    TourPackageSourceSelectionStrategy.Recommended =>
                        (priceScore * 0.30m) + (durationScore * 0.20m) + (stopScore * 0.15m) + (startScore * 0.15m),
                    _ =>
                        (priceScore * 0.65m) + (startScore * 0.15m) + (durationScore * 0.10m) + (stopScore * 0.10m)
                };

                var preferenceBonus = ComputePreferenceBonus(template, x.Context);
                var score = Math.Round(weightedScore + preferenceBonus, 4, MidpointRounding.AwayFromZero);

                return new
                {
                    x.Candidate,
                    x.Context,
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Context.UnitPrice ?? decimal.MaxValue)
            .ThenBy(x => x.Context.ServiceStartAt ?? DateTimeOffset.MaxValue)
            .ThenBy(x => x.Context.DurationMinutes ?? decimal.MaxValue)
            .ThenBy(x => x.Context.StopCount ?? int.MaxValue)
            .ToList();

        var best = scored[0];

        return new TourPackageOptimizationDecision<T>
        {
            Candidate = best.Candidate,
            Context = best.Context,
            Score = best.Score,
            CandidateCount = scored.Count,
            Strategy = strategy,
            StrategyLabel = ResolveStrategyLabel(strategy),
            Reason = BuildReason(template, best.Context, best.Score, strategy)
        };
    }

    public static string ResolveStrategyLabel(TourPackageSourceSelectionStrategy strategy)
        => strategy switch
        {
            TourPackageSourceSelectionStrategy.Earliest => "earliest",
            TourPackageSourceSelectionStrategy.BestValue => "best-value",
            TourPackageSourceSelectionStrategy.Recommended => "recommended",
            TourPackageSourceSelectionStrategy.ShortestDuration => "shortest-duration",
            _ => "cheapest"
        };

    private static decimal ComputePreferenceBonus(
        TourPackageSearchTemplate template,
        TourPackageOptimizationCandidateContext context)
    {
        decimal bonus = 0m;

        if (template.MaxUnitPrice.HasValue && context.UnitPrice.HasValue)
            bonus += context.UnitPrice.Value <= template.MaxUnitPrice.Value ? 5m : -25m;

        if (template.MaxStops.HasValue && context.StopCount.HasValue)
            bonus += context.StopCount.Value <= template.MaxStops.Value ? 5m : -15m;

        if (template.PreferDirect == true && context.StopCount.HasValue)
            bonus += context.StopCount.Value == 0 ? 8m : -10m;

        if (template.PreferRefundable == true && context.IsRefundable.HasValue)
            bonus += context.IsRefundable.Value ? 8m : -12m;

        if (template.PreferBreakfastIncluded == true && context.IncludesBreakfast.HasValue)
            bonus += context.IncludesBreakfast.Value ? 6m : -8m;

        if (MatchesHourWindow(context.ServiceStartAt, template.PreferredDepartureHourFrom, template.PreferredDepartureHourTo))
            bonus += 4m;
        else if (template.PreferredDepartureHourFrom.HasValue || template.PreferredDepartureHourTo.HasValue)
            bonus -= 4m;

        if (MatchesHourWindow(context.ServiceEndAt, template.PreferredArrivalHourFrom, template.PreferredArrivalHourTo))
            bonus += 4m;
        else if (template.PreferredArrivalHourFrom.HasValue || template.PreferredArrivalHourTo.HasValue)
            bonus -= 4m;

        return bonus;
    }

    private static string BuildReason(
        TourPackageSearchTemplate template,
        TourPackageOptimizationCandidateContext context,
        decimal score,
        TourPackageSourceSelectionStrategy strategy)
    {
        var highlights = new List<string>();

        switch (strategy)
        {
            case TourPackageSourceSelectionStrategy.Earliest:
                highlights.Add("earliest departure");
                break;
            case TourPackageSourceSelectionStrategy.ShortestDuration:
                highlights.Add("shortest travel duration");
                break;
            case TourPackageSourceSelectionStrategy.BestValue:
                highlights.Add("strong price-to-convenience balance");
                break;
            case TourPackageSourceSelectionStrategy.Recommended:
                highlights.Add("best overall trade-off");
                break;
            default:
                highlights.Add("lowest live price");
                break;
        }

        if (template.PreferDirect == true && context.StopCount == 0)
            highlights.Add("direct service");

        if (template.PreferRefundable == true && context.IsRefundable == true)
            highlights.Add("refundable option");

        if (template.PreferBreakfastIncluded == true && context.IncludesBreakfast == true)
            highlights.Add("breakfast included");

        if (MatchesHourWindow(context.ServiceStartAt, template.PreferredDepartureHourFrom, template.PreferredDepartureHourTo))
            highlights.Add("matches preferred departure window");

        if (MatchesHourWindow(context.ServiceEndAt, template.PreferredArrivalHourFrom, template.PreferredArrivalHourTo))
            highlights.Add("matches preferred arrival window");

        var reason = $"Selected by {ResolveStrategyLabel(strategy)} optimization";
        if (highlights.Count > 0)
            reason += $": {string.Join(", ", highlights.Distinct(StringComparer.OrdinalIgnoreCase))}.";
        else
            reason += ".";

        return $"{reason} Score {score:0.##}.";
    }

    private static bool MatchesHourWindow(DateTimeOffset? value, int? fromHour, int? toHour)
    {
        if (!value.HasValue || (!fromHour.HasValue && !toHour.HasValue))
            return false;

        var start = fromHour ?? 0;
        var end = toHour ?? 23;
        var hour = value.Value.Hour;

        if (start <= end)
            return hour >= start && hour <= end;

        return hour >= start || hour <= end;
    }

    private static DecimalRange BuildDecimalRange(IEnumerable<decimal?> values)
    {
        var normalized = values
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        return normalized.Count == 0
            ? DecimalRange.Empty
            : new DecimalRange(normalized.Min(), normalized.Max(), true);
    }

    private static DecimalRange BuildDecimalRange(IEnumerable<int?> values)
        => BuildDecimalRange(values.Select(x => x.HasValue ? (decimal?)x.Value : null));

    private static DateRange BuildDateRange(IEnumerable<DateTimeOffset?> values)
    {
        var normalized = values
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        return normalized.Count == 0
            ? DateRange.Empty
            : new DateRange(normalized.Min(), normalized.Max(), true);
    }

    private static decimal ScoreLowValuePreferred(decimal? value, DecimalRange range)
    {
        if (!value.HasValue || range.IsEmpty)
            return 50m;

        if (range.Max == range.Min)
            return 100m;

        var normalized = (value.Value - range.Min) / (range.Max - range.Min);
        return Clamp(100m - (normalized * 100m), 0m, 100m);
    }

    private static decimal ScoreEarlyValuePreferred(DateTimeOffset? value, DateRange range)
    {
        if (!value.HasValue || range.IsEmpty)
            return 50m;

        if (range.Max == range.Min)
            return 100m;

        var totalTicks = range.Max.UtcTicks - range.Min.UtcTicks;
        if (totalTicks <= 0)
            return 100m;

        var normalized = (decimal)(value.Value.UtcTicks - range.Min.UtcTicks) / totalTicks;
        return Clamp(100m - (normalized * 100m), 0m, 100m);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
        => value < min ? min : value > max ? max : value;

    private readonly record struct DecimalRange(decimal Min, decimal Max, bool HasData)
    {
        public static DecimalRange Empty => new(decimal.Zero, decimal.Zero, false);
        public bool IsEmpty => !HasData;
    }

    private readonly record struct DateRange(DateTimeOffset Min, DateTimeOffset Max, bool HasData)
    {
        public static DateRange Empty => new(DateTimeOffset.MinValue, DateTimeOffset.MinValue, false);
        public bool IsEmpty => !HasData;
    }
}

public sealed class TourPackageOptimizationDecision<T>
{
    public T Candidate { get; set; } = default!;
    public TourPackageOptimizationCandidateContext Context { get; set; } = new();
    public decimal Score { get; set; }
    public int CandidateCount { get; set; }
    public TourPackageSourceSelectionStrategy Strategy { get; set; }
    public string StrategyLabel { get; set; } = "";
    public string Reason { get; set; } = "";
}

public sealed class TourPackageOptimizationCandidateContext
{
    public string? Label { get; set; }
    public decimal? UnitPrice { get; set; }
    public DateTimeOffset? ServiceStartAt { get; set; }
    public DateTimeOffset? ServiceEndAt { get; set; }
    public decimal? DurationMinutes
        => ServiceStartAt.HasValue && ServiceEndAt.HasValue && ServiceEndAt >= ServiceStartAt
            ? (decimal)(ServiceEndAt.Value - ServiceStartAt.Value).TotalMinutes
            : null;

    public int? StopCount { get; set; }
    public bool? IsRefundable { get; set; }
    public bool? IncludesBreakfast { get; set; }
}
