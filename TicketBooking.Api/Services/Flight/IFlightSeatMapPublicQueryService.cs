//FILE: TicketBooking.Api / Services / Flight / IFlightSeatMapPublicQueryService.cs
using TicketBooking.Api.Contracts.Flight;

namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Customer/public read-only seat-map queries for Flight.
/// Separated from general public query service to keep responsibilities focused.
/// </summary>
public interface IFlightSeatMapPublicQueryService
{
    Task<FlightSeatMapResponse?> GetByOfferAsync(
        FlightSeatMapByOfferRequest request,
        CancellationToken ct = default);

    Task<FlightSeatMapResponse?> GetByIdAsync(
        FlightSeatMapByIdRequest request,
        CancellationToken ct = default);
}
