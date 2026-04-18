using System.Text.Json;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public static class TourPackageRescheduleSupport
{
    public static bool IsBookingEligibleForReschedule(TourPackageBooking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return booking.Status is TourPackageBookingStatus.Confirmed or TourPackageBookingStatus.PartiallyConfirmed;
    }

    public static bool IsActive(TourPackageRescheduleStatus status)
        => status is TourPackageRescheduleStatus.Held or TourPackageRescheduleStatus.Confirming;

    public static bool CanRelease(TourPackageRescheduleStatus status)
        => status == TourPackageRescheduleStatus.Held;

    public static List<TourPackageReservationSelectedOptionRequest> BuildSelectedOptionsFromBooking(
        IReadOnlyCollection<TourPackageBookingItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .Where(x =>
                !x.IsDeleted &&
                x.Status is TourPackageBookingItemStatus.Confirmed or TourPackageBookingItemStatus.Pending)
            .GroupBy(x => x.TourPackageComponentOptionId)
            .Select(g => g
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.CreatedAt)
                .First())
            .Select(x => new TourPackageReservationSelectedOptionRequest
            {
                OptionId = x.TourPackageComponentOptionId,
                Quantity = x.Quantity
            })
            .OrderBy(x => x.OptionId)
            .ToList();
    }

    public static string BuildSnapshotJson(
        TourPackageBooking sourceBooking,
        TourPackageReservation targetReservation,
        IReadOnlyCollection<TourPackageReservationSelectedOptionRequest> selectedOptions)
    {
        return JsonSerializer.Serialize(new
        {
            sourceBookingId = sourceBooking.Id,
            sourceBookingCode = sourceBooking.Code,
            sourceScheduleId = sourceBooking.TourScheduleId,
            sourcePackageId = sourceBooking.TourPackageId,
            sourceSubtotal = sourceBooking.PackageSubtotalAmount,
            targetReservationId = targetReservation.Id,
            targetReservationCode = targetReservation.Code,
            targetScheduleId = targetReservation.TourScheduleId,
            targetPackageId = targetReservation.TourPackageId,
            targetSubtotal = targetReservation.PackageSubtotalAmount,
            selectedOptions = selectedOptions.Select(x => new
            {
                x.OptionId,
                x.Quantity
            }).ToList()
        });
    }

    public static string BuildResolutionSnapshotJson(
        TourPackageReschedule reschedule,
        TourPackageBooking targetBooking,
        TourPackageCancellationView? sourceCancellation)
    {
        return JsonSerializer.Serialize(new
        {
            rescheduleId = reschedule.Id,
            rescheduleCode = reschedule.Code,
            rescheduleStatus = reschedule.Status,
            targetBookingId = targetBooking.Id,
            targetBookingCode = targetBooking.Code,
            targetBookingStatus = targetBooking.Status,
            sourceCancellationId = sourceCancellation?.Id,
            sourceCancellationStatus = sourceCancellation?.Status,
            sourceCancellationRefundAmount = sourceCancellation?.RefundAmount,
            sourceCancellationFailureReason = sourceCancellation?.FailureReason
        });
    }
}
