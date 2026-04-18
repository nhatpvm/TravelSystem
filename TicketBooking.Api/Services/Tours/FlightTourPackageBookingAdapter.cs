using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class FlightTourPackageBookingAdapter : ITourPackageSourceBookingAdapter
{
    private readonly AppDbContext _db;

    public FlightTourPackageBookingAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Flight;

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
                ErrorMessage = "Flight reservation item is missing SourceHoldToken."
            };
        }

        if (!request.ReservationItem.SourceEntityId.HasValue || request.ReservationItem.SourceEntityId.Value == Guid.Empty)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Flight reservation item is missing SourceEntityId."
            };
        }

        var offer = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.ReservationItem.SourceEntityId.Value &&
                !x.IsDeleted, ct);

        if (offer is null)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Flight offer source was not found."
            };
        }

        var now = DateTimeOffset.Now;
        if (offer.Status != OfferStatus.Active || offer.ExpiresAt <= now)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Flight offer source is no longer active for confirmation."
            };
        }

        var flight = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == offer.FlightId &&
                !x.IsDeleted, ct);

        if (flight is null || !flight.IsActive || flight.Status != FlightStatus.Published)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Flight source is no longer available for confirmation."
            };
        }

        if (flight.DepartureAt <= now)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Flight has already departed and can no longer be confirmed."
            };
        }

        return new TourPackageSourceBookingConfirmResult
        {
            Status = TourPackageBookingConfirmOutcomeStatus.Confirmed,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                bookingId = request.Booking.Id,
                bookingItemId = request.BookingItem.Id,
                offer.Id,
                offer.FlightId,
                offer.AirlineId,
                offer.FareClassId,
                offer.TotalPrice,
                offer.CurrencyCode,
                offer.ExpiresAt,
                requestedQuantity = request.ReservationItem.Quantity,
                holdToken = request.ReservationItem.SourceHoldToken
            }),
            Note = $"Confirmed {request.ReservationItem.Quantity} flight seat(s) from pooled flight inventory."
        };
    }
}
