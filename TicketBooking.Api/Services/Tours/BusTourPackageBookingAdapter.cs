using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class BusTourPackageBookingAdapter : ITourPackageSourceBookingAdapter
{
    private readonly AppDbContext _db;

    public BusTourPackageBookingAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Bus;

    public async Task<TourPackageSourceBookingConfirmResult> ConfirmAsync(
        TourPackageSourceBookingConfirmRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ReservationItem.SourceHoldToken))
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Bus reservation item is missing SourceHoldToken."
            };
        }

        var holds = await _db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == request.ReservationItem.SourceHoldToken &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held)
            .ToListAsync(ct);

        if (holds.Count == 0)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Bus hold is no longer active for confirmation."
            };
        }

        var now = DateTimeOffset.Now;
        foreach (var hold in holds)
        {
            hold.Status = SeatHoldStatus.Confirmed;
            hold.BookingId = request.Booking.Id;
            hold.UpdatedAt = now;
            hold.UpdatedByUserId = request.UserId;
        }

        await _db.SaveChangesAsync(ct);

        return new TourPackageSourceBookingConfirmResult
        {
            Status = TourPackageBookingConfirmOutcomeStatus.Confirmed,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                bookingId = request.Booking.Id,
                bookingItemId = request.BookingItem.Id,
                holdToken = request.ReservationItem.SourceHoldToken,
                holdIds = holds.Select(x => x.Id).ToList(),
                seatIds = holds.Select(x => x.SeatId).ToList()
            }),
            Note = $"Confirmed {holds.Count} bus seat hold(s)."
        };
    }
}
