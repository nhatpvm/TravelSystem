//FILE: TicketBooking.Api/Services/Flight/IFlightOfferAncillaryQueryService.cs
using TicketBooking.Api.Contracts.Flight;

namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Public/customer read-only ancillary query service for Flight.
/// Scope:
/// - resolve ancillaries available for a specific offer
/// - optionally filter by ancillary type
/// - return airline / flight / fare-class context for booking UI
/// </summary>
public interface IFlightOfferAncillaryQueryService
{
    /// <summary>
    /// Get ancillary options for a specific flight offer.
    /// Returns null when:
    /// - offer does not exist
    /// - offer is expired and IncludeExpired = false
    /// - offer is not accessible in the resolved tenant context
    /// </summary>
    Task<FlightOfferAncillaryResponse?> GetByOfferAsync(
        FlightOfferAncillaryRequest request,
        CancellationToken ct = default);
}
