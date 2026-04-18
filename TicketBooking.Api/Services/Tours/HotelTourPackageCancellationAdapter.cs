using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class HotelTourPackageCancellationAdapter : ITourPackageSourceCancellationAdapter
{
    private readonly AppDbContext _db;

    public HotelTourPackageCancellationAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Hotel;

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
                ErrorMessage = "Hotel booking item is missing SourceHoldToken."
            };
        }

        var holds = await _db.InventoryHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.CorrelationId == request.BookingItem.SourceHoldToken &&
                !x.IsDeleted &&
                x.Status == HoldStatus.Confirmed)
            .ToListAsync(ct);

        if (holds.Count == 0)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Hotel inventory is no longer in a confirmed state for cancellation."
            };
        }

        var currentDate = DateOnly.FromDateTime(request.CurrentTime.Date);
        if (holds.Any(x => x.CheckInDate <= currentDate))
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Hotel stay has already started and can no longer be cancelled."
            };
        }

        await AdjustInventorySoldUnitsAsync(holds, ct);

        foreach (var hold in holds)
        {
            hold.Status = HoldStatus.Cancelled;
            hold.UpdatedAt = request.CurrentTime;
            hold.UpdatedByUserId = request.UserId;
        }

        await _db.SaveChangesAsync(ct);

        return new TourPackageSourceCancellationResult
        {
            Status = TourPackageSourceCancellationOutcomeStatus.Cancelled,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                correlationId = request.BookingItem.SourceHoldToken,
                holdIds = holds.Select(x => x.Id).ToList(),
                roomTypeIds = holds.Select(x => x.RoomTypeId).Distinct().ToList(),
                units = holds.Sum(x => x.Units)
            }),
            Note = $"Cancelled {holds.Sum(x => x.Units)} confirmed hotel room unit(s)."
        };
    }

    private async Task AdjustInventorySoldUnitsAsync(
        IReadOnlyCollection<InventoryHold> holds,
        CancellationToken ct)
    {
        if (holds.Count == 0)
            return;

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
                inventory.SoldUnits = Math.Max(0, inventory.SoldUnits - hold.Units);
            }
        }
    }
}
