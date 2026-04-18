using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPricingResolver
{
    public static TourSchedulePrice? ResolveDisplayPrice(
        IEnumerable<TourSchedulePrice> prices,
        TourPriceType priceType)
    {
        ArgumentNullException.ThrowIfNull(prices);

        return prices
            .Where(x => IsUsable(x) && x.PriceType == priceType)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Price)
            .ThenBy(x => x.MinQuantity ?? int.MaxValue)
            .ThenBy(x => x.MaxQuantity ?? int.MaxValue)
            .FirstOrDefault();
    }

    public static TourSchedulePrice? ResolvePriceForQuantity(
        IEnumerable<TourSchedulePrice> prices,
        TourPriceType priceType,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(prices);

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0.", nameof(quantity));

        return prices
            .Where(x => IsUsable(x) && x.PriceType == priceType && MatchesQuantity(x, quantity))
            .OrderByDescending(HasQuantityBounds)
            .ThenByDescending(x => x.MinQuantity ?? 0)
            .ThenBy(x => x.MaxQuantity ?? int.MaxValue)
            .ThenByDescending(x => x.IsDefault)
            .ThenBy(x => x.Price)
            .FirstOrDefault();
    }

    public static bool HasQuantityOverlap(
        TourSchedulePrice current,
        TourSchedulePrice other)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(other);

        return QuantityRangesOverlap(
            current.MinQuantity,
            current.MaxQuantity,
            other.MinQuantity,
            other.MaxQuantity);
    }

    public static bool QuantityRangesOverlap(
        int? minQuantityA,
        int? maxQuantityA,
        int? minQuantityB,
        int? maxQuantityB)
    {
        var minA = minQuantityA ?? 1;
        var maxA = maxQuantityA ?? int.MaxValue;
        var minB = minQuantityB ?? 1;
        var maxB = maxQuantityB ?? int.MaxValue;

        return minA <= maxB && minB <= maxA;
    }

    public static bool MatchesQuantity(TourSchedulePrice price, int quantity)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (quantity <= 0)
            return false;

        if (price.MinQuantity.HasValue && quantity < price.MinQuantity.Value)
            return false;

        if (price.MaxQuantity.HasValue && quantity > price.MaxQuantity.Value)
            return false;

        return true;
    }

    private static bool HasQuantityBounds(TourSchedulePrice price)
        => price.MinQuantity.HasValue || price.MaxQuantity.HasValue;

    private static bool IsUsable(TourSchedulePrice price)
        => price.IsActive && !price.IsDeleted;
}
