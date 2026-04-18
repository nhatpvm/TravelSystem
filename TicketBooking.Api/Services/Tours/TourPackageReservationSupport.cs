using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPackageReservationSupport
{
    public static TourPackageReservationStatus CalculateReservationStatus(
        IReadOnlyCollection<TourPackageSourceReservationHoldResult> results,
        TourPackageHoldStrategy holdStrategy)
    {
        ArgumentNullException.ThrowIfNull(results);

        var failedCount = results.Count(x => x.Status == TourPackageReservationHoldOutcomeStatus.Failed);
        if (failedCount == 0)
            return TourPackageReservationStatus.Held;

        var successCount = results.Count(x =>
            x.Status == TourPackageReservationHoldOutcomeStatus.Held ||
            x.Status == TourPackageReservationHoldOutcomeStatus.Validated);

        if (successCount == 0)
            return TourPackageReservationStatus.Failed;

        return holdStrategy == TourPackageHoldStrategy.BestEffort
            ? TourPackageReservationStatus.PartiallyHeld
            : TourPackageReservationStatus.Failed;
    }

    public static DateTimeOffset ResolveReservationExpiry(
        DateTimeOffset now,
        IReadOnlyCollection<TourPackageSourceReservationHoldResult> results,
        int fallbackHoldMinutes)
    {
        ArgumentNullException.ThrowIfNull(results);

        var expiries = results
            .Where(x =>
                (x.Status == TourPackageReservationHoldOutcomeStatus.Held ||
                 x.Status == TourPackageReservationHoldOutcomeStatus.Validated) &&
                x.HoldExpiresAt.HasValue &&
                x.HoldExpiresAt.Value > now)
            .Select(x => x.HoldExpiresAt!.Value)
            .OrderBy(x => x)
            .ToList();

        if (expiries.Count > 0)
            return expiries[0];

        return now.AddMinutes(fallbackHoldMinutes > 0 ? fallbackHoldMinutes : 5);
    }

    public static bool IsSuccess(TourPackageSourceReservationHoldResult result)
        => result.Status == TourPackageReservationHoldOutcomeStatus.Held
           || result.Status == TourPackageReservationHoldOutcomeStatus.Validated;
}

public interface ITourPackageSourceReservationAdapter
{
    bool CanHandle(TourPackageSourceType sourceType);

    Task<TourPackageSourceReservationHoldResult> HoldAsync(
        TourPackageSourceReservationHoldRequest request,
        CancellationToken ct = default);

    Task ReleaseAsync(
        TourPackageSourceReservationReleaseRequest request,
        CancellationToken ct = default);
}

public sealed class TourPackageSourceReservationHoldRequest
{
    public Guid ReservationId { get; set; }
    public string ReservationToken { get; set; } = "";
    public Guid? UserId { get; set; }
    public Tour Tour { get; set; } = null!;
    public TourSchedule Schedule { get; set; } = null!;
    public TourPackage Package { get; set; } = null!;
    public TourPackageComponent Component { get; set; } = null!;
    public TourPackageComponentOption Option { get; set; } = null!;
    public TourQuoteBuildPackageLineInput Line { get; set; } = null!;
    public int TotalNights { get; set; }
}

public sealed class TourPackageSourceReservationReleaseRequest
{
    public Guid? UserId { get; set; }
    public bool IsAdmin { get; set; }
    public TourPackageReservation Reservation { get; set; } = null!;
    public TourPackageReservationItem Item { get; set; } = null!;
}

public sealed class TourPackageSourceReservationHoldResult
{
    public TourPackageReservationHoldOutcomeStatus Status { get; set; }
    public string? SourceHoldToken { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public string? SnapshotJson { get; set; }
    public string? Note { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TourPackageReservationHoldOutcomeStatus
{
    Held = 1,
    Validated = 2,
    Failed = 3
}
