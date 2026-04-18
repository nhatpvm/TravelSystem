

//FILE: TicketBooking.Api / Contracts / Flight / FlightSearchContracts.cs
using System.ComponentModel.DataAnnotations;

namespace TicketBooking.Api.Contracts.Flight;

/// <summary>
/// Public flight search query.
/// Example:
/// GET /api/v1/search/flights?from=SGN&to=HAN&date=2026-03-03
/// </summary>
public sealed class FlightSearchRequest
{
    [Required]
    public string From { get; set; } = string.Empty;

    [Required]
    public string To { get; set; } = string.Empty;

    [Required]
    public DateOnly Date { get; set; }
}

/// <summary>
/// Public airport lookup query for dropdown/autocomplete.
/// Example:
/// GET /api/v1/search/flights/airports?q=SGN
/// </summary>
public sealed class FlightAirportLookupRequest
{
    public string? Q { get; set; }

    /// <summary>
    /// Max items returned. Service/controller should clamp this to a safe range.
    /// </summary>
    public int Limit { get; set; } = 50;
}

public sealed class FlightSearchResponse
{
    public int Count { get; set; }
    public List<FlightSearchItemDto> Items { get; set; } = new();
    public string? Message { get; set; }
}

public sealed class FlightSearchItemDto
{
    public Guid OfferId { get; set; }

    public string Status { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";

    public decimal BaseFare { get; set; }
    public decimal TaxesFees { get; set; }
    public decimal TotalPrice { get; set; }

    public int SeatsAvailable { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public FlightAirlineSummaryDto Airline { get; set; } = new();
    public FlightSummaryDto Flight { get; set; } = new();
    public FlightFareClassSummaryDto FareClass { get; set; } = new();

    public List<FlightOfferSegmentSummaryDto> Segments { get; set; } = new();
    public List<FlightOfferTaxFeeLineSummaryDto> TaxFeeLines { get; set; } = new();
}

public sealed class FlightAirlineSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public string? LogoUrl { get; set; }
}

public sealed class FlightSummaryDto
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public DateTimeOffset DepartureAt { get; set; }
    public DateTimeOffset ArrivalAt { get; set; }
}

public sealed class FlightFareClassSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CabinClass { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public bool IsChangeable { get; set; }
}

public sealed class FlightOfferSegmentSummaryDto
{
    public int SegmentIndex { get; set; }
    public FlightAirportLiteDto From { get; set; } = new();
    public FlightAirportLiteDto To { get; set; } = new();
    public string? FlightNumber { get; set; }
    public DateTimeOffset DepartureAt { get; set; }
    public DateTimeOffset ArrivalAt { get; set; }
}

public sealed class FlightOfferTaxFeeLineSummaryDto
{
    public int SortOrder { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public decimal Amount { get; set; }
}

public sealed class FlightAirportLiteDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public string? TimeZone { get; set; }
}

public sealed class FlightAirportLookupResponse
{
    public int Count { get; set; }
    public List<FlightAirportLookupItemDto> Items { get; set; } = new();
}

public sealed class FlightAirportLookupItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public string? TimeZone { get; set; }
}
