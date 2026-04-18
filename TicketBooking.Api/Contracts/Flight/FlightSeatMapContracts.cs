
//FILE: TicketBooking.Api / Contracts / Flight / FlightSeatMapContracts.cs
namespace TicketBooking.Api.Contracts.Flight;

/// <summary>
/// Public seat-map query by offer.
/// Example:
/// GET /api/v1/flight/offers/{offerId}/seat-map
/// </summary>
public sealed class FlightSeatMapByOfferRequest
{
    public Guid OfferId { get; set; }
}

/// <summary>
/// Public seat-map query by cabin seat-map id.
/// Example:
/// GET /api/v1/flight/cabin-seat-maps/{cabinSeatMapId}
/// </summary>
public sealed class FlightSeatMapByIdRequest
{
    public Guid CabinSeatMapId { get; set; }
}

public sealed class FlightSeatMapResponse
{
    /// <summary>
    /// Present when the seat-map is resolved from an offer flow.
    /// Null when queried directly by CabinSeatMapId.
    /// </summary>
    public Guid? OfferId { get; set; }

    public Guid TenantId { get; set; }

    public bool UsesPooledInventory { get; set; }

    public int? SeatsAvailable { get; set; }

    public int ActiveSeatCount { get; set; }

    public string? InventoryNote { get; set; }

    public FlightSeatMapDto CabinSeatMap { get; set; } = new();

    public List<FlightSeatMapSegmentDto> Segments { get; set; } = new();

    public List<FlightSegmentSeatMapDto> SegmentSeatMaps { get; set; } = new();

    public List<FlightSeatDto> Seats { get; set; } = new();
}

public sealed class FlightSeatMapDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? CabinClass { get; set; }

    public int TotalRows { get; set; }

    public int TotalColumns { get; set; }

    public int DeckCount { get; set; } = 1;

    public bool IsActive { get; set; }
}

public sealed class FlightSeatDto
{
    public Guid Id { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public int RowIndex { get; set; }

    public int ColumnIndex { get; set; }

    public int DeckIndex { get; set; } = 1;

    public string? SeatType { get; set; }

    public string? SeatClass { get; set; }

    public bool IsWindow { get; set; }

    public bool IsAisle { get; set; }

    public decimal? PriceModifier { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Customer-facing seat status.
    /// Current baseline:
    /// - available
    /// - inactive
    /// Future flight-hold logic can extend this to:
    /// - held
    /// - held_by_me
    /// - booked
    /// </summary>
    public string Status { get; set; } = "available";

    public string? HoldToken { get; set; }

    public DateTimeOffset? HoldExpiresAt { get; set; }
}

public sealed class FlightSeatMapSegmentDto
{
    public int SegmentIndex { get; set; }

    public Guid? FlightId { get; set; }

    public Guid? CabinSeatMapId { get; set; }

    public string? FlightNumber { get; set; }

    public DateTimeOffset DepartureAt { get; set; }

    public DateTimeOffset ArrivalAt { get; set; }

    public bool IsPrimarySelectedMap { get; set; }

    public FlightAirportLiteDto From { get; set; } = new();

    public FlightAirportLiteDto To { get; set; } = new();
}

public sealed class FlightSegmentSeatMapDto
{
    public int SegmentIndex { get; set; }

    public Guid? FlightId { get; set; }

    public Guid? CabinSeatMapId { get; set; }

    public string? FlightNumber { get; set; }

    public DateTimeOffset DepartureAt { get; set; }

    public DateTimeOffset ArrivalAt { get; set; }

    public bool IsPrimarySelectedMap { get; set; }

    public int ActiveSeatCount { get; set; }

    public FlightAirportLiteDto From { get; set; } = new();

    public FlightAirportLiteDto To { get; set; } = new();

    public FlightSeatMapDto? CabinSeatMap { get; set; }

    public List<FlightSeatDto> Seats { get; set; } = new();
}
