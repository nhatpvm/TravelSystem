using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class BusTourPackageReservationAdapter : ITourPackageSourceReservationAdapter
{
    private readonly AppDbContext _db;

    public BusTourPackageReservationAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Bus;

    public async Task<TourPackageSourceReservationHoldResult> HoldAsync(
        TourPackageSourceReservationHoldRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Line.Quantity <= 0)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Bus reservation requires quantity greater than 0."
            };
        }

        var segmentPriceId = request.Line.BoundSourceEntityId;
        if (!segmentPriceId.HasValue || segmentPriceId.Value == Guid.Empty)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Bus reservation source is missing BoundSourceEntityId."
            };
        }

        var segmentPrice = await _db.BusTripSegmentPrices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == segmentPriceId.Value &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (segmentPrice is null)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Bus segment price source was not found."
            };
        }

        var trip = await _db.BusTrips
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == segmentPrice.TripId &&
                !x.IsDeleted, ct);

        if (trip is null || !trip.IsActive || trip.Status != TripStatus.Published)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Bus trip source is not available for reservation."
            };
        }

        var vehicle = await _db.Vehicles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == trip.VehicleId &&
                x.TenantId == trip.TenantId &&
                !x.IsDeleted, ct);

        if (vehicle?.SeatMapId is null)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Bus trip vehicle or seat map was not found."
            };
        }

        var now = DateTimeOffset.Now;
        var holdMinutes = await ResolveHoldMinutesAsync(trip.TenantId, ct);
        var expiresAt = now.AddMinutes(holdMinutes);
        var holdToken = $"pkg-bus-{Guid.NewGuid():N}";

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        await ExpireTripHoldsAsync(trip.Id, trip.TenantId, now, ct);

        var seats = await _db.Seats
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == trip.TenantId &&
                x.SeatMapId == vehicle.SeatMapId.Value &&
                !x.IsDeleted &&
                x.IsActive)
            .OrderBy(x => x.DeckIndex)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .ThenBy(x => x.SeatNumber)
            .ToListAsync(ct);

        var occupiedSeatIds = await _db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == trip.TenantId &&
                x.TripId == trip.Id &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(segmentPrice.FromStopIndex, segmentPrice.ToStopIndex)
            .Select(x => x.SeatId)
            .Distinct()
            .ToListAsync(ct);

        var selectedSeats = seats
            .Where(x => !occupiedSeatIds.Contains(x.Id))
            .Take(request.Line.Quantity)
            .ToList();

        if (selectedSeats.Count < request.Line.Quantity)
        {
            await tx.CommitAsync(ct);

            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Bus trip does not have enough available seats for this package reservation."
            };
        }

        var rows = selectedSeats.Select(seat => new TripSeatHold
        {
            Id = Guid.NewGuid(),
            TenantId = trip.TenantId,
            TripId = trip.Id,
            SeatId = seat.Id,
            FromTripStopTimeId = segmentPrice.FromTripStopTimeId,
            ToTripStopTimeId = segmentPrice.ToTripStopTimeId,
            FromStopIndex = segmentPrice.FromStopIndex,
            ToStopIndex = segmentPrice.ToStopIndex,
            Status = SeatHoldStatus.Held,
            UserId = request.UserId,
            BookingId = null,
            HoldToken = holdToken,
            HoldExpiresAt = expiresAt,
            IsDeleted = false,
            CreatedAt = now
        }).ToList();

        _db.BusTripSeatHolds.AddRange(rows);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new TourPackageSourceReservationHoldResult
        {
            Status = TourPackageReservationHoldOutcomeStatus.Held,
            SourceHoldToken = holdToken,
            HoldExpiresAt = expiresAt,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                tripId = trip.Id,
                tripCode = trip.Code,
                tripName = trip.Name,
                segmentPriceId = segmentPrice.Id,
                segmentPrice.FromTripStopTimeId,
                segmentPrice.ToTripStopTimeId,
                segmentPrice.FromStopIndex,
                segmentPrice.ToStopIndex,
                seatIds = rows.Select(x => x.SeatId).ToList()
            }),
            Note = $"Held {rows.Count} bus seat(s)."
        };
    }

    public async Task ReleaseAsync(
        TourPackageSourceReservationReleaseRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Item.SourceHoldToken))
            return;

        var holds = await _db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == request.Item.SourceHoldToken &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held)
            .ToListAsync(ct);

        if (holds.Count == 0)
            return;

        var now = DateTimeOffset.Now;
        foreach (var hold in holds)
        {
            hold.Status = SeatHoldStatus.Cancelled;
            hold.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> ResolveHoldMinutesAsync(Guid tenantId, CancellationToken ct)
    {
        var holdMinutes = await _db.Tenants
            .IgnoreQueryFilters()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => x.HoldMinutes)
            .FirstOrDefaultAsync(ct);

        return holdMinutes > 0 ? holdMinutes : 5;
    }

    private async Task ExpireTripHoldsAsync(Guid tripId, Guid tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var expired = await _db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == tripId &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held &&
                x.HoldExpiresAt <= now)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return;

        foreach (var item in expired)
        {
            item.Status = SeatHoldStatus.Expired;
            item.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}
