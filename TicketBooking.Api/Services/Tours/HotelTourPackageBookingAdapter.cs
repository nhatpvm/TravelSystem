using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class HotelTourPackageBookingAdapter : ITourPackageSourceBookingAdapter
{
    private readonly AppDbContext _db;

    public HotelTourPackageBookingAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Hotel;

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
                ErrorMessage = "Hotel reservation item is missing SourceHoldToken."
            };
        }

        var holds = await _db.InventoryHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.CorrelationId == request.ReservationItem.SourceHoldToken &&
                !x.IsDeleted &&
                x.Status == HoldStatus.Held)
            .ToListAsync(ct);

        if (holds.Count == 0)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Hotel hold is no longer active for confirmation."
            };
        }

        await AdjustInventoryAsync(holds, ct);

        var now = DateTimeOffset.Now;
        foreach (var hold in holds)
        {
            hold.Status = HoldStatus.Confirmed;
            hold.BookingId = request.Booking.Id;
            hold.BookingItemId = request.BookingItem.Id;
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
                correlationId = request.ReservationItem.SourceHoldToken,
                holdIds = holds.Select(x => x.Id).ToList(),
                roomTypeIds = holds.Select(x => x.RoomTypeId).Distinct().ToList(),
                checkInDate = holds.Min(x => x.CheckInDate),
                checkOutDate = holds.Max(x => x.CheckOutDate),
                units = holds.Sum(x => x.Units)
            }),
            Note = $"Confirmed {holds.Sum(x => x.Units)} hotel room unit(s)."
        };
    }

    private async Task AdjustInventoryAsync(
        IReadOnlyCollection<InventoryHold> holds,
        CancellationToken ct)
    {
        var roomTypeIds = holds.Select(x => x.RoomTypeId).Distinct().ToList();
        var minDate = holds.Min(x => x.CheckInDate);
        var maxDate = holds.Max(x => x.CheckOutDate);

        var inventories = await _db.RoomTypeInventories
            .IgnoreQueryFilters()
            .Where(x =>
                roomTypeIds.Contains(x.RoomTypeId) &&
                !x.IsDeleted &&
                x.Date >= minDate &&
                x.Date < maxDate)
            .ToListAsync(ct);

        foreach (var hold in holds)
        {
            foreach (var inventory in inventories.Where(x =>
                         x.RoomTypeId == hold.RoomTypeId &&
                         x.Date >= hold.CheckInDate &&
                         x.Date < hold.CheckOutDate))
            {
                inventory.HeldUnits = Math.Max(0, inventory.HeldUnits - hold.Units);
                inventory.SoldUnits += hold.Units;
            }
        }
    }
}
