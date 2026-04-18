using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourQuoteBuilderTests
{
    private readonly TourQuoteBuilder _builder = new();

    [Fact]
    public void Build_IncludesPackageLinesInGrandTotal()
    {
        var result = _builder.Build(new TourQuoteBuildRequest
        {
            TourId = Guid.NewGuid(),
            TourName = "Da Nang Combo",
            TourCurrencyCode = "VND",
            Schedule = new TourQuoteBuildScheduleRequest
            {
                ScheduleId = Guid.NewGuid(),
                Code = "SCH-001",
                DepartureDate = new DateOnly(2026, 4, 10),
                ReturnDate = new DateOnly(2026, 4, 12),
                CurrencyCode = "VND"
            },
            Passengers = new List<TourQuoteBuildPassengerInput>
            {
                new()
                {
                    PriceType = TourPriceType.Adult,
                    DisplayName = "Adult",
                    Quantity = 2,
                    CurrencyCode = "VND",
                    UnitPrice = 100m
                }
            },
            Package = new TourQuoteBuildPackageRequest
            {
                PackageId = Guid.NewGuid(),
                Code = "PKG-STD",
                Name = "Standard Package",
                Mode = TourPackageMode.Configurable
            },
            PackageLines = new List<TourQuoteBuildPackageLineInput>
            {
                new()
                {
                    ComponentId = Guid.NewGuid(),
                    ComponentCode = "HOTEL",
                    ComponentName = "Hotel",
                    ComponentType = TourPackageComponentType.Accommodation,
                    OptionId = Guid.NewGuid(),
                    Code = "HOTEL-3S",
                    Name = "Hotel 3*",
                    Quantity = 1,
                    CurrencyCode = "VND",
                    UnitPrice = 250m,
                    PricingMode = TourPackagePricingMode.Override,
                    IsRequired = true
                }
            }
        });

        Assert.NotNull(result.Package);
        Assert.Single(result.PackageLines);
        Assert.Equal(450m, result.SubtotalAmount);
        Assert.Equal(450m, result.TotalAmount);
    }
}
