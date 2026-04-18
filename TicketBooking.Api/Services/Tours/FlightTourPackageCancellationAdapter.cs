using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Flight;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class FlightTourPackageCancellationAdapter : ITourPackageSourceCancellationAdapter
{
    private readonly AppDbContext _db;

    public FlightTourPackageCancellationAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Flight;

    public async Task<TourPackageSourceCancellationResult> CancelAsync(
        TourPackageSourceCancellationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.BookingItem.SourceEntityId.HasValue || request.BookingItem.SourceEntityId.Value == Guid.Empty)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Flight booking item is missing SourceEntityId."
            };
        }

        var offer = await _db.FlightOffers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == request.BookingItem.SourceEntityId.Value,
                ct);

        if (offer is null)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Flight offer source was not found for cancellation."
            };
        }

        var flight = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == offer.FlightId &&
                !x.IsDeleted, ct);

        if (flight is null)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Flight source was not found for cancellation."
            };
        }

        if (flight.DepartureAt <= request.CurrentTime)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                ErrorMessage = "Flight has already departed and can no longer be cancelled."
            };
        }

        var targetOffer = await FlightOfferSupport.ResolveCanonicalOfferAsync(
            _db,
            offer.TenantId,
            offer.FlightId,
            offer.FareClassId,
            request.CurrentTime,
            asNoTracking: false,
            ct);

        targetOffer ??= offer;
        targetOffer.SeatsAvailable += Math.Max(request.BookingItem.Quantity, 0);
        targetOffer.UpdatedAt = request.CurrentTime;
        targetOffer.UpdatedByUserId = request.UserId;

        await _db.SaveChangesAsync(ct);

        return new TourPackageSourceCancellationResult
        {
            Status = TourPackageSourceCancellationOutcomeStatus.Cancelled,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                bookingItemId = request.BookingItem.Id,
                offerId = offer.Id,
                effectiveOfferId = targetOffer.Id,
                offerStatus = offer.Status,
                offerExpiresAt = offer.ExpiresAt,
                restoredQuantity = request.BookingItem.Quantity
            }),
            Note = $"Cancelled {request.BookingItem.Quantity} flight seat(s) and restored pooled flight inventory."
        };
    }
}
