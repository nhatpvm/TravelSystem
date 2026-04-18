using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class TrainTourPackageCancellationAdapter : ITourPackageSourceCancellationAdapter
{
    private readonly AppDbContext _db;

    public TrainTourPackageCancellationAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Train;

    public async Task<TourPackageSourceCancellationResult> CancelAsync(
        TourPackageSourceCancellationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.BookingItem.SourceHoldToken))
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Train booking item is missing SourceHoldToken."
            };
        }

        var holds = await _db.TrainTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == request.BookingItem.SourceHoldToken &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Confirmed)
            .ToListAsync(ct);

        if (holds.Count == 0)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Train seats are no longer in a confirmed state for cancellation."
            };
        }

        var tripId = holds[0].TripId;
        var trip = await _db.TrainTrips
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tripId && !x.IsDeleted, ct);

        if (trip is null)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Train trip source was not found for cancellation."
            };
        }

        var fromStop = await _db.TrainTripStopTimes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == holds[0].FromTripStopTimeId &&
                x.TripId == tripId &&
                !x.IsDeleted, ct);

        var serviceStartAt = fromStop?.DepartAt ?? fromStop?.ArriveAt ?? trip.DepartureAt;

        if (serviceStartAt <= request.CurrentTime)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Train trip has already departed and can no longer be cancelled."
            };
        }

        foreach (var hold in holds)
        {
            hold.Status = TrainSeatHoldStatus.Cancelled;
            hold.UpdatedAt = request.CurrentTime;
            hold.UpdatedByUserId = request.UserId;
        }

        await _db.SaveChangesAsync(ct);

        return new TourPackageSourceCancellationResult
        {
            Status = TourPackageSourceCancellationOutcomeStatus.Cancelled,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                tripId = trip.Id,
                tripCode = trip.Code,
                holdIds = holds.Select(x => x.Id).ToList(),
                seatIds = holds.Select(x => x.TrainCarSeatId).ToList()
            }),
            Note = $"Cancelled {holds.Count} confirmed train seat(s)."
        };
    }
}
