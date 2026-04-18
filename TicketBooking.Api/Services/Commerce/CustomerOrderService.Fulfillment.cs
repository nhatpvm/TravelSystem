using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Train;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CustomerOrderService
{
    private async Task ConfirmBusOrderAsync(CustomerOrder order, Guid actorUserId, CancellationToken ct)
    {
        var metadata = DeserializeMetadata<BusOrderMetadata>(order.MetadataJson)
            ?? throw new InvalidOperationException("Thiếu metadata của đơn xe khách.");

        var holds = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == metadata.HoldToken &&
                x.TripId == metadata.TripId &&
                x.UserId == order.UserId &&
                !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var hold in holds.Where(x => x.Status == SeatHoldStatus.Held))
        {
            hold.Status = SeatHoldStatus.Confirmed;
            hold.BookingId = order.Id;
            hold.UpdatedAt = DateTimeOffset.UtcNow;
            hold.UpdatedByUserId = actorUserId;
        }
    }

    private async Task ConfirmTrainOrderAsync(CustomerOrder order, Guid actorUserId, CancellationToken ct)
    {
        var metadata = DeserializeMetadata<TrainOrderMetadata>(order.MetadataJson)
            ?? throw new InvalidOperationException("Thiếu metadata của đơn tàu.");

        var holds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == metadata.HoldToken &&
                x.TripId == metadata.TripId &&
                x.UserId == order.UserId &&
                !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var hold in holds.Where(x => x.Status == TrainSeatHoldStatus.Held))
        {
            hold.Status = TrainSeatHoldStatus.Confirmed;
            hold.BookingId = order.Id;
            hold.UpdatedAt = DateTimeOffset.UtcNow;
            hold.UpdatedByUserId = actorUserId;
        }
    }

    private Task ConfirmFlightOrderAsync(CustomerOrder order, Guid actorUserId, CancellationToken ct)
    {
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.UpdatedByUserId = actorUserId;
        return Task.CompletedTask;
    }

    private async Task ConfirmHotelOrderAsync(CustomerOrder order, Guid actorUserId, CancellationToken ct)
    {
        var metadata = DeserializeMetadata<HotelOrderMetadata>(order.MetadataJson)
            ?? throw new InvalidOperationException("Thiếu metadata của đơn khách sạn.");

        var hold = await _db.InventoryHolds.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == metadata.InventoryHoldId &&
                x.BookingId == order.Id &&
                !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Không tìm thấy hold tồn phòng của đơn khách sạn.");

        if (hold.Status == HoldStatus.Confirmed)
            return;

        var stayDates = EachNight(metadata.CheckInDate, metadata.CheckOutDate).ToList();
        var inventories = await _db.RoomTypeInventories.IgnoreQueryFilters()
            .Where(x =>
                x.RoomTypeId == metadata.RoomTypeId &&
                stayDates.Contains(x.Date) &&
                !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var inventory in inventories)
        {
            inventory.HeldUnits = Math.Max(0, inventory.HeldUnits - metadata.RoomCount);
            inventory.SoldUnits += metadata.RoomCount;
            inventory.UpdatedAt = DateTimeOffset.UtcNow;
            inventory.UpdatedByUserId = actorUserId;
        }

        hold.Status = HoldStatus.Confirmed;
        hold.UpdatedAt = DateTimeOffset.UtcNow;
        hold.UpdatedByUserId = actorUserId;
    }

    private async Task ConfirmTourOrderAsync(CustomerOrder order, Guid actorUserId, CancellationToken ct)
    {
        if (order.SourceBookingId.HasValue && order.SourceBookingId != Guid.Empty)
            return;

        var metadata = DeserializeMetadata<TourOrderMetadata>(order.MetadataJson)
            ?? throw new InvalidOperationException("Thiếu metadata của đơn tour.");

        var result = await _tourBookingService.ConfirmAsync(
            metadata.TourId,
            new TourPackageBookingConfirmRequest
            {
                ReservationId = metadata.ReservationId,
                Notes = order.CustomerNote,
            },
            order.UserId,
            false,
            ct);

        order.SourceBookingId = result.Booking.Id;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.UpdatedByUserId = actorUserId;
    }

    private async Task ReleaseBusOrderAsync(
        CustomerOrder order,
        Guid actorUserId,
        CancellationToken ct,
        CustomerPaymentStatus finalStatus)
    {
        var metadata = DeserializeMetadata<BusOrderMetadata>(order.MetadataJson);
        if (metadata is null)
            return;

        var holds = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == metadata.HoldToken &&
                x.TripId == metadata.TripId &&
                x.UserId == order.UserId &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held)
            .ToListAsync(ct);

        foreach (var hold in holds)
        {
            hold.Status = finalStatus == CustomerPaymentStatus.Expired ? SeatHoldStatus.Expired : SeatHoldStatus.Cancelled;
            hold.UpdatedAt = DateTimeOffset.UtcNow;
            hold.UpdatedByUserId = actorUserId;
        }
    }

    private async Task ReleaseTrainOrderAsync(
        CustomerOrder order,
        Guid actorUserId,
        CancellationToken ct,
        CustomerPaymentStatus finalStatus)
    {
        var metadata = DeserializeMetadata<TrainOrderMetadata>(order.MetadataJson);
        if (metadata is null)
            return;

        var holds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == metadata.HoldToken &&
                x.TripId == metadata.TripId &&
                x.UserId == order.UserId &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Held)
            .ToListAsync(ct);

        foreach (var hold in holds)
        {
            hold.Status = finalStatus == CustomerPaymentStatus.Expired ? TrainSeatHoldStatus.Expired : TrainSeatHoldStatus.Cancelled;
            hold.UpdatedAt = DateTimeOffset.UtcNow;
            hold.UpdatedByUserId = actorUserId;
        }
    }

    private async Task ReleaseTourOrderAsync(
        CustomerOrder order,
        Guid actorUserId,
        CancellationToken ct)
    {
        var metadata = DeserializeMetadata<TourOrderMetadata>(order.MetadataJson);
        if (metadata is null || !order.SourceReservationId.HasValue || order.SourceReservationId == Guid.Empty)
            return;

        await _tourReservationService.ReleaseAsync(
            metadata.TourId,
            order.SourceReservationId.Value,
            order.UserId,
            false,
            ct);
    }
}
