//FILE: TicketBooking.Api/Contracts/Flight/FlightOfferAncillaryContracts.cs
using TicketBooking.Domain.Flight;

namespace TicketBooking.Api.Contracts.Flight;

/// <summary>
/// Public offer ancillary query.
/// Example:
/// GET /api/v1/flight/offers/{offerId}/ancillaries
/// GET /api/v1/flight/offers/{offerId}/ancillaries?type=Meal
/// GET /api/v1/flight/offers/{offerId}/ancillaries?includeInactive=true&includeExpired=true
/// </summary>
public sealed class FlightOfferAncillaryRequest
{
    public Guid OfferId { get; set; }

    /// <summary>
    /// Optional filter by ancillary type.
    /// </summary>
    public AncillaryType? Type { get; set; }

    /// <summary>
    /// Include inactive ancillary definitions.
    /// Default = true so QLVMM/admin-like inspection and public debug are easier during build-out.
    /// </summary>
    public bool IncludeInactive { get; set; } = true;

    /// <summary>
    /// Include expired offer.
    /// Default = false for customer/public safety.
    /// </summary>
    public bool IncludeExpired { get; set; } = false;
}

public sealed class FlightOfferAncillaryResponse
{
    public FlightOfferAncillaryOfferDto Offer { get; set; } = new();
    public FlightOfferAncillaryAirlineDto Airline { get; set; } = new();
    public FlightOfferAncillaryFlightDto Flight { get; set; } = new();
    public FlightOfferAncillaryFareClassDto FareClass { get; set; } = new();
    public FlightOfferAncillarySummaryDto Summary { get; set; } = new();
    public List<FlightOfferAncillaryItemDto> Items { get; set; } = new();
}

public sealed class FlightOfferAncillaryOfferDto
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

    public bool IsExpired { get; set; }

    public string? ConditionsJson { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class FlightOfferAncillaryAirlineDto
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

public sealed class FlightOfferAncillaryFlightDto
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public DateTimeOffset DepartureAt { get; set; }
    public DateTimeOffset ArrivalAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public FlightOfferAncillaryAirportDto From { get; set; } = new();
    public FlightOfferAncillaryAirportDto To { get; set; } = new();
}

public sealed class FlightOfferAncillaryAirportDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public string? TimeZone { get; set; }
}

public sealed class FlightOfferAncillaryFareClassDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CabinClass { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public bool IsChangeable { get; set; }
}

public sealed class FlightOfferAncillarySummaryDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public List<FlightOfferAncillaryGroupSummaryDto> Groups { get; set; } = new();
}

public sealed class FlightOfferAncillaryGroupSummaryDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public int ActiveCount { get; set; }
}

public sealed class FlightOfferAncillaryItemDto
{
    public Guid Id { get; set; }
    public Guid AirlineId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "VND";
    public decimal Price { get; set; }

    public string? RulesJson { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
