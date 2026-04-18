using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Flight;

public static class FlightOfferSupport
{
    public static IQueryable<Offer> WhereInventoryPool(
        this IQueryable<Offer> query,
        Guid tenantId,
        Guid flightId,
        Guid fareClassId)
        => query.Where(x =>
            x.TenantId == tenantId &&
            x.FlightId == flightId &&
            x.FareClassId == fareClassId &&
            !x.IsDeleted);

    public static IOrderedQueryable<Offer> OrderByInventoryPriority(
        this IQueryable<Offer> query,
        DateTimeOffset now)
        => query
            .OrderByDescending(x => x.Status == OfferStatus.Active && x.ExpiresAt > now ? 1 : 0)
            .ThenByDescending(x => x.Status == OfferStatus.Active ? 1 : 0)
            .ThenByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id);

    public static async Task<Offer?> ResolveCanonicalOfferAsync(
        AppDbContext db,
        Guid tenantId,
        Guid flightId,
        Guid fareClassId,
        DateTimeOffset now,
        bool asNoTracking,
        CancellationToken ct = default)
    {
        IQueryable<Offer> query = db.FlightOffers
            .IgnoreQueryFilters()
            .WhereInventoryPool(tenantId, flightId, fareClassId);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query
            .OrderByInventoryPriority(now)
            .FirstOrDefaultAsync(ct);
    }

    public static async Task<int> ResolveCurrentSeatsAvailableAsync(
        AppDbContext db,
        Guid tenantId,
        Guid flightId,
        Guid fareClassId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var offer = await ResolveCanonicalOfferAsync(
            db,
            tenantId,
            flightId,
            fareClassId,
            now,
            asNoTracking: true,
            ct);

        return Math.Max(0, offer?.SeatsAvailable ?? 0);
    }

    public static DateOnly ResolveLocalDate(
        DateTimeOffset timestamp,
        string? timeZoneId)
    {
        var timeZone = TryResolveTimeZoneInfo(timeZoneId);
        if (timeZone is null)
            return DateOnly.FromDateTime(timestamp.DateTime);

        var localTime = TimeZoneInfo.ConvertTime(timestamp, timeZone);
        return DateOnly.FromDateTime(localTime.DateTime);
    }

    public static string BuildDisplayFlightNumber(
        IEnumerable<string?> flightNumbers,
        string fallback)
    {
        var normalized = flightNumbers
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
            return fallback;

        return string.Join("/", normalized);
    }

    private static TimeZoneInfo? TryResolveTimeZoneInfo(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
        }
        catch (InvalidTimeZoneException)
        {
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId.Trim(), out var windowsId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return null;
    }
}
