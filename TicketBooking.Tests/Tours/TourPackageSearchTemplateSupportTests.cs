using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageSearchTemplateSupportTests
{
    [Fact]
    public void ResolveServiceDate_UsesReturnAnchorAndDayOffset()
    {
        var schedule = new TourSchedule
        {
            Id = Guid.NewGuid(),
            TourId = Guid.NewGuid(),
            Code = "SCH-001",
            DepartureDate = new DateOnly(2026, 4, 10),
            ReturnDate = new DateOnly(2026, 4, 12)
        };

        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TourPackageId = Guid.NewGuid(),
            Code = "RETURN",
            Name = "Return",
            ComponentType = TourPackageComponentType.ReturnTransport,
            DayOffsetFromDeparture = 1
        };

        var template = new TourPackageSearchTemplate
        {
            DateAnchor = TourPackageSearchDateAnchor.ReturnDate,
            DayOffset = 2
        };

        var resolved = TourPackageSearchTemplateSupport.ResolveServiceDate(schedule, component, template);
        Assert.Equal(new DateOnly(2026, 4, 14), resolved);
    }

    [Fact]
    public void ParseRequired_ParsesSearchTemplateJson()
    {
        var template = TourPackageSearchTemplateSupport.ParseRequired("""
            {
              "tenantId": "11111111-1111-1111-1111-111111111111",
              "fromLocationId": "22222222-2222-2222-2222-222222222222",
              "toLocationId": "33333333-3333-3333-3333-333333333333",
              "selectionStrategy": "Recommended",
              "nightCount": 2,
              "maxUnitPrice": 1500000,
              "preferDirect": true,
              "preferredDepartureHourFrom": 7,
              "preferredDepartureHourTo": 11
            }
            """);

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), template.TenantId);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), template.FromLocationId);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), template.ToLocationId);
        Assert.Equal(TourPackageSourceSelectionStrategy.Recommended, template.SelectionStrategy);
        Assert.Equal(2, template.NightCount);
        Assert.Equal(1500000m, template.MaxUnitPrice);
        Assert.True(template.PreferDirect);
        Assert.Equal(7, template.PreferredDepartureHourFrom);
        Assert.Equal(11, template.PreferredDepartureHourTo);
    }
}
