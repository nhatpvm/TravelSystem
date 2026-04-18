using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPackageQuoteSupport
{
    public static int ResolveQuantity(
        TourPackageComponentOption option,
        int totalPax,
        int totalNights,
        int? requestedQuantity)
    {
        ArgumentNullException.ThrowIfNull(option);

        var quantity = requestedQuantity ?? option.QuantityMode switch
        {
            TourPackageQuantityMode.PerBooking => option.DefaultQuantity,
            TourPackageQuantityMode.PerPax => option.DefaultQuantity * totalPax,
            TourPackageQuantityMode.PerNight => option.DefaultQuantity * Math.Max(totalNights, 1),
            TourPackageQuantityMode.PerRoom => option.DefaultQuantity,
            TourPackageQuantityMode.Custom => option.DefaultQuantity,
            _ => option.DefaultQuantity
        };

        if (quantity <= 0)
            throw new ArgumentException($"Package option '{option.Name}' resolved to an invalid quantity.");

        if (option.MinQuantity.HasValue && quantity < option.MinQuantity.Value)
            throw new ArgumentException($"Package option '{option.Name}' requires at least {option.MinQuantity.Value}.");

        if (option.MaxQuantity.HasValue && quantity > option.MaxQuantity.Value)
            throw new ArgumentException($"Package option '{option.Name}' exceeds MaxQuantity ({option.MaxQuantity.Value}).");

        return quantity;
    }

    public static TourPackageScheduleOptionOverride? ResolveScheduleOverride(
        TourPackageComponentOption option,
        Guid scheduleId)
    {
        ArgumentNullException.ThrowIfNull(option);

        return option.ScheduleOverrides
            .Where(x => x.TourScheduleId == scheduleId && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefault();
    }

    public static string NormalizeCurrency(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate.Trim().ToUpperInvariant();
        }

        throw new ArgumentException("CurrencyCode is required.");
    }

    public static bool IsUsable(TourPackageComponent component)
        => component.IsActive && !component.IsDeleted;

    public static bool IsUsable(TourPackageComponentOption option)
        => option.IsActive && !option.IsDeleted;
}
