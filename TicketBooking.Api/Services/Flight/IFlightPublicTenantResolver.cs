//FILE: TicketBooking.Api / Services / Flight / IFlightPublicTenantResolver.cs
namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Resolves tenant scope for public Flight endpoints when X-TenantId is not supplied.
/// This allows customer-facing endpoints to work in 2 modes:
/// - Scoped mode: tenant already resolved by middleware
/// - Public/global mode: resolve tenant(s) automatically from flight data
/// </summary>
public interface IFlightPublicTenantResolver
{
    /// <summary>
    /// Resolve candidate tenant ids for public search by route code/IATA.
    /// Example: from=SGN, to=HAN.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> ResolveTenantIdsForSearchAsync(
        string from,
        string to,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve candidate tenant ids for airport lookup.
    /// When query is empty, implementation may return all active flight tenants
    /// that currently have active airports.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> ResolveTenantIdsForAirportLookupAsync(
        string? query,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve tenant id for a specific offer.
    /// Returns null when the offer does not exist or is expired and includeExpired=false.
    /// </summary>
    Task<Guid?> ResolveTenantIdForOfferAsync(
        Guid offerId,
        bool includeExpired,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve tenant id for a specific cabin seat map.
    /// Returns null when the seat map does not exist or is inactive/deleted.
    /// </summary>
    Task<Guid?> ResolveTenantIdForCabinSeatMapAsync(
        Guid cabinSeatMapId,
        CancellationToken ct = default);
}


