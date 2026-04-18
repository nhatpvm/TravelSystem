using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Bus;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Bus;

public static class BusSeatOccupancySupport
{
    public const string TripMutationBlockedMessage =
        "Trip cannot be modified while it has active or confirmed seat occupancy.";

    public static IQueryable<TripSeatHold> WhereActiveSeatOccupancy(
        this IQueryable<TripSeatHold> query,
        DateTimeOffset now)
        => query.Where(x =>
            !x.IsDeleted &&
            (x.Status == SeatHoldStatus.Confirmed ||
             (x.Status == SeatHoldStatus.Held && x.HoldExpiresAt > now)));

    public static IQueryable<TripSeatHold> WhereOverlappingSegment(
        this IQueryable<TripSeatHold> query,
        int fromStopIndex,
        int toStopIndex)
        => query.Where(x => x.FromStopIndex < toStopIndex && fromStopIndex < x.ToStopIndex);

    public static Task<bool> HasActiveSeatOccupancyAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tripId,
        DateTimeOffset now,
        CancellationToken ct)
        => db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId)
            .WhereActiveSeatOccupancy(now)
            .AnyAsync(ct);

    public static Task<int> CountActiveSeatOccupancyAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tripId,
        DateTimeOffset now,
        CancellationToken ct)
        => db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId)
            .WhereActiveSeatOccupancy(now)
            .CountAsync(ct);
}
