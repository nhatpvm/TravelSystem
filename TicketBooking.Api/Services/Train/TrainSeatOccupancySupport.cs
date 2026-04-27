using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Train;

public static class TrainSeatOccupancySupport
{
    public const string TripMutationBlockedMessage =
        "Train trip cannot be modified while it has active or confirmed seat occupancy.";

    public static IQueryable<TrainTripSeatHold> WhereActiveSeatOccupancy(
        this IQueryable<TrainTripSeatHold> query,
        DateTimeOffset now)
        => query.Where(x =>
            !x.IsDeleted &&
            (x.Status == TrainSeatHoldStatus.Confirmed ||
             (x.Status == TrainSeatHoldStatus.Held && x.HoldExpiresAt > now)));

    public static IQueryable<TrainTripSeatHold> WhereOverlappingSegment(
        this IQueryable<TrainTripSeatHold> query,
        int fromStopIndex,
        int toStopIndex)
        => query.Where(x => x.FromStopIndex < toStopIndex && fromStopIndex < x.ToStopIndex);

    public static IQueryable<TrainSeatBlock> WhereActiveSeatBlocks(
        this IQueryable<TrainSeatBlock> query)
        => query.Where(x => !x.IsDeleted && x.Status == TrainSeatBlockStatus.Active);

    public static IQueryable<TrainSeatBlock> WhereOverlappingSegment(
        this IQueryable<TrainSeatBlock> query,
        int fromStopIndex,
        int toStopIndex)
        => query.Where(x => x.FromStopIndex < toStopIndex && fromStopIndex < x.ToStopIndex);

    public static Task<bool> HasActiveSeatOccupancyAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tripId,
        DateTimeOffset now,
        CancellationToken ct)
        => db.TrainTripSeatHolds
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
        => db.TrainTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId)
            .WhereActiveSeatOccupancy(now)
            .CountAsync(ct);

    public static Task<bool> HasActiveSeatBlockAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tripId,
        IReadOnlyCollection<Guid> seatIds,
        int fromStopIndex,
        int toStopIndex,
        CancellationToken ct)
        => db.TrainSeatBlocks
            .IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == tripId &&
                seatIds.Contains(x.TrainCarSeatId))
            .WhereActiveSeatBlocks()
            .WhereOverlappingSegment(fromStopIndex, toStopIndex)
            .AnyAsync(ct);
}
