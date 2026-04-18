using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPricingResolverTests
{
    [Fact]
    public void ResolvePriceForQuantity_PrefersMostSpecificMatchingTier()
    {
        var prices = new[]
        {
            CreatePrice(120m, isDefault: true),
            CreatePrice(95m, minQuantity: 5, maxQuantity: 10)
        };

        var resolved = TourPricingResolver.ResolvePriceForQuantity(prices, TourPriceType.Adult, quantity: 6);

        Assert.NotNull(resolved);
        Assert.Equal(95m, resolved!.Price);
        Assert.Equal(5, resolved.MinQuantity);
        Assert.Equal(10, resolved.MaxQuantity);
    }

    [Fact]
    public void ResolvePriceForQuantity_FallsBackToDefaultPriceWhenNoTierMatches()
    {
        var prices = new[]
        {
            CreatePrice(120m, isDefault: true),
            CreatePrice(95m, minQuantity: 5, maxQuantity: 10)
        };

        var resolved = TourPricingResolver.ResolvePriceForQuantity(prices, TourPriceType.Adult, quantity: 2);

        Assert.NotNull(resolved);
        Assert.Equal(120m, resolved!.Price);
        Assert.True(resolved.IsDefault);
    }

    [Theory]
    [InlineData(null, 5, 5, null, true)]
    [InlineData(1, 4, 5, 8, false)]
    [InlineData(3, null, 8, 10, true)]
    public void QuantityRangesOverlap_HandlesOpenEndedBands(
        int? minA,
        int? maxA,
        int? minB,
        int? maxB,
        bool expected)
    {
        var overlaps = TourPricingResolver.QuantityRangesOverlap(minA, maxA, minB, maxB);

        Assert.Equal(expected, overlaps);
    }

    [Fact]
    public void ResolveDisplayPrice_PrefersDefaultDisplayBand()
    {
        var prices = new[]
        {
            CreatePrice(100m),
            CreatePrice(130m, isDefault: true)
        };

        var resolved = TourPricingResolver.ResolveDisplayPrice(prices, TourPriceType.Adult);

        Assert.NotNull(resolved);
        Assert.True(resolved!.IsDefault);
        Assert.Equal(130m, resolved.Price);
    }

    private static TourSchedulePrice CreatePrice(
        decimal price,
        bool isDefault = false,
        int? minQuantity = null,
        int? maxQuantity = null)
    {
        return new TourSchedulePrice
        {
            Id = Guid.NewGuid(),
            TourScheduleId = Guid.NewGuid(),
            PriceType = TourPriceType.Adult,
            CurrencyCode = "VND",
            Price = price,
            IsDefault = isDefault,
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity,
            IsActive = true,
            IsDeleted = false
        };
    }
}
