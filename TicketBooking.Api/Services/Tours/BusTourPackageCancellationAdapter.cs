using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class BusTourPackageCancellationAdapter : ITourPackageSourceCancellationAdapter
{
    private readonly AppDbContext _db;

    public BusTourPackageCancellationAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Bus;

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
                ErrorMessage = "Bus booking item is missing SourceHoldToken."
            };
        }

        var holds = await _db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == request.BookingItem.SourceHoldToken &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Confirmed)
            .ToListAsync(ct);

        if (holds.Count == 0)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Bus seats are no longer in a confirmed state for cancellation."
            };
        }

        var tripId = holds[0].TripId;
        var trip = await _db.BusTrips
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tripId && !x.IsDeleted, ct);

        if (trip is null)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Bus trip source was not found for cancellation."
            };
        }

        if (trip.DepartureAt <= request.CurrentTime)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Bus trip has already departed and can no longer be cancelled."
            };
        }

        foreach (var hold in holds)
        {
            hold.Status = SeatHoldStatus.Cancelled;
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
                seatIds = holds.Select(x => x.SeatId).ToList()
            }),
            Note = $"Cancelled {holds.Count} confirmed bus seat(s)."
        };
    }
}
