// FILE #272: TicketBooking.Api/Services/Tours/TourQuoteBuilder.cs
using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourQuoteBuilder
{
    public TourQuoteBuildResult Build(TourQuoteBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Schedule);
        ArgumentNullException.ThrowIfNull(request.Passengers);
        ArgumentNullException.ThrowIfNull(request.Addons);
        ArgumentNullException.ThrowIfNull(request.AdditionalNotes);
        ArgumentNullException.ThrowIfNull(request.PackageLines);

        if (request.TourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.TourName))
            throw new ArgumentException("TourName is required.", nameof(request));

        if (request.Schedule.ScheduleId == Guid.Empty)
            throw new ArgumentException("ScheduleId is required.", nameof(request));

        if (request.Passengers.Count == 0)
            throw new ArgumentException("At least one passenger line is required.", nameof(request));

        if (request.Passengers.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Passenger quantity must be greater than 0.", nameof(request));

        if (request.Addons.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Addon quantity must be greater than 0.", nameof(request));

        if (request.PackageLines.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Package line quantity must be greater than 0.", nameof(request));

        var passengerDuplicateTypes = request.Passengers
            .GroupBy(x => x.PriceType)
            .Any(g => g.Count() > 1);

        if (passengerDuplicateTypes)
            throw new ArgumentException("Duplicate passenger PriceType values are not allowed.", nameof(request));

        var addonDuplicateIds = request.Addons
            .GroupBy(x => x.AddonId)
            .Any(g => g.Count() > 1);

        if (addonDuplicateIds)
            throw new ArgumentException("Duplicate AddonId values are not allowed.", nameof(request));

        var packageLineDuplicateIds = request.PackageLines
            .Where(x => x.OptionId != Guid.Empty)
            .GroupBy(x => x.OptionId)
            .Any(g => g.Count() > 1);

        if (packageLineDuplicateIds)
            throw new ArgumentException("Duplicate package OptionId values are not allowed.", nameof(request));

        if (request.Package is not null && request.Package.PackageId == Guid.Empty)
            throw new ArgumentException("PackageId is required when package info is supplied.", nameof(request));

        var currencyCode = NormalizeCurrency(request.Schedule.CurrencyCode, request.TourCurrencyCode);

        var result = new TourQuoteBuildResult
        {
            TourId = request.TourId,
            TourName = request.TourName.Trim(),
            CurrencyCode = currencyCode,
            TotalPax = request.Passengers.Sum(x => x.Quantity),
            IsWaitlist = request.IsWaitlist,
            Package = request.Package is null
                ? null
                : new TourQuoteBuildPackageResult
                {
                    PackageId = request.Package.PackageId,
                    Code = request.Package.Code?.Trim() ?? string.Empty,
                    Name = request.Package.Name?.Trim() ?? string.Empty,
                    Mode = request.Package.Mode
                },
            Schedule = new TourQuoteBuildScheduleResult
            {
                ScheduleId = request.Schedule.ScheduleId,
                Code = request.Schedule.Code?.Trim() ?? string.Empty,
                Name = TrimOrNull(request.Schedule.Name),
                DepartureDate = request.Schedule.DepartureDate,
                ReturnDate = request.Schedule.ReturnDate,
                DepartureTime = request.Schedule.DepartureTime,
                ReturnTime = request.Schedule.ReturnTime,
                BookingOpenAt = request.Schedule.BookingOpenAt,
                BookingCutoffAt = request.Schedule.BookingCutoffAt,
                IsGuaranteedDeparture = request.Schedule.IsGuaranteedDeparture,
                IsInstantConfirm = request.Schedule.IsInstantConfirm,
                AvailableSlots = request.Schedule.AvailableSlots,
                AllowWaitlist = request.Schedule.AllowWaitlist
            }
        };

        decimal subtotal = 0m;
        decimal originalSubtotal = 0m;
        decimal extraTaxes = 0m;
        decimal extraFees = 0m;

        foreach (var passenger in request.Passengers)
        {
            ValidateCurrency(passenger.CurrencyCode, currencyCode, $"passenger:{passenger.PriceType}");

            var unitPrice = EnsureNonNegative(passenger.UnitPrice, $"{passenger.PriceType}.UnitPrice");
            var unitOriginalPrice = passenger.UnitOriginalPrice.HasValue
                ? EnsureNonNegative(passenger.UnitOriginalPrice.Value, $"{passenger.PriceType}.UnitOriginalPrice")
                : (decimal?)null;

            var unitTaxes = passenger.UnitTaxes.HasValue
                ? EnsureNonNegative(passenger.UnitTaxes.Value, $"{passenger.PriceType}.UnitTaxes")
                : 0m;

            var unitFees = passenger.UnitFees.HasValue
                ? EnsureNonNegative(passenger.UnitFees.Value, $"{passenger.PriceType}.UnitFees")
                : 0m;

            var baseAmount = unitPrice * passenger.Quantity;
            var originalAmount = (unitOriginalPrice ?? unitPrice) * passenger.Quantity;
            var taxAmount = passenger.IsIncludedTax ? 0m : unitTaxes * passenger.Quantity;
            var feeAmount = passenger.IsIncludedFee ? 0m : unitFees * passenger.Quantity;

            subtotal += baseAmount;
            originalSubtotal += originalAmount;
            extraTaxes += taxAmount;
            extraFees += feeAmount;

            result.PassengerLines.Add(new TourQuoteBuildLine
            {
                Kind = "passenger",
                ReferenceId = null,
                Code = passenger.PriceType.ToString(),
                Name = passenger.DisplayName?.Trim() ?? passenger.PriceType.ToString(),
                Quantity = passenger.Quantity,
                UnitPrice = unitPrice,
                UnitOriginalPrice = unitOriginalPrice,
                LineBaseAmount = baseAmount,
                LineOriginalAmount = originalAmount,
                LineTaxAmount = taxAmount,
                LineFeeAmount = feeAmount,
                CurrencyCode = currencyCode,
                Notes = TrimOrNull(passenger.Label)
            });
        }

        foreach (var addon in request.Addons)
        {
            if (addon.AddonId == Guid.Empty)
                throw new ArgumentException("AddonId is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(addon.Code))
                throw new ArgumentException("Addon code is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(addon.Name))
                throw new ArgumentException("Addon name is required.", nameof(request));

            ValidateCurrency(addon.CurrencyCode, currencyCode, $"addon:{addon.Code}");

            var unitPrice = EnsureNonNegative(addon.UnitPrice, $"{addon.Code}.UnitPrice");
            var unitOriginalPrice = addon.UnitOriginalPrice.HasValue
                ? EnsureNonNegative(addon.UnitOriginalPrice.Value, $"{addon.Code}.UnitOriginalPrice")
                : (decimal?)null;

            var baseAmount = unitPrice * addon.Quantity;
            var originalAmount = (unitOriginalPrice ?? unitPrice) * addon.Quantity;

            subtotal += baseAmount;
            originalSubtotal += originalAmount;

            result.AddonLines.Add(new TourQuoteBuildLine
            {
                Kind = "addon",
                ReferenceId = addon.AddonId,
                Code = addon.Code.Trim(),
                Name = addon.Name.Trim(),
                Quantity = addon.Quantity,
                UnitPrice = unitPrice,
                UnitOriginalPrice = unitOriginalPrice,
                LineBaseAmount = baseAmount,
                LineOriginalAmount = originalAmount,
                LineTaxAmount = 0m,
                LineFeeAmount = 0m,
                CurrencyCode = currencyCode,
                Notes = BuildAddonNote(addon)
            });
        }

        foreach (var packageLine in request.PackageLines)
        {
            if (packageLine.ComponentId == Guid.Empty)
                throw new ArgumentException("Package ComponentId is required.", nameof(request));

            if (packageLine.OptionId == Guid.Empty)
                throw new ArgumentException("Package OptionId is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(packageLine.ComponentCode))
                throw new ArgumentException("Package component code is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(packageLine.ComponentName))
                throw new ArgumentException("Package component name is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(packageLine.Code))
                throw new ArgumentException("Package option code is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(packageLine.Name))
                throw new ArgumentException("Package option name is required.", nameof(request));

            ValidateCurrency(packageLine.CurrencyCode, currencyCode, $"package:{packageLine.Code}");

            var unitPrice = EnsureNonNegative(packageLine.UnitPrice, $"{packageLine.Code}.UnitPrice");
            var unitOriginalPrice = packageLine.UnitOriginalPrice.HasValue
                ? EnsureNonNegative(packageLine.UnitOriginalPrice.Value, $"{packageLine.Code}.UnitOriginalPrice")
                : (decimal?)null;

            var baseAmount = unitPrice * packageLine.Quantity;
            var originalAmount = (unitOriginalPrice ?? unitPrice) * packageLine.Quantity;

            subtotal += baseAmount;
            originalSubtotal += originalAmount;

            result.PackageLines.Add(new TourQuoteBuildPackageLine
            {
                ComponentId = packageLine.ComponentId,
                ComponentCode = packageLine.ComponentCode.Trim(),
                ComponentName = packageLine.ComponentName.Trim(),
                ComponentType = packageLine.ComponentType,
                OptionId = packageLine.OptionId,
                BoundSourceEntityId = packageLine.BoundSourceEntityId,
                Kind = "package",
                ReferenceId = packageLine.OptionId,
                Code = packageLine.Code.Trim(),
                Name = packageLine.Name.Trim(),
                Quantity = packageLine.Quantity,
                UnitPrice = unitPrice,
                UnitOriginalPrice = unitOriginalPrice,
                LineBaseAmount = baseAmount,
                LineOriginalAmount = originalAmount,
                LineTaxAmount = 0m,
                LineFeeAmount = 0m,
                CurrencyCode = currencyCode,
                PricingMode = packageLine.PricingMode,
                IsRequired = packageLine.IsRequired,
                IsDefaultSelected = packageLine.IsDefaultSelected,
                SelectedByOptimization = packageLine.SelectedByOptimization,
                OptimizationStrategy = TrimOrNull(packageLine.OptimizationStrategy),
                OptimizationScore = packageLine.OptimizationScore,
                SelectionReason = TrimOrNull(packageLine.SelectionReason),
                Notes = TrimOrNull(packageLine.Note)
            });
        }

        result.SubtotalAmount = subtotal;
        result.TaxAmount = extraTaxes;
        result.FeeAmount = extraFees;
        result.TotalAmount = subtotal + extraTaxes + extraFees;
        result.OriginalTotalAmount = originalSubtotal;
        result.DiscountAmount = originalSubtotal > subtotal ? originalSubtotal - subtotal : 0m;
        result.Notes = BuildNotes(result, request);

        return result;
    }

    private static List<string> BuildNotes(TourQuoteBuildResult result, TourQuoteBuildRequest request)
    {
        var notes = new List<string>();

        if (result.Schedule.BookingCutoffAt.HasValue)
            notes.Add($"Booking cutoff: {result.Schedule.BookingCutoffAt:yyyy-MM-dd HH:mm:ss zzz}");

        if (result.Schedule.IsGuaranteedDeparture)
            notes.Add("Guaranteed departure.");

        if (result.Schedule.IsInstantConfirm)
            notes.Add("Instant confirmation supported.");

        if (result.Schedule.AvailableSlots.HasValue)
            notes.Add($"Available slots: {result.Schedule.AvailableSlots.Value}.");

        if (result.Package is not null)
            notes.Add($"Package: {result.Package.Name}.");

        if (result.IsWaitlist || (result.Schedule.AllowWaitlist == true && result.Schedule.AvailableSlots.HasValue && result.Schedule.AvailableSlots.Value < result.TotalPax))
            notes.Add("Current request may be processed as waitlist.");

        if (request.Passengers.Any(x => !x.IsIncludedTax))
            notes.Add("Some passenger prices exclude taxes.");

        if (request.Passengers.Any(x => !x.IsIncludedFee))
            notes.Add("Some passenger prices exclude fees.");

        if (request.PackageLines.Any(x => x.PricingMode == TourPackagePricingMode.Included))
            notes.Add("Some package services are included in the base tour price.");

        if (request.PackageLines.Any(x => x.SelectedByOptimization))
            notes.Add("Some package services were auto-selected by dynamic package optimization.");

        foreach (var extra in request.AdditionalNotes.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var normalized = extra.Trim();
            if (!notes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                notes.Add(normalized);
        }

        return notes;
    }

    private static string? BuildAddonNote(TourQuoteBuildAddonInput addon)
    {
        if (addon.IsRequired)
            return "Required addon";

        if (addon.IsDefaultSelected)
            return "Default selected addon";

        return TrimOrNull(addon.Note);
    }

    private static void ValidateCurrency(string? lineCurrency, string expectedCurrency, string source)
    {
        var normalized = NormalizeCurrency(lineCurrency, expectedCurrency);
        if (!string.Equals(normalized, expectedCurrency, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Mixed currencies are not supported. Source '{source}' uses '{normalized}', expected '{expectedCurrency}'.");
    }

    private static string NormalizeCurrency(string? preferred, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CurrencyCode is required.");

        return value.Trim().ToUpperInvariant();
    }

    private static decimal EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
            throw new ArgumentException($"{fieldName} cannot be negative.");

        return value;
    }

    private static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class TourQuoteBuildRequest
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public string TourCurrencyCode { get; set; } = "";
    public bool IsWaitlist { get; set; }
    public TourQuoteBuildPackageRequest? Package { get; set; }
    public TourQuoteBuildScheduleRequest Schedule { get; set; } = new();
    public List<TourQuoteBuildPassengerInput> Passengers { get; set; } = new();
    public List<TourQuoteBuildAddonInput> Addons { get; set; } = new();
    public List<string> AdditionalNotes { get; set; } = new();
    public List<TourQuoteBuildPackageLineInput> PackageLines { get; set; } = new();
}

public sealed class TourQuoteBuildScheduleRequest
{
    public Guid ScheduleId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public int? AvailableSlots { get; set; }
    public bool? AllowWaitlist { get; set; }
    public string CurrencyCode { get; set; } = "";
}

public sealed class TourQuoteBuildPassengerInput
{
    public TourPriceType PriceType { get; set; }
    public string? DisplayName { get; set; }
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public decimal? UnitTaxes { get; set; }
    public decimal? UnitFees { get; set; }
    public bool IsIncludedTax { get; set; } = true;
    public bool IsIncludedFee { get; set; } = true;
    public string? Label { get; set; }
}

public sealed class TourQuoteBuildAddonInput
{
    public Guid AddonId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDefaultSelected { get; set; }
    public string? Note { get; set; }
}

public sealed class TourQuoteBuildPackageRequest
{
    public Guid PackageId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TourPackageMode Mode { get; set; }
}

public sealed class TourQuoteBuildPackageLineInput
{
    public Guid ComponentId { get; set; }
    public string ComponentCode { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public TourPackageComponentType ComponentType { get; set; }
    public Guid OptionId { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public TourPackagePricingMode PricingMode { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDefaultSelected { get; set; }
    public bool SelectedByOptimization { get; set; }
    public string? OptimizationStrategy { get; set; }
    public decimal? OptimizationScore { get; set; }
    public string? SelectionReason { get; set; }
    public string? Note { get; set; }
}

public sealed class TourQuoteBuildResult
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public int TotalPax { get; set; }
    public TourQuoteBuildPackageResult? Package { get; set; }
    public TourQuoteBuildScheduleResult Schedule { get; set; } = new();
    public List<TourQuoteBuildLine> PassengerLines { get; set; } = new();
    public List<TourQuoteBuildLine> AddonLines { get; set; } = new();
    public List<TourQuoteBuildPackageLine> PackageLines { get; set; } = new();
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OriginalTotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsWaitlist { get; set; }
    public List<string> Notes { get; set; } = new();
}

public sealed class TourQuoteBuildPackageResult
{
    public Guid PackageId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageMode Mode { get; set; }
}

public sealed class TourQuoteBuildScheduleResult
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
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public int? AvailableSlots { get; set; }
    public bool? AllowWaitlist { get; set; }
}

public sealed class TourQuoteBuildLine
{
    public string Kind { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public decimal LineBaseAmount { get; set; }
    public decimal LineOriginalAmount { get; set; }
    public decimal LineTaxAmount { get; set; }
    public decimal LineFeeAmount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string? Notes { get; set; }
}

public sealed class TourQuoteBuildPackageLine
{
    public Guid ComponentId { get; set; }
    public string ComponentCode { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public TourPackageComponentType ComponentType { get; set; }
    public Guid OptionId { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public string Kind { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public decimal LineBaseAmount { get; set; }
    public decimal LineOriginalAmount { get; set; }
    public decimal LineTaxAmount { get; set; }
    public decimal LineFeeAmount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public TourPackagePricingMode PricingMode { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDefaultSelected { get; set; }
    public bool SelectedByOptimization { get; set; }
    public string? OptimizationStrategy { get; set; }
    public decimal? OptimizationScore { get; set; }
    public string? SelectionReason { get; set; }
    public string? Notes { get; set; }
}

