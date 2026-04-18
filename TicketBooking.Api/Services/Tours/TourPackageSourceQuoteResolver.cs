using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public interface ITourPackageSourceQuoteAdapter
{
    bool CanHandle(TourPackageSourceType sourceType);

    Task<TourPackageSourceQuoteResult> ResolveAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct = default);
}

public sealed class TourPackageSourceQuoteResolver
{
    private readonly IReadOnlyCollection<ITourPackageSourceQuoteAdapter> _adapters;

    public TourPackageSourceQuoteResolver(IEnumerable<ITourPackageSourceQuoteAdapter> adapters)
    {
        _adapters = adapters?.ToList() ?? throw new ArgumentNullException(nameof(adapters));
    }

    public async Task<TourPackageSourceQuoteResolutionResult> ResolveAsync(
        TourPackageSourceQuoteResolverRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Tour);
        ArgumentNullException.ThrowIfNull(request.Schedule);
        ArgumentNullException.ThrowIfNull(request.Package);
        ArgumentNullException.ThrowIfNull(request.SelectedOptions);

        var result = new TourPackageSourceQuoteResolutionResult();
        var selectedByOptionId = request.SelectedOptions
            .GroupBy(x => x.OptionId)
            .ToDictionary(g => g.Key, g => g.Last());

        foreach (var component in request.Package.Components
                     .Where(TourPackageQuoteSupport.IsUsable)
                     .OrderBy(x => x.SortOrder)
                     .ThenBy(x => x.Name))
        {
            foreach (var option in component.Options
                         .Where(TourPackageQuoteSupport.IsUsable)
                         .OrderBy(x => x.SortOrder)
                         .ThenBy(x => x.Name))
            {
                var scheduleOverride = TourPackageQuoteSupport.ResolveScheduleOverride(option, request.Schedule.Id);
                if (scheduleOverride?.Status == TourPackageScheduleOverrideStatus.Disabled)
                    continue;

                var adapter = _adapters.FirstOrDefault(x => x.CanHandle(option.SourceType));
                if (adapter is null)
                    continue;

                if (option.BindingMode != TourPackageBindingMode.StaticReference &&
                    option.BindingMode != TourPackageBindingMode.SearchTemplate)
                {
                    continue;
                }

                var sourceEntityId = scheduleOverride?.BoundSourceEntityId ?? option.SourceEntityId;
                if (option.BindingMode == TourPackageBindingMode.StaticReference &&
                    (!sourceEntityId.HasValue || sourceEntityId.Value == Guid.Empty))
                {
                    result.SourceQuotes[option.Id] = TourPackageSourceQuoteResultFactory.Unavailable(
                        request,
                        component,
                        option,
                        scheduleOverride,
                        "Static package source is missing SourceEntityId.");
                    continue;
                }

                var requestedQuantity = TryResolveQuantity(
                    option,
                    request.TotalPax,
                    request.TotalNights,
                    selectedByOptionId.TryGetValue(option.Id, out var selected) ? selected.Quantity : null);

                var sourceQuote = await adapter.ResolveAsync(new TourPackageSourceQuoteAdapterRequest
                {
                    Tour = request.Tour,
                    Schedule = request.Schedule,
                    Package = request.Package,
                    Component = component,
                    Option = option,
                    ScheduleOverride = scheduleOverride,
                    SourceEntityId = sourceEntityId ?? Guid.Empty,
                    TotalPax = request.TotalPax,
                    TotalNights = request.TotalNights,
                    RequestedQuantity = requestedQuantity
                }, ct);

                result.SourceQuotes[option.Id] = sourceQuote;
            }
        }

        if (result.SourceQuotes.Values.Any(x => x.WasResolved && x.IsAvailable))
        {
            result.Notes.Add("Some package services use live source pricing and availability snapshots.");
        }

        if (result.SourceQuotes.Values.Any(x => x.WasResolved && x.IsAvailable && x.WasOptimizedSelection))
        {
            result.Notes.Add("Dynamic package optimization selected the best available source candidates for some services.");
        }

        if (result.SourceQuotes.Values.Any(x =>
                x.WasResolved &&
                x.IsAvailable &&
                x.SourceType == TourPackageSourceType.Flight))
        {
            result.Notes.Add("Flight source prices may change before confirmation.");
        }

        return result;
    }

    private static int TryResolveQuantity(
        TourPackageComponentOption option,
        int totalPax,
        int totalNights,
        int? requestedQuantity)
    {
        try
        {
            return TourPackageQuoteSupport.ResolveQuantity(option, totalPax, totalNights, requestedQuantity);
        }
        catch
        {
            return Math.Max(option.DefaultQuantity, 1);
        }
    }
}

public sealed class TourPackageSourceQuoteResolverRequest
{
    public Tour Tour { get; set; } = null!;
    public TourSchedule Schedule { get; set; } = null!;
    public TourPackage Package { get; set; } = null!;
    public int TotalPax { get; set; }
    public int TotalNights { get; set; }
    public List<TourPackageQuoteSelectedOptionInput> SelectedOptions { get; set; } = new();
}

public sealed class TourPackageSourceQuoteResolutionResult
{
    public Dictionary<Guid, TourPackageSourceQuoteResult> SourceQuotes { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public sealed class TourPackageSourceQuoteAdapterRequest
{
    public Tour Tour { get; set; } = null!;
    public TourSchedule Schedule { get; set; } = null!;
    public TourPackage Package { get; set; } = null!;
    public TourPackageComponent Component { get; set; } = null!;
    public TourPackageComponentOption Option { get; set; } = null!;
    public TourPackageScheduleOptionOverride? ScheduleOverride { get; set; }
    public Guid SourceEntityId { get; set; }
    public int TotalPax { get; set; }
    public int TotalNights { get; set; }
    public int RequestedQuantity { get; set; }
}

public sealed class TourPackageSourceQuoteResult
{
    public Guid OptionId { get; set; }
    public TourPackageSourceType SourceType { get; set; }
    public bool WasResolved { get; set; }
    public bool IsAvailable { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal? UnitBasePrice { get; set; }
    public decimal? UnitTaxes { get; set; }
    public decimal? UnitFees { get; set; }
    public decimal? UnitTotalPrice { get; set; }
    public decimal? UnitCost { get; set; }
    public string? SourceCode { get; set; }
    public string? SourceName { get; set; }
    public string? Note { get; set; }
    public bool WasOptimizedSelection { get; set; }
    public string? OptimizationStrategy { get; set; }
    public string? SelectionReason { get; set; }
    public decimal? OptimizationScore { get; set; }
    public int? CandidateCount { get; set; }
    public DateTimeOffset? ServiceStartAt { get; set; }
    public DateTimeOffset? ServiceEndAt { get; set; }
    public int? StopCount { get; set; }
    public bool? IsRefundable { get; set; }
    public bool? IncludesBreakfast { get; set; }
    public string? ErrorMessage { get; set; }
}

internal static class TourPackageSourceQuoteResultFactory
{
    public static TourPackageSourceQuoteResult Unavailable(
        TourPackageSourceQuoteResolverRequest request,
        TourPackageComponent component,
        TourPackageComponentOption option,
        TourPackageScheduleOptionOverride? scheduleOverride,
        string message)
        => Unavailable(
            new TourPackageSourceQuoteAdapterRequest
            {
                Tour = request.Tour,
                Schedule = request.Schedule,
                Package = request.Package,
                Component = component,
                Option = option,
                ScheduleOverride = scheduleOverride,
                SourceEntityId = scheduleOverride?.BoundSourceEntityId ?? option.SourceEntityId ?? Guid.Empty
            },
            message);

    public static TourPackageSourceQuoteResult Unavailable(
        TourPackageSourceQuoteAdapterRequest request,
        string message)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new TourPackageSourceQuoteResult
        {
            OptionId = request.Option.Id,
            SourceType = request.Option.SourceType,
            WasResolved = true,
            IsAvailable = false,
            BoundSourceEntityId = request.SourceEntityId != Guid.Empty ? request.SourceEntityId : null,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(
                request.ScheduleOverride?.CurrencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode,
                "VND"),
            ErrorMessage = message
        };
    }
}
