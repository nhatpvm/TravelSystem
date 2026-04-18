using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPackageBookingSupport
{
    public static TourPackageBookingStatus CalculateBookingStatus(
        IReadOnlyCollection<TourPackageSourceBookingConfirmResult> results,
        TourPackageHoldStrategy holdStrategy)
    {
        ArgumentNullException.ThrowIfNull(results);

        var failedCount = results.Count(x => x.Status == TourPackageBookingConfirmOutcomeStatus.Failed);
        if (failedCount == 0)
            return TourPackageBookingStatus.Confirmed;

        var successCount = results.Count(x => x.Status == TourPackageBookingConfirmOutcomeStatus.Confirmed);
        if (successCount == 0)
            return TourPackageBookingStatus.Failed;

        return holdStrategy == TourPackageHoldStrategy.BestEffort
            ? TourPackageBookingStatus.PartiallyConfirmed
            : TourPackageBookingStatus.Failed;
    }

    public static bool IsSuccess(TourPackageSourceBookingConfirmResult result)
        => result.Status == TourPackageBookingConfirmOutcomeStatus.Confirmed;
}

public interface ITourPackageSourceBookingAdapter
{
    bool CanHandle(TourPackageSourceType sourceType);

    Task<TourPackageSourceBookingConfirmResult> ConfirmAsync(
        TourPackageSourceBookingConfirmRequest request,
        CancellationToken ct = default);
}

public sealed class TourPackageSourceBookingConfirmRequest
{
    public Guid? UserId { get; set; }
    public bool IsAdmin { get; set; }
    public Tour Tour { get; set; } = null!;
    public TourSchedule Schedule { get; set; } = null!;
    public TourPackage Package { get; set; } = null!;
    public TourPackageReservation Reservation { get; set; } = null!;
    public TourPackageReservationItem ReservationItem { get; set; } = null!;
    public TourPackageBooking Booking { get; set; } = null!;
    public TourPackageBookingItem BookingItem { get; set; } = null!;
}

public sealed class TourPackageSourceBookingConfirmResult
{
    public TourPackageBookingConfirmOutcomeStatus Status { get; set; }
    public string? SnapshotJson { get; set; }
    public string? Note { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TourPackageBookingConfirmOutcomeStatus
{
    Confirmed = 1,
    Failed = 2
}
