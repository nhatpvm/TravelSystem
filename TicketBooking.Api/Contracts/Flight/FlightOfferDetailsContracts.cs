
//FILE: TicketBooking.Api / Contracts / Flight / FlightOfferDetailsContracts.cs
namespace TicketBooking.Api.Contracts.Flight;

/// <summary>
/// Public offer details query.
/// Example:
/// GET /api/v1/flight/offers/{offerId}
/// GET /api/v1/flight/offers/{offerId}?includeExpired=true
/// </summary>
public sealed class FlightOfferDetailsRequest
{
    public Guid OfferId { get; set; }
    public bool IncludeExpired { get; set; }
}

public sealed class FlightOfferDetailsResponse
{
    public FlightOfferDetailsOfferDto Offer { get; set; } = new();
    public FlightOfferDetailsAirlineDto Airline { get; set; } = new();
    public FlightOfferDetailsFlightDto Flight { get; set; } = new();
    public FlightOfferDetailsFareClassDto FareClass { get; set; } = new();

    /// <summary>
    /// May be null when no fare rule document exists for the fare class.
    /// </summary>
    public FlightOfferDetailsFareRuleDto? FareRule { get; set; }

    public List<FlightOfferDetailsSegmentDto> Segments { get; set; } = new();
    public List<FlightOfferDetailsTaxFeeLineDto> TaxFeeLines { get; set; } = new();
}

public sealed class FlightOfferDetailsOfferDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "VND";

    public decimal BaseFare { get; set; }
    public decimal TaxesFees { get; set; }
    public decimal TotalPrice { get; set; }

    public int SeatsAvailable { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public string? ConditionsJson { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class FlightOfferDetailsAirlineDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
}

public sealed class FlightOfferDetailsFlightDto
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public DateTimeOffset DepartureAt { get; set; }
    public DateTimeOffset ArrivalAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class FlightOfferDetailsFareClassDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CabinClass { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public bool IsChangeable { get; set; }
}

public sealed class FlightOfferDetailsFareRuleDto
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public string RulesJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class FlightOfferDetailsSegmentDto
{
    public int SegmentIndex { get; set; }
    public FlightOfferDetailsAirportDto From { get; set; } = new();
    public FlightOfferDetailsAirportDto To { get; set; } = new();
    public string? FlightNumber { get; set; }
    public DateTimeOffset DepartureAt { get; set; }
    public DateTimeOffset ArrivalAt { get; set; }
}

public sealed class FlightOfferDetailsAirportDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public string? TimeZone { get; set; }
}

public sealed class FlightOfferDetailsTaxFeeLineDto
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public decimal Amount { get; set; }
}
