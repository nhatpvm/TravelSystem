using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Resolves tenant scope for public Flight endpoints when X-TenantId is not supplied.
/// Strategy:
/// - Search: find tenants that have both matching from-airport and to-airport
/// - Airport lookup: find tenants that have matching active airports
/// - Offer details: resolve tenant from the offer itself
/// - Cabin seat map: resolve tenant from the seat-map itself
/// </summary>
public sealed class FlightPublicTenantResolver : IFlightPublicTenantResolver
{
    private readonly AppDbContext _db;

    public FlightPublicTenantResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<Guid>> ResolveTenantIdsForSearchAsync(
        string from,
        string to,
        CancellationToken ct = default)
    {
        from = NormalizeCode(from);
        to = NormalizeCode(to);

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return Array.Empty<Guid>();

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<Guid>();

        var airportQuery = _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive);

        var fromTenantIds = await airportQuery
            .Where(x =>
                x.Code.ToUpper() == from ||
                (x.IataCode != null && x.IataCode.ToUpper() == from))
            .Select(x => x.TenantId)
            .Distinct()
            .ToListAsync(ct);

        if (fromTenantIds.Count == 0)
            return Array.Empty<Guid>();

        var toTenantIds = await airportQuery
            .Where(x =>
                x.Code.ToUpper() == to ||
                (x.IataCode != null && x.IataCode.ToUpper() == to))
            .Select(x => x.TenantId)
            .Distinct()
            .ToListAsync(ct);

        if (toTenantIds.Count == 0)
            return Array.Empty<Guid>();

        var toSet = new HashSet<Guid>(toTenantIds);
        var tenantIds = fromTenantIds
            .Where(id => toSet.Contains(id))
            .Distinct()
            .ToList();

        return tenantIds;
    }

    public async Task<IReadOnlyCollection<Guid>> ResolveTenantIdsForAirportLookupAsync(
        string? query,
        CancellationToken ct = default)
    {
        var q = NormalizeCode(query);

        var airportQuery = _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            airportQuery = airportQuery.Where(x =>
                x.Code.ToUpper().Contains(q) ||
                (x.IataCode != null && x.IataCode.ToUpper().Contains(q)) ||
                (x.IcaoCode != null && x.IcaoCode.ToUpper().Contains(q)) ||
                x.Name.ToUpper().Contains(q));
        }

        var tenantIds = await airportQuery
            .Select(x => x.TenantId)
            .Distinct()
            .ToListAsync(ct);

        return tenantIds;
    }

    public async Task<Guid?> ResolveTenantIdForOfferAsync(
        Guid offerId,
        bool includeExpired,
        CancellationToken ct = default)
    {
        if (offerId == Guid.Empty)
            return null;

        var now = DateTimeOffset.Now;

        var query = _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == offerId && !x.IsDeleted);

        if (!includeExpired)
        {
            query = query.Where(x =>
                x.Status == OfferStatus.Active &&
                x.ExpiresAt > now);
        }

        return await query
            .Select(x => (Guid?)x.TenantId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid?> ResolveTenantIdForCabinSeatMapAsync(
        Guid cabinSeatMapId,
        CancellationToken ct = default)
    {
        if (cabinSeatMapId == Guid.Empty)
            return null;

        return await _db.FlightCabinSeatMaps
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.Id == cabinSeatMapId &&
                !x.IsDeleted &&
                x.IsActive)
            .Select(x => (Guid?)x.TenantId)
            .FirstOrDefaultAsync(ct);
    }

    private static string NormalizeCode(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}

