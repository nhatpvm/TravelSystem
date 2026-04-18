using System.Text.Json;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPackageCancellationSupport
{
    public static TourPackageBookingStatus CalculateBookingStatus(IReadOnlyCollection<TourPackageBookingItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var activeCount = items.Count(IsBookingItemActiveAfterSales);
        var cancelledCount = items.Count(IsBookingItemCancelledAfterSales);
        var failedCount = items.Count(x => x.Status == TourPackageBookingItemStatus.Failed);
        var confirmedCount = items.Count(x => x.Status == TourPackageBookingItemStatus.Confirmed);

        if (activeCount == 0 && cancelledCount > 0)
            return TourPackageBookingStatus.Cancelled;

        if (activeCount > 0 && cancelledCount > 0)
            return TourPackageBookingStatus.PartiallyCancelled;

        if (confirmedCount > 0 && failedCount > 0)
            return TourPackageBookingStatus.PartiallyConfirmed;

        if (confirmedCount > 0)
            return TourPackageBookingStatus.Confirmed;

        return failedCount == items.Count
            ? TourPackageBookingStatus.Failed
            : TourPackageBookingStatus.Pending;
    }

    public static bool IsBookingItemCancellable(TourPackageBookingItemStatus status)
        => status == TourPackageBookingItemStatus.Confirmed;

    public static bool IsBookingItemCancelledAfterSales(TourPackageBookingItem item)
        => item.Status is TourPackageBookingItemStatus.Cancelled
            or TourPackageBookingItemStatus.RefundPending
            or TourPackageBookingItemStatus.Refunded
            or TourPackageBookingItemStatus.RefundRejected;

    public static bool IsBookingItemActiveAfterSales(TourPackageBookingItem item)
        => item.Status is TourPackageBookingItemStatus.Pending
            or TourPackageBookingItemStatus.Confirmed
            or TourPackageBookingItemStatus.CancellationPending;

    public static bool IsOptionalSelectionMode(TourPackageSelectionMode selectionMode)
        => selectionMode is TourPackageSelectionMode.OptionalSingle or TourPackageSelectionMode.OptionalMulti;

    public static TourPackageCancellationMatchedRule ResolveRule(string? policyJson, DateOnly departureDate, DateTimeOffset now)
    {
        var daysBeforeDeparture = departureDate.DayNumber - DateOnly.FromDateTime(now.Date).DayNumber;
        var rules = ParseRules(policyJson);

        foreach (var rule in rules)
        {
            if (rule.SameDay == true)
            {
                if (daysBeforeDeparture <= 0)
                    return rule with { Summary = "Same-day cancellation rule matched." };

                continue;
            }

            if (rule.FromDay.HasValue && daysBeforeDeparture < rule.FromDay.Value)
                continue;

            if (rule.ToDay.HasValue && daysBeforeDeparture > rule.ToDay.Value)
                continue;

            return rule with
            {
                Summary = rule.FromDay.HasValue || rule.ToDay.HasValue
                    ? $"Matched cancellation window for {daysBeforeDeparture} day(s) before departure."
                    : "Matched default cancellation rule."
            };
        }

        return new TourPackageCancellationMatchedRule
        {
            FeePercent = 100m,
            Summary = "No machine-readable cancellation rule matched; defaulted to non-refundable cancellation."
        };
    }

    public static TourPackageCancellationAmountBreakdown CalculateAmounts(
        decimal grossLineAmount,
        TourPackageCancellationMatchedRule rule)
    {
        if (grossLineAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(grossLineAmount));

        ArgumentNullException.ThrowIfNull(rule);

        var penalty = rule.FeeAmount.HasValue
            ? rule.FeeAmount.Value
            : decimal.Round(grossLineAmount * (rule.FeePercent / 100m), 2, MidpointRounding.AwayFromZero);

        penalty = Math.Clamp(penalty, 0m, grossLineAmount);

        return new TourPackageCancellationAmountBreakdown
        {
            GrossLineAmount = grossLineAmount,
            PenaltyAmount = penalty,
            RefundAmount = grossLineAmount - penalty
        };
    }

    private static List<TourPackageCancellationMatchedRule> ParseRules(string? policyJson)
    {
        if (string.IsNullOrWhiteSpace(policyJson))
            return new List<TourPackageCancellationMatchedRule>();

        try
        {
            using var document = JsonDocument.Parse(policyJson);
            if (!document.RootElement.TryGetProperty("rules", out var rulesElement) ||
                rulesElement.ValueKind != JsonValueKind.Array)
            {
                return new List<TourPackageCancellationMatchedRule>();
            }

            var results = new List<TourPackageCancellationMatchedRule>();
            foreach (var item in rulesElement.EnumerateArray())
            {
                var rawJson = item.GetRawText();
                results.Add(new TourPackageCancellationMatchedRule
                {
                    FromDay = TryGetInt32(item, "fromDay"),
                    ToDay = TryGetInt32(item, "toDay"),
                    SameDay = TryGetBoolean(item, "sameDay"),
                    FeePercent = TryGetDecimal(item, "feePercent") ?? 100m,
                    FeeAmount = TryGetDecimal(item, "feeAmount"),
                    RawJson = rawJson
                });
            }

            return results;
        }
        catch (JsonException)
        {
            return new List<TourPackageCancellationMatchedRule>();
        }
    }

    private static int? TryGetInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static bool? TryGetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static decimal? TryGetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }
}

public sealed record TourPackageCancellationMatchedRule
{
    public int? FromDay { get; init; }
    public int? ToDay { get; init; }
    public bool? SameDay { get; init; }
    public decimal FeePercent { get; init; }
    public decimal? FeeAmount { get; init; }
    public string? RawJson { get; init; }
    public string Summary { get; init; } = "";
}

public sealed class TourPackageCancellationAmountBreakdown
{
    public decimal GrossLineAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
}

public interface ITourPackageSourceCancellationAdapter
{
    bool CanHandle(TourPackageSourceType sourceType);

    Task<TourPackageSourceCancellationResult> CancelAsync(
        TourPackageSourceCancellationRequest request,
        CancellationToken ct = default);
}

public sealed class TourPackageSourceCancellationRequest
{
    public Guid? UserId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTimeOffset CurrentTime { get; set; }
    public Tour Tour { get; set; } = null!;
    public TourSchedule Schedule { get; set; } = null!;
    public TourPackageBooking Booking { get; set; } = null!;
    public TourPackageBookingItem BookingItem { get; set; } = null!;
}

public sealed class TourPackageSourceCancellationResult
{
    public TourPackageSourceCancellationOutcomeStatus Status { get; set; }
    public string? SnapshotJson { get; set; }
    public string? Note { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TourPackageSourceCancellationOutcomeStatus
{
    Cancelled = 1,
    Rejected = 2
}
