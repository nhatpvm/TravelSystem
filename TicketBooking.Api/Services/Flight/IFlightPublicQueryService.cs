//FILE: TicketBooking.Api / Services / Flight / IFlightPublicQueryService.cs
using TicketBooking.Api.Contracts.Flight;

namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Customer/public read-only query service for Flight module.
/// Scope:
/// - public search
/// - airport lookup
/// - offer details
/// </summary>
public interface IFlightPublicQueryService
{
    Task<FlightSearchResponse> SearchAsync(
        FlightSearchRequest request,
        CancellationToken ct = default);

    Task<FlightAirportLookupResponse> LookupAirportsAsync(
        FlightAirportLookupRequest request,
        CancellationToken ct = default);

    Task<FlightOfferDetailsResponse?> GetOfferDetailsAsync(
        FlightOfferDetailsRequest request,
        CancellationToken ct = default);
}
