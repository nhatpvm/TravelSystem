using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageQuoteBuilder
{
    public TourPackageQuoteBuildResult? Build(TourPackageQuoteBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SelectedOptions);
        ArgumentNullException.ThrowIfNull(request.SourceQuotes);

        if (request.Package is null)
        {
            if (request.SelectedOptions.Count > 0)
                throw new ArgumentException("Selected package options require an active package.", nameof(request));

            return null;
        }

        if (request.TourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(request));

        if (request.ScheduleId == Guid.Empty)
            throw new ArgumentException("ScheduleId is required.", nameof(request));

        if (request.TotalPax <= 0)
            throw new ArgumentException("TotalPax must be greater than 0.", nameof(request));

        if (request.TotalNights < 0)
            throw new ArgumentException("TotalNights cannot be negative.", nameof(request));

        if (request.Package.Id == Guid.Empty)
            throw new ArgumentException("Package.Id is required.", nameof(request));

        if (request.Package.TourId != request.TourId)
            throw new ArgumentException("Package does not belong to the requested tour.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Package.Code))
            throw new ArgumentException("Package.Code is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Package.Name))
            throw new ArgumentException("Package.Name is required.", nameof(request));

        if (!request.Package.IsActive || request.Package.IsDeleted || request.Package.Status != TourPackageStatus.Active)
            throw new ArgumentException("Package is not available for quote.", nameof(request));

        var duplicateSelectedOptionIds = request.SelectedOptions
            .GroupBy(x => x.OptionId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateSelectedOptionIds.Count > 0)
        {
            throw new ArgumentException(
                $"Duplicate package option selections are not allowed: {string.Join(", ", duplicateSelectedOptionIds)}.",
                nameof(request));
        }

        var selectedByOptionId = request.SelectedOptions
            .ToDictionary(x => x.OptionId, x => x);

        if (selectedByOptionId.Keys.Any(x => x == Guid.Empty))
            throw new ArgumentException("Selected package option OptionId is required.", nameof(request));

        if (selectedByOptionId.Values.Any(x => x.Quantity.HasValue && x.Quantity.Value <= 0))
            throw new ArgumentException("Selected package option quantity must be greater than 0.", nameof(request));

        var components = request.Package.Components
            .Where(TourPackageQuoteSupport.IsUsable)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var effectiveOptionsByComponentId = components.ToDictionary(
            x => x.Id,
            x => x.Options
                .Where(TourPackageQuoteSupport.IsUsable)
                .Select(option => BuildEffectiveOption(option, request.ScheduleId, request.SourceQuotes))
                .Where(x => !x.IsDisabled)
                .OrderByDescending(x => x.Option.IsDefaultSelected)
                .ThenBy(x => x.Option.SortOrder)
                .ThenBy(x => x.Option.Name)
                .ToList());

        var availableOptionIds = effectiveOptionsByComponentId.Values
            .SelectMany(x => x)
            .Select(x => x.Option.Id)
            .ToHashSet();

        var unavailableSelectedMessages = selectedByOptionId.Keys
            .Select(optionId =>
            {
                request.SourceQuotes.TryGetValue(optionId, out var sourceQuote);
                return sourceQuote;
            })
            .Where(x => x is not null && x.WasResolved && !x.IsAvailable && !string.IsNullOrWhiteSpace(x.ErrorMessage))
            .Select(x => x!.ErrorMessage!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unavailableSelectedMessages.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", unavailableSelectedMessages),
                nameof(request));
        }

        var invalidSelectedOptionIds = selectedByOptionId.Keys
            .Where(x => !availableOptionIds.Contains(x))
            .ToList();

        if (invalidSelectedOptionIds.Count > 0)
        {
            throw new ArgumentException(
                $"One or more selected package options are invalid for this schedule: {string.Join(", ", invalidSelectedOptionIds)}.",
                nameof(request));
        }

        var result = new TourPackageQuoteBuildResult
        {
            PackageId = request.Package.Id,
            PackageCode = request.Package.Code.Trim(),
            PackageName = request.Package.Name.Trim(),
            Mode = request.Package.Mode,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(request.ExpectedCurrency, request.Package.CurrencyCode),
            Notes = new List<string>()
        };

        foreach (var component in components)
        {
            effectiveOptionsByComponentId.TryGetValue(component.Id, out var availableOptions);
            availableOptions ??= new List<EffectivePackageOption>();

            if (availableOptions.Count == 0)
            {
                if (IsRequired(component.SelectionMode))
                {
                    throw new ArgumentException(
                        $"Component '{component.Name}' does not have any active options for the selected schedule.",
                        nameof(request));
                }

                continue;
            }

            var selectedOptions = ResolveSelections(
                component,
                availableOptions,
                selectedByOptionId,
                request.IncludeDefaultOptions);

            foreach (var selected in selectedOptions)
            {
                var requestedQuantity = selectedByOptionId.TryGetValue(selected.Option.Id, out var input)
                    ? input.Quantity
                    : null;

                var quantity = TourPackageQuoteSupport.ResolveQuantity(selected.Option, request.TotalPax, request.TotalNights, requestedQuantity);
                var price = ResolvePrice(selected.Option, selected.Override, selected.SourceQuote);
                var currencyCode = ResolveCurrency(selected.Option, selected.Override, selected.SourceQuote, result.CurrencyCode, price.IsIncluded);

                if (!price.IsIncluded && !string.Equals(currencyCode, result.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Mixed currencies are not supported in package quote. Option '{selected.Option.Name}' uses '{currencyCode}', expected '{result.CurrencyCode}'.",
                        nameof(request));
                }

                result.Lines.Add(new TourQuoteBuildPackageLineInput
                {
                    ComponentId = component.Id,
                    ComponentCode = component.Code.Trim(),
                    ComponentName = component.Name.Trim(),
                    ComponentType = component.ComponentType,
                    OptionId = selected.Option.Id,
                    Code = selected.Option.Code.Trim(),
                    Name = selected.Option.Name.Trim(),
                    Quantity = quantity,
                    CurrencyCode = price.IsIncluded ? result.CurrencyCode : currencyCode,
                    UnitPrice = price.UnitPrice,
                    UnitOriginalPrice = price.UnitOriginalPrice,
                    PricingMode = selected.Option.PricingMode,
                    IsRequired = IsRequired(component.SelectionMode),
                    IsDefaultSelected = selected.Option.IsDefaultSelected,
                    SelectedByOptimization = selected.WasAutoSelected || selected.SourceQuote?.WasOptimizedSelection == true,
                    OptimizationStrategy = selected.SourceQuote?.OptimizationStrategy,
                    OptimizationScore = selected.SourceQuote?.OptimizationScore,
                    SelectionReason = selected.WasAutoSelected
                        ? selected.AutoSelectionReason ?? selected.SourceQuote?.SelectionReason
                        : selected.SourceQuote?.SelectionReason,
                    BoundSourceEntityId = selected.SourceQuote?.BoundSourceEntityId ?? selected.Override?.BoundSourceEntityId ?? selected.Option.SourceEntityId,
                    Note = BuildLineNote(component, selected, price.IsIncluded)
                });
            }
        }

        result.Notes = BuildNotes(request.Package, result.Lines, request.SourceQuotes);
        return result;
    }

    private static List<EffectivePackageOption> ResolveSelections(
        TourPackageComponent component,
        List<EffectivePackageOption> availableOptions,
        IReadOnlyDictionary<Guid, TourPackageQuoteSelectedOptionInput> selectedByOptionId,
        bool includeDefaultOptions)
    {
        var explicitSelections = availableOptions
            .Where(x => selectedByOptionId.ContainsKey(x.Option.Id))
            .ToList();

        if (IsSingleSelection(component.SelectionMode))
        {
            if (explicitSelections.Count > 1)
            {
                throw new ArgumentException(
                    $"Component '{component.Name}' only allows a single option to be selected.");
            }

            if (explicitSelections.Count == 1)
                return explicitSelections;

            var defaults = availableOptions.Where(x => x.Option.IsDefaultSelected).ToList();

            if (includeDefaultOptions)
            {
                if (defaults.Count > 1)
                {
                    throw new ArgumentException(
                        $"Component '{component.Name}' has multiple default options configured for a single-select component.");
                }

                if (defaults.Count == 1)
                    return defaults;
            }

            if (IsRequired(component.SelectionMode))
            {
                if (availableOptions.Count == 1)
                    return new List<EffectivePackageOption> { availableOptions[0] };

                var autoSelected = TryAutoSelectSingle(component, availableOptions);
                if (autoSelected is not null)
                    return new List<EffectivePackageOption> { autoSelected };

                throw new ArgumentException(
                    $"Component '{component.Name}' requires one option to be selected.");
            }

            return new List<EffectivePackageOption>();
        }

        var selected = explicitSelections
            .ToDictionary(x => x.Option.Id, x => x);

        if (includeDefaultOptions)
        {
            foreach (var item in availableOptions.Where(x => x.Option.IsDefaultSelected))
                selected.TryAdd(item.Option.Id, item);
        }

        var selectedItems = selected.Values
            .OrderBy(x => x.Option.SortOrder)
            .ThenBy(x => x.Option.Name)
            .ToList();

        var minSelect = component.MinSelect ?? (IsRequired(component.SelectionMode) ? 1 : 0);
        var maxSelect = component.MaxSelect ?? int.MaxValue;

        if (selectedItems.Count == 0 && IsRequired(component.SelectionMode) && availableOptions.Count == minSelect)
            selectedItems = availableOptions.Take(minSelect).ToList();

        if (selectedItems.Count < minSelect && CanAutoSelect(component, availableOptions))
        {
            var missing = minSelect - selectedItems.Count;
            var autoCandidates = RankAutoSelectableOptions(availableOptions)
                .Where(x => !selected.ContainsKey(x.Option.Id))
                .Take(missing)
                .ToList();

            foreach (var candidate in autoCandidates)
            {
                candidate.WasAutoSelected = true;
                candidate.AutoSelectionReason = BuildAutoSelectionReason(candidate);
                selected[candidate.Option.Id] = candidate;
            }

            selectedItems = selected.Values
                .OrderBy(x => x.Option.SortOrder)
                .ThenBy(x => x.Option.Name)
                .ToList();
        }

        if (selectedItems.Count < minSelect)
        {
            throw new ArgumentException(
                $"Component '{component.Name}' requires at least {minSelect} option(s) to be selected.");
        }

        if (selectedItems.Count > maxSelect)
        {
            throw new ArgumentException(
                $"Component '{component.Name}' allows at most {maxSelect} option(s) to be selected.");
        }

        return selectedItems;
    }

    private static EffectivePackageOption? TryAutoSelectSingle(
        TourPackageComponent component,
        List<EffectivePackageOption> availableOptions)
    {
        if (!CanAutoSelect(component, availableOptions))
            return null;

        var autoSelected = RankAutoSelectableOptions(availableOptions).FirstOrDefault();
        if (autoSelected is null)
            return null;

        autoSelected.WasAutoSelected = true;
        autoSelected.AutoSelectionReason = BuildAutoSelectionReason(autoSelected);
        return autoSelected;
    }

    private static bool CanAutoSelect(
        TourPackageComponent component,
        List<EffectivePackageOption> availableOptions)
    {
        if (availableOptions.Count == 0)
            return false;

        return availableOptions.Any(x =>
            x.Option.IsDynamicCandidate ||
            x.Option.IsFallback ||
            x.SourceQuote?.WasResolved == true ||
            x.SourceQuote?.WasOptimizedSelection == true);
    }

    private static IEnumerable<EffectivePackageOption> RankAutoSelectableOptions(IEnumerable<EffectivePackageOption> options)
        => options
            .OrderBy(x => x.Option.IsFallback ? 1 : 0)
            .ThenByDescending(x => x.Option.IsDynamicCandidate)
            .ThenByDescending(x => x.SourceQuote?.OptimizationScore ?? decimal.MinValue)
            .ThenBy(x => x.SourceQuote?.UnitTotalPrice ?? decimal.MaxValue)
            .ThenBy(x => x.Option.SortOrder)
            .ThenBy(x => x.Option.Name);

    private static string BuildAutoSelectionReason(EffectivePackageOption option)
    {
        var details = new List<string> { "Auto-selected by dynamic package optimization" };

        if (option.Option.IsFallback)
            details.Add("fallback option used");

        if (!string.IsNullOrWhiteSpace(option.SourceQuote?.SelectionReason))
            details.Add(option.SourceQuote.SelectionReason.Trim());
        else if (option.SourceQuote?.OptimizationScore.HasValue == true)
            details.Add($"score {option.SourceQuote.OptimizationScore.Value:0.##}");

        return string.Join("; ", details);
    }

    private static ResolvedPackagePrice ResolvePrice(
        TourPackageComponentOption option,
        TourPackageScheduleOptionOverride? scheduleOverride,
        TourPackageSourceQuoteResult? sourceQuote)
    {
        var priceOverride = scheduleOverride?.PriceOverride ?? option.PriceOverride;
        var costOverride = scheduleOverride?.CostOverride ?? option.CostOverride;
        var sourceMarketPrice = sourceQuote?.UnitTotalPrice;
        var sourceComparablePrice = sourceQuote?.UnitTotalPrice ?? sourceQuote?.UnitCost;
        var sourceCost = sourceQuote?.UnitCost ?? sourceMarketPrice;

        return option.PricingMode switch
        {
            TourPackagePricingMode.Included => new ResolvedPackagePrice
            {
                IsIncluded = true,
                UnitPrice = 0m
            },
            TourPackagePricingMode.Override => new ResolvedPackagePrice
            {
                UnitPrice = priceOverride ?? throw new ArgumentException(
                    $"Package option '{option.Name}' requires PriceOverride when PricingMode = Override."),
                UnitOriginalPrice = ResolveOriginalPrice(
                    priceOverride,
                    sourceComparablePrice)
            },
            TourPackagePricingMode.PassThrough => new ResolvedPackagePrice
            {
                UnitPrice = priceOverride ?? sourceMarketPrice ?? costOverride ?? throw new ArgumentException(
                    $"Package option '{option.Name}' requires PriceOverride, live source price, or CostOverride when PricingMode = PassThrough."),
                UnitOriginalPrice = ResolveOriginalPrice(
                    priceOverride ?? sourceMarketPrice ?? costOverride,
                    sourceComparablePrice)
            },
            TourPackagePricingMode.Markup => new ResolvedPackagePrice
            {
                UnitPrice = ApplyMarkup(
                    costOverride ?? sourceCost ?? priceOverride ?? throw new ArgumentException(
                        $"Package option '{option.Name}' requires CostOverride, live source price, or PriceOverride when PricingMode = Markup."),
                    option.MarkupPercent,
                    option.MarkupAmount),
                UnitOriginalPrice = ResolveOriginalPrice(
                    ApplyMarkup(
                        costOverride ?? sourceCost ?? priceOverride ?? throw new ArgumentException(
                            $"Package option '{option.Name}' requires CostOverride, live source price, or PriceOverride when PricingMode = Markup."),
                        option.MarkupPercent,
                        option.MarkupAmount),
                    sourceComparablePrice ?? costOverride ?? sourceCost ?? priceOverride)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(option.PricingMode), option.PricingMode, "Unsupported pricing mode.")
        };
    }

    private static decimal ApplyMarkup(decimal basePrice, decimal? markupPercent, decimal? markupAmount)
    {
        var finalPrice = basePrice;

        if (markupPercent.HasValue)
        {
            if (markupPercent.Value < 0)
                throw new ArgumentException("MarkupPercent cannot be negative.");

            var ratio = markupPercent.Value <= 1m
                ? markupPercent.Value
                : markupPercent.Value / 100m;

            finalPrice += basePrice * ratio;
        }

        if (markupAmount.HasValue)
        {
            if (markupAmount.Value < 0)
                throw new ArgumentException("MarkupAmount cannot be negative.");

            finalPrice += markupAmount.Value;
        }

        return finalPrice;
    }

    private static string ResolveCurrency(
        TourPackageComponentOption option,
        TourPackageScheduleOptionOverride? scheduleOverride,
        TourPackageSourceQuoteResult? sourceQuote,
        string expectedCurrency,
        bool isIncluded)
    {
        if (isIncluded)
            return TourPackageQuoteSupport.NormalizeCurrency(expectedCurrency, expectedCurrency);

        return TourPackageQuoteSupport.NormalizeCurrency(
            scheduleOverride?.CurrencyCode,
            sourceQuote?.CurrencyCode,
            option.CurrencyCode,
            expectedCurrency);
    }

    private static EffectivePackageOption BuildEffectiveOption(
        TourPackageComponentOption option,
        Guid scheduleId,
        IReadOnlyDictionary<Guid, TourPackageSourceQuoteResult> sourceQuotes)
    {
        var scheduleOverride = TourPackageQuoteSupport.ResolveScheduleOverride(option, scheduleId);
        sourceQuotes.TryGetValue(option.Id, out var sourceQuote);

        return new EffectivePackageOption
        {
            Option = option,
            Override = scheduleOverride,
            SourceQuote = sourceQuote,
            IsDisabled = scheduleOverride?.Status == TourPackageScheduleOverrideStatus.Disabled
                || (sourceQuote?.WasResolved == true && !sourceQuote.IsAvailable)
        };
    }

    private static List<string> BuildNotes(
        TourPackage package,
        IReadOnlyCollection<TourQuoteBuildPackageLineInput> lines,
        IReadOnlyDictionary<Guid, TourPackageSourceQuoteResult> sourceQuotes)
    {
        var notes = new List<string>
        {
            $"Package applied: {package.Name.Trim()}."
        };

        if (lines.Any(x => x.PricingMode == TourPackagePricingMode.Included))
            notes.Add("Some package services are included in the base tour price.");

        if (lines.Any(x => x.BoundSourceEntityId.HasValue))
            notes.Add("Some package services are pinned to schedule-specific sources.");

        if (sourceQuotes.Values.Any(x => x.WasResolved && x.IsAvailable))
            notes.Add("Some package services use live source pricing and availability snapshots.");

        if (lines.Any(x => x.SelectedByOptimization))
            notes.Add("Some package services were auto-selected by dynamic package optimization.");

        if (sourceQuotes.Values.Any(x =>
                x.WasResolved &&
                x.IsAvailable &&
                x.SourceType == TourPackageSourceType.Flight))
        {
            notes.Add("Flight source prices may change before confirmation.");
        }

        return notes;
    }

    private static string? BuildLineNote(
        TourPackageComponent component,
        EffectivePackageOption selected,
        bool isIncluded)
    {
        var option = selected.Option;
        var scheduleOverride = selected.Override;
        var sourceQuote = selected.SourceQuote;
        var notes = new List<string>
        {
            component.ComponentType.ToString()
        };

        if (isIncluded)
            notes.Add("Included in base tour price");

        if (option.IsDefaultSelected)
            notes.Add("Default package option");

        if (option.IsFallback)
            notes.Add("Fallback package option");

        if (scheduleOverride?.Status == TourPackageScheduleOverrideStatus.Pinned)
            notes.Add("Pinned for selected schedule");

        if (selected.WasAutoSelected)
            notes.Add(selected.AutoSelectionReason ?? "Auto-selected by dynamic package optimization");

        if (!string.IsNullOrWhiteSpace(sourceQuote?.SourceName))
            notes.Add($"Source: {sourceQuote.SourceName.Trim()}");

        if (!string.IsNullOrWhiteSpace(sourceQuote?.SelectionReason) &&
            !(selected.WasAutoSelected &&
              !string.IsNullOrWhiteSpace(selected.AutoSelectionReason) &&
              selected.AutoSelectionReason.Contains(sourceQuote.SelectionReason, StringComparison.OrdinalIgnoreCase)))
        {
            notes.Add(sourceQuote.SelectionReason.Trim());
        }

        if (!string.IsNullOrWhiteSpace(sourceQuote?.Note))
            notes.Add(sourceQuote.Note.Trim());

        return notes.Count == 0 ? null : string.Join("; ", notes);
    }

    private static bool IsRequired(TourPackageSelectionMode selectionMode)
        => selectionMode == TourPackageSelectionMode.RequiredSingle || selectionMode == TourPackageSelectionMode.RequiredMulti;

    private static bool IsSingleSelection(TourPackageSelectionMode selectionMode)
        => selectionMode == TourPackageSelectionMode.RequiredSingle || selectionMode == TourPackageSelectionMode.OptionalSingle;

    private sealed class EffectivePackageOption
    {
        public TourPackageComponentOption Option { get; set; } = null!;
        public TourPackageScheduleOptionOverride? Override { get; set; }
        public TourPackageSourceQuoteResult? SourceQuote { get; set; }
        public bool IsDisabled { get; set; }
        public bool WasAutoSelected { get; set; }
        public string? AutoSelectionReason { get; set; }
    }

    private sealed class ResolvedPackagePrice
    {
        public decimal UnitPrice { get; set; }
        public decimal? UnitOriginalPrice { get; set; }
        public bool IsIncluded { get; set; }
    }

    private static decimal? ResolveOriginalPrice(decimal? finalPrice, decimal? comparablePrice)
    {
        if (!finalPrice.HasValue || !comparablePrice.HasValue)
            return comparablePrice;

        return finalPrice.Value == comparablePrice.Value
            ? null
            : comparablePrice.Value;
    }
}

public sealed class TourPackageQuoteBuildRequest
{
    public Guid TourId { get; set; }
    public Guid ScheduleId { get; set; }
    public int TotalPax { get; set; }
    public int TotalNights { get; set; }
    public string ExpectedCurrency { get; set; } = "";
    public bool IncludeDefaultOptions { get; set; } = true;
    public TourPackage? Package { get; set; }
    public List<TourPackageQuoteSelectedOptionInput> SelectedOptions { get; set; } = new();
    public IReadOnlyDictionary<Guid, TourPackageSourceQuoteResult> SourceQuotes { get; set; } = new Dictionary<Guid, TourPackageSourceQuoteResult>();
}

public sealed class TourPackageQuoteSelectedOptionInput
{
    public Guid OptionId { get; set; }
    public int? Quantity { get; set; }
}

public sealed class TourPackageQuoteBuildResult
{
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = "";
    public string PackageName { get; set; } = "";
    public TourPackageMode Mode { get; set; }
    public string CurrencyCode { get; set; } = "";
    public List<TourQuoteBuildPackageLineInput> Lines { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
