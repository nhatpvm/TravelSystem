using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Flight;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class FlightTourPackageReservationAdapter : ITourPackageSourceReservationAdapter
{
    private readonly AppDbContext _db;

    public FlightTourPackageReservationAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Flight;

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
                ErrorMessage = "Flight reservation requires quantity greater than 0."
            };
        }

        var sourceEntityId = request.Line.BoundSourceEntityId;
        if (!sourceEntityId.HasValue || sourceEntityId.Value == Guid.Empty)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Flight reservation source is missing BoundSourceEntityId."
            };
        }

        var now = DateTimeOffset.Now;
        var offer = await _db.FlightOffers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == sourceEntityId.Value &&
                !x.IsDeleted, ct);

        if (offer is null)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Flight offer source was not found."
            };
        }

        if (offer.Status != OfferStatus.Active || offer.ExpiresAt <= now)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Flight offer source is no longer active."
            };
        }

        var flight = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == offer.FlightId &&
                !x.IsDeleted, ct);

        if (flight is null || !flight.IsActive || flight.Status != FlightStatus.Published || flight.DepartureAt <= now)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Flight source is not available for reservation."
            };
        }

        var canonicalOffer = await FlightOfferSupport.ResolveCanonicalOfferAsync(
            _db,
            offer.TenantId,
            offer.FlightId,
            offer.FareClassId,
            now,
            asNoTracking: false,
            ct);

        if (canonicalOffer is null || canonicalOffer.Status != OfferStatus.Active || canonicalOffer.ExpiresAt <= now)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = "Flight inventory source is no longer active."
            };
        }

        if (canonicalOffer.SeatsAvailable < request.Line.Quantity)
        {
            return new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Failed,
                ErrorMessage = $"Flight source only has {canonicalOffer.SeatsAvailable} seat(s) available."
            };
        }

        canonicalOffer.SeatsAvailable -= request.Line.Quantity;
        canonicalOffer.UpdatedAt = now;
        canonicalOffer.UpdatedByUserId = request.UserId;

        await _db.SaveChangesAsync(ct);

        var holdToken = $"pkg-flight-{Guid.NewGuid():N}";
        var holdExpiresAt = offer.ExpiresAt < canonicalOffer.ExpiresAt
            ? offer.ExpiresAt
            : canonicalOffer.ExpiresAt;

        return new TourPackageSourceReservationHoldResult
        {
            Status = TourPackageReservationHoldOutcomeStatus.Held,
            SourceHoldToken = holdToken,
            HoldExpiresAt = holdExpiresAt,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                offer.Id,
                effectiveOfferId = canonicalOffer.Id,
                offer.FlightId,
                offer.AirlineId,
                offer.FareClassId,
                offer.TotalPrice,
                offer.CurrencyCode,
                holdToken,
                holdExpiresAt,
                requestedQuantity = request.Line.Quantity,
                remainingSeatsAvailable = canonicalOffer.SeatsAvailable
            }),
            Note = $"Held {request.Line.Quantity} flight seat(s) from pooled flight inventory."
        };
    }

    public async Task ReleaseAsync(
        TourPackageSourceReservationReleaseRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Item.SourceEntityId.HasValue ||
            request.Item.SourceEntityId.Value == Guid.Empty ||
            request.Item.Quantity <= 0 ||
            request.Item.Status is not (TourPackageReservationItemStatus.Held or TourPackageReservationItemStatus.Validated))
        {
            return;
        }

        var sourceOffer = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.Item.SourceEntityId.Value,
                ct);

        if (sourceOffer is null)
            return;

        var now = DateTimeOffset.Now;
        var targetOffer = await FlightOfferSupport.ResolveCanonicalOfferAsync(
            _db,
            sourceOffer.TenantId,
            sourceOffer.FlightId,
            sourceOffer.FareClassId,
            now,
            asNoTracking: false,
            ct);

        if (targetOffer is null)
        {
            targetOffer = await _db.FlightOffers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == sourceOffer.Id, ct);
        }

        if (targetOffer is null)
            return;

        targetOffer.SeatsAvailable += request.Item.Quantity;
        targetOffer.UpdatedAt = now;
        targetOffer.UpdatedByUserId = request.UserId;

        await _db.SaveChangesAsync(ct);
    }
}
