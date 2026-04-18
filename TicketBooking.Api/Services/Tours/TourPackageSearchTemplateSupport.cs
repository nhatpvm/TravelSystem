using System.Text.Json;
using System.Text.Json.Serialization;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPackageSearchTemplateSupport
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static TourPackageSearchTemplate ParseRequired(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("SearchTemplateJson is required for SearchTemplate binding.");

        try
        {
            var template = JsonSerializer.Deserialize<TourPackageSearchTemplate>(json, JsonOptions);
            if (template is null)
                throw new ArgumentException("SearchTemplateJson could not be parsed.");

            return template;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"SearchTemplateJson is invalid: {ex.Message}", ex);
        }
    }

    public static DateOnly ResolveServiceDate(
        TourSchedule schedule,
        TourPackageComponent component,
        TourPackageSearchTemplate template)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(template);

        var anchor = template.DateAnchor ?? ResolveDefaultDateAnchor(component.ComponentType);
        var baseDate = anchor switch
        {
            TourPackageSearchDateAnchor.DepartureDate => schedule.DepartureDate,
            TourPackageSearchDateAnchor.ReturnDate => schedule.ReturnDate,
            _ => schedule.DepartureDate
        };

        var dayOffset = template.DayOffset ?? component.DayOffsetFromDeparture ?? 0;
        return baseDate.AddDays(dayOffset);
    }

    public static int ResolveNightCount(
        TourPackageSourceQuoteAdapterRequest request,
        TourPackageSearchTemplate template)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(template);

        if (template.NightCount.HasValue && template.NightCount.Value > 0)
            return template.NightCount.Value;

        if (request.Component.NightCount.HasValue && request.Component.NightCount.Value > 0)
            return request.Component.NightCount.Value;

        return Math.Max(request.TotalNights, 1);
    }

    public static int ResolveRoomCount(
        TourPackageSourceQuoteAdapterRequest request,
        TourPackageSearchTemplate template)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(template);

        if (template.RoomCount.HasValue && template.RoomCount.Value > 0)
            return template.RoomCount.Value;

        if (request.Option.QuantityMode == TourPackageQuantityMode.PerRoom)
            return Math.Max(request.RequestedQuantity, 1);

        return 1;
    }

    public static TourPackageSourceSelectionStrategy ResolveStrategy(TourPackageSearchTemplate template)
        => template.SelectionStrategy ?? TourPackageSourceSelectionStrategy.Cheapest;

    private static TourPackageSearchDateAnchor ResolveDefaultDateAnchor(TourPackageComponentType componentType)
        => componentType == TourPackageComponentType.ReturnTransport
            ? TourPackageSearchDateAnchor.ReturnDate
            : TourPackageSearchDateAnchor.DepartureDate;
}

public sealed class TourPackageSearchTemplate
{
    public Guid? TenantId { get; set; }
    public Guid? ProviderId { get; set; }
    public TourPackageSourceSelectionStrategy? SelectionStrategy { get; set; }
    public TourPackageSearchDateAnchor? DateAnchor { get; set; }
    public int? DayOffset { get; set; }

    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }

    public Guid? FromAirportId { get; set; }
    public string? FromAirportCode { get; set; }
    public Guid? ToAirportId { get; set; }
    public string? ToAirportCode { get; set; }
    public Guid? AirlineId { get; set; }
    public CabinClass? CabinClass { get; set; }

    public Guid? TripId { get; set; }
    public string? TripCode { get; set; }

    public Guid? HotelId { get; set; }
    public Guid? RoomTypeId { get; set; }
    public Guid? RatePlanId { get; set; }
    public Guid? RatePlanRoomTypeId { get; set; }
    public int? NightCount { get; set; }
    public int? RoomCount { get; set; }
    public decimal? MaxUnitPrice { get; set; }
    public int? MaxStops { get; set; }
    public int? PreferredDepartureHourFrom { get; set; }
    public int? PreferredDepartureHourTo { get; set; }
    public int? PreferredArrivalHourFrom { get; set; }
    public int? PreferredArrivalHourTo { get; set; }
    public bool? PreferDirect { get; set; }
    public bool? PreferRefundable { get; set; }
    public bool? PreferBreakfastIncluded { get; set; }
    public bool? RefundableOnly { get; set; }
    public bool? BreakfastIncluded { get; set; }
}

public enum TourPackageSearchDateAnchor
{
    DepartureDate = 1,
    ReturnDate = 2
}

public enum TourPackageSourceSelectionStrategy
{
    Cheapest = 1,
    Earliest = 2,
    BestValue = 3,
    Recommended = 4,
    ShortestDuration = 5
}
