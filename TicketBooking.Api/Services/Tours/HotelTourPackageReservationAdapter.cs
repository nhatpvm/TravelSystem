using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class HotelTourPackageReservationAdapter : ITourPackageSourceReservationAdapter
{
    private readonly AppDbContext _db;

    public HotelTourPackageReservationAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Hotel;

    public async Task<TourPackageSourceReservationHoldResult> HoldAsync(
        TourPackageSourceReservationHoldRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mappingId = request.Line.BoundSourceEntityId;
        if (!mappingId.HasValue || mappingId.Value == Guid.Empty)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Hotel reservation source is missing BoundSourceEntityId."
            };
        }

        var mapping = await _db.RatePlanRoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == mappingId.Value &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (mapping is null)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Hotel rate mapping source was not found."
            };
        }

        var roomType = await _db.RoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == mapping.RoomTypeId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == RoomTypeStatus.Active, ct);

        if (roomType is null)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Hotel room type source is not available."
            };
        }

        var nightCount = ResolveNightCount(request);
        if (nightCount <= 0)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Hotel reservation requires at least 1 night."
            };
        }

        var checkInDate = ResolveCheckInDate(request);
        var checkOutDate = checkInDate.AddDays(nightCount);
        var stayDates = EachNight(checkInDate, checkOutDate).ToList();
        var roomCount = Math.Max(request.Line.Quantity, 1);
        var now = DateTimeOffset.Now;

        await ExpireInventoryHoldsAsync(roomType.Id, stayDates, now, ct);

        var inventories = await _db.RoomTypeInventories
            .IgnoreQueryFilters()
            .Where(x =>
                x.RoomTypeId == roomType.Id &&
                !x.IsDeleted &&
                x.Status == InventoryStatus.Open &&
                stayDates.Contains(x.Date))
            .ToListAsync(ct);

        if (inventories.Count != stayDates.Count)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Hotel source does not have inventory for the full stay."
            };
        }

        if (inventories.Any(x => (x.TotalUnits - x.SoldUnits - x.HeldUnits) < roomCount))
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Hotel source does not have enough available units for this package reservation."
            };
        }

        var holdMinutes = await ResolveHoldMinutesAsync(roomType.TenantId, ct);
        var expiresAt = now.AddMinutes(holdMinutes);
        var correlationId = $"pkg-hotel-{Guid.NewGuid():N}";

        foreach (var inventory in inventories)
            inventory.HeldUnits += roomCount;

        var hold = new InventoryHold
        {
            Id = Guid.NewGuid(),
            TenantId = roomType.TenantId,
            RoomTypeId = roomType.Id,
            BookingId = null,
            BookingItemId = null,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Units = roomCount,
            Status = HoldStatus.Held,
            HoldExpiresAt = expiresAt,
            CorrelationId = correlationId,
            Notes = $"Package reservation {request.ReservationToken}",
            IsDeleted = false,
            CreatedAt = now
        };

        _db.InventoryHolds.Add(hold);
        await _db.SaveChangesAsync(ct);

        return new TourPackageSourceReservationHoldResult
        {
            Status = TourPackageReservationHoldOutcomeStatus.Held,
            SourceHoldToken = correlationId,
            HoldExpiresAt = expiresAt,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                hold.Id,
                hold.RoomTypeId,
                hold.CheckInDate,
                hold.CheckOutDate,
                hold.Units
            }),
            Note = $"Held hotel inventory for {roomCount} room unit(s)."
        };
    }

    public async Task ReleaseAsync(
        TourPackageSourceReservationReleaseRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Item.SourceHoldToken))
            return;

        var holds = await _db.InventoryHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.CorrelationId == request.Item.SourceHoldToken &&
                !x.IsDeleted &&
                x.Status == HoldStatus.Held)
            .ToListAsync(ct);

        if (holds.Count == 0)
            return;

        var now = DateTimeOffset.Now;
        foreach (var hold in holds)
        {
            hold.Status = HoldStatus.Cancelled;
            hold.UpdatedAt = now;
        }

        await AdjustInventoryHeldUnitsAsync(holds, decrement: true, ct);
        await _db.SaveChangesAsync(ct);
    }

    private int ResolveNightCount(TourPackageSourceReservationHoldRequest request)
    {
        if (request.Option.BindingMode == TourPackageBindingMode.SearchTemplate &&
            !string.IsNullOrWhiteSpace(request.Option.SearchTemplateJson))
        {
            try
            {
                var template = TourPackageSearchTemplateSupport.ParseRequired(request.Option.SearchTemplateJson);
                return TourPackageSearchTemplateSupport.ResolveNightCount(
                    new TourPackageSourceQuoteAdapterRequest
                    {
                        Schedule = request.Schedule,
                        Component = request.Component,
                        Option = request.Option,
                        TotalNights = request.TotalNights
                    },
                    template);
            }
            catch
            {
            }
        }

        return request.TotalNights > 0 ? request.TotalNights : Math.Max(request.Schedule.ReturnDate.DayNumber - request.Schedule.DepartureDate.DayNumber, 1);
    }

    private DateOnly ResolveCheckInDate(TourPackageSourceReservationHoldRequest request)
    {
        if (request.Option.BindingMode == TourPackageBindingMode.SearchTemplate &&
            !string.IsNullOrWhiteSpace(request.Option.SearchTemplateJson))
        {
            try
            {
                var template = TourPackageSearchTemplateSupport.ParseRequired(request.Option.SearchTemplateJson);
                return TourPackageSearchTemplateSupport.ResolveServiceDate(request.Schedule, request.Component, template);
            }
            catch
            {
            }
        }

        return request.Schedule.DepartureDate;
    }

    private static IEnumerable<DateOnly> EachNight(DateOnly checkInDate, DateOnly checkOutDate)
    {
        for (var d = checkInDate; d < checkOutDate; d = d.AddDays(1))
            yield return d;
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

    private async Task ExpireInventoryHoldsAsync(
        Guid roomTypeId,
        IReadOnlyCollection<DateOnly> stayDates,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var expiredHolds = await _db.InventoryHolds
            .IgnoreQueryFilters()
            .Where(x =>
                x.RoomTypeId == roomTypeId &&
                !x.IsDeleted &&
                x.Status == HoldStatus.Held &&
                x.HoldExpiresAt <= now &&
                x.CheckInDate < stayDates.Max().AddDays(1) &&
                stayDates.Min() < x.CheckOutDate)
            .ToListAsync(ct);

        if (expiredHolds.Count == 0)
            return;

        foreach (var hold in expiredHolds)
        {
            hold.Status = HoldStatus.Expired;
            hold.UpdatedAt = now;
        }

        await AdjustInventoryHeldUnitsAsync(expiredHolds, decrement: true, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task AdjustInventoryHeldUnitsAsync(
        IReadOnlyCollection<InventoryHold> holds,
        bool decrement,
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
                inventory.HeldUnits = decrement
                    ? Math.Max(0, inventory.HeldUnits - hold.Units)
                    : inventory.HeldUnits + hold.Units;
            }
        }
    }
}
