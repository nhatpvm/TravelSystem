// FILE #275: TicketBooking.Api/Services/Tours/TourPriceCalculator.cs
using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPriceCalculator
{
    public TourPriceCalculationResult Calculate(TourPriceCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.PassengerLines);
        ArgumentNullException.ThrowIfNull(request.AddonLines);

        var currencyCode = NormalizeCurrency(request.CurrencyCode);

        var result = new TourPriceCalculationResult
        {
            CurrencyCode = currencyCode
        };

        foreach (var passenger in request.PassengerLines)
        {
            var line = BuildPassengerLine(passenger, currencyCode);
            result.PassengerLines.Add(line);

            result.SubtotalAmount += line.LineBaseAmount;
            result.OriginalTotalAmount += line.LineOriginalAmount;
            result.TaxAmount += line.LineTaxAmount;
            result.FeeAmount += line.LineFeeAmount;
        }

        foreach (var addon in request.AddonLines)
        {
            var line = BuildAddonLine(addon, currencyCode);
            result.AddonLines.Add(line);

            result.SubtotalAmount += line.LineBaseAmount;
            result.OriginalTotalAmount += line.LineOriginalAmount;
            result.TaxAmount += line.LineTaxAmount;
            result.FeeAmount += line.LineFeeAmount;
        }

        result.TotalAmount = result.SubtotalAmount + result.TaxAmount + result.FeeAmount;
        result.DiscountAmount = result.OriginalTotalAmount > result.SubtotalAmount
            ? result.OriginalTotalAmount - result.SubtotalAmount
            : 0m;

        return result;
    }

    public TourPriceCalculatedLine BuildPassengerLine(
        TourPriceCalculationPassengerLineRequest request,
        string? expectedCurrency = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Quantity <= 0)
            throw new ArgumentException("Passenger Quantity must be greater than 0.", nameof(request));

        var currencyCode = NormalizeCurrency(expectedCurrency ?? request.CurrencyCode);

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            ValidateCurrency(request.CurrencyCode, currencyCode, $"passenger:{request.PriceType}");

        var unitPrice = EnsureNonNegative(request.UnitPrice, $"{request.PriceType}.UnitPrice");
        var unitOriginalPrice = request.UnitOriginalPrice.HasValue
            ? EnsureNonNegative(request.UnitOriginalPrice.Value, $"{request.PriceType}.UnitOriginalPrice")
            : (decimal?)null;

        var unitTaxes = request.UnitTaxes.HasValue
            ? EnsureNonNegative(request.UnitTaxes.Value, $"{request.PriceType}.UnitTaxes")
            : 0m;

        var unitFees = request.UnitFees.HasValue
            ? EnsureNonNegative(request.UnitFees.Value, $"{request.PriceType}.UnitFees")
            : 0m;

        return new TourPriceCalculatedLine
        {
            Kind = "passenger",
            ReferenceId = null,
            Code = request.PriceType.ToString(),
            Name = string.IsNullOrWhiteSpace(request.DisplayName)
                ? request.PriceType.ToString()
                : request.DisplayName.Trim(),
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            UnitOriginalPrice = unitOriginalPrice,
            LineBaseAmount = unitPrice * request.Quantity,
            LineOriginalAmount = (unitOriginalPrice ?? unitPrice) * request.Quantity,
            LineTaxAmount = request.IsIncludedTax ? 0m : unitTaxes * request.Quantity,
            LineFeeAmount = request.IsIncludedFee ? 0m : unitFees * request.Quantity,
            CurrencyCode = currencyCode,
            Notes = TrimOrNull(request.Label)
        };
    }

    public TourPriceCalculatedLine BuildAddonLine(
        TourPriceCalculationAddonLineRequest request,
        string? expectedCurrency = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AddonId == Guid.Empty)
            throw new ArgumentException("AddonId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Addon Code is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Addon Name is required.", nameof(request));

        if (request.Quantity <= 0)
            throw new ArgumentException("Addon Quantity must be greater than 0.", nameof(request));

        var currencyCode = NormalizeCurrency(expectedCurrency ?? request.CurrencyCode);

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            ValidateCurrency(request.CurrencyCode, currencyCode, $"addon:{request.Code}");

        var unitPrice = EnsureNonNegative(request.UnitPrice, $"{request.Code}.UnitPrice");
        var unitOriginalPrice = request.UnitOriginalPrice.HasValue
            ? EnsureNonNegative(request.UnitOriginalPrice.Value, $"{request.Code}.UnitOriginalPrice")
            : (decimal?)null;

        return new TourPriceCalculatedLine
        {
            Kind = "addon",
            ReferenceId = request.AddonId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            UnitOriginalPrice = unitOriginalPrice,
            LineBaseAmount = unitPrice * request.Quantity,
            LineOriginalAmount = (unitOriginalPrice ?? unitPrice) * request.Quantity,
            LineTaxAmount = 0m,
            LineFeeAmount = 0m,
            CurrencyCode = currencyCode,
            Notes = BuildAddonNote(request)
        };
    }

    private static string? BuildAddonNote(TourPriceCalculationAddonLineRequest request)
    {
        if (request.IsRequired)
            return "Required addon";

        if (request.IsDefaultSelected)
            return "Default selected addon";

        return TrimOrNull(request.Note);
    }

    private static void ValidateCurrency(string actualCurrency, string expectedCurrency, string source)
    {
        var normalizedActual = NormalizeCurrency(actualCurrency);

        if (!string.Equals(normalizedActual, expectedCurrency, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Mixed currencies are not supported. Source '{source}' uses '{normalizedActual}', expected '{expectedCurrency}'.");
        }
    }

    private static string NormalizeCurrency(string? value)
    {
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

public sealed class TourPriceCalculationRequest
{
    public string CurrencyCode { get; set; } = "VND";
    public List<TourPriceCalculationPassengerLineRequest> PassengerLines { get; set; } = new();
    public List<TourPriceCalculationAddonLineRequest> AddonLines { get; set; } = new();
}

public sealed class TourPriceCalculationPassengerLineRequest
{
    public TourPriceType PriceType { get; set; }
    public string? DisplayName { get; set; }
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public decimal? UnitTaxes { get; set; }
    public decimal? UnitFees { get; set; }
    public bool IsIncludedTax { get; set; } = true;
    public bool IsIncludedFee { get; set; } = true;
    public string? Label { get; set; }
}

public sealed class TourPriceCalculationAddonLineRequest
{
    public Guid AddonId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDefaultSelected { get; set; }
    public string? Note { get; set; }
}

public sealed class TourPriceCalculationResult
{
    public string CurrencyCode { get; set; } = "";
    public List<TourPriceCalculatedLine> PassengerLines { get; set; } = new();
    public List<TourPriceCalculatedLine> AddonLines { get; set; } = new();
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OriginalTotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
}

public sealed class TourPriceCalculatedLine
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

