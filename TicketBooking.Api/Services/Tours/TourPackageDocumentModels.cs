using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageCustomerItineraryView
{
    public Guid TourId { get; set; }
    public string TourCode { get; set; } = string.Empty;
    public string TourName { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public TourPackageBookingStatus BookingStatus { get; set; }
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public TourPackageMode PackageMode { get; set; }
    public Guid ScheduleId { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string? ScheduleName { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public int RequestedPax { get; set; }
    public int ConfirmedCapacitySlots { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal PackageSubtotalAmount { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? MeetingPointSummary { get; set; }
    public string? PickupSummary { get; set; }
    public string? DropoffSummary { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<TourPackageCustomerContactView> Contacts { get; set; } = new();
    public List<TourPackageCustomerPickupPointView> PickupPoints { get; set; } = new();
    public List<TourPackageCustomerDropoffPointView> DropoffPoints { get; set; } = new();
    public List<TourPackageCustomerServiceLineView> Services { get; set; } = new();
    public List<TourPackageCustomerItineraryDayView> Days { get; set; } = new();
    public List<TourPackageCustomerRescheduleSummaryView> Reschedules { get; set; } = new();
}

public sealed class TourPackageCustomerVoucherView
{
    public Guid TourId { get; set; }
    public string TourCode { get; set; } = string.Empty;
    public string TourName { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string VoucherNumber { get; set; } = string.Empty;
    public TourPackageBookingStatus BookingStatus { get; set; }
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public TourPackageMode PackageMode { get; set; }
    public Guid ScheduleId { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string? ScheduleName { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public int RequestedPax { get; set; }
    public int ConfirmedCapacitySlots { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal PackageSubtotalAmount { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public TourPackageCustomerVoucherSummaryView Summary { get; set; } = new();
    public List<string> IncludedItems { get; set; } = new();
    public List<string> ExcludedItems { get; set; } = new();
    public List<string> Terms { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public List<TourPackageCustomerPolicyView> Policies { get; set; } = new();
    public List<TourPackageCustomerPickupPointView> PickupPoints { get; set; } = new();
    public List<TourPackageCustomerDropoffPointView> DropoffPoints { get; set; } = new();
    public List<TourPackageCustomerContactView> Contacts { get; set; } = new();
    public List<TourPackageCustomerServiceLineView> ServiceLines { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<TourPackageCustomerRescheduleSummaryView> Reschedules { get; set; } = new();
}

public sealed class TourPackageCustomerVoucherSummaryView
{
    public string? MeetingPointSummary { get; set; }
    public string? PickupSummary { get; set; }
    public string? DropoffSummary { get; set; }
    public string? ScheduleNotes { get; set; }
    public string? BookingNotes { get; set; }
}

public sealed class TourPackageCustomerItineraryDayView
{
    public Guid DayId { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public string? AccommodationName { get; set; }
    public bool IncludesBreakfast { get; set; }
    public bool IncludesLunch { get; set; }
    public bool IncludesDinner { get; set; }
    public string? TransportationSummary { get; set; }
    public string? Notes { get; set; }
    public List<TourPackageCustomerItineraryItemView> Items { get; set; } = new();
}

public sealed class TourPackageCustomerItineraryItemView
{
    public Guid Id { get; set; }
    public TourItineraryItemType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? LocationName { get; set; }
    public string? AddressLine { get; set; }
    public string? TransportationMode { get; set; }
    public bool IncludesTicket { get; set; }
    public bool IncludesMeal { get; set; }
    public bool IsOptional { get; set; }
    public bool RequiresAdditionalFee { get; set; }
    public string? Notes { get; set; }
}

public sealed class TourPackageCustomerServiceLineView
{
    public Guid BookingItemId { get; set; }
    public Guid ComponentId { get; set; }
    public Guid OptionId { get; set; }
    public TourPackageComponentType ComponentType { get; set; }
    public TourPackageSourceType SourceType { get; set; }
    public TourPackageBookingItemStatus Status { get; set; }
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string? SourceHoldToken { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ConfirmationReference { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SourceSummary { get; set; }
    public string? SnapshotJson { get; set; }
}

public sealed class TourPackageCustomerContactView
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TourContactType ContactType { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
}

public sealed class TourPackageCustomerPickupPointView
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public TimeOnly? PickupTime { get; set; }
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
}

public sealed class TourPackageCustomerDropoffPointView
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public TimeOnly? DropoffTime { get; set; }
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
}

public sealed class TourPackageCustomerPolicyView
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TourPolicyType Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? PolicyJson { get; set; }
    public bool IsHighlighted { get; set; }
}

public sealed class TourPackageCustomerRescheduleSummaryView
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public TourPackageRescheduleStatus Status { get; set; }
    public Guid SourceScheduleId { get; set; }
    public string SourceScheduleCode { get; set; } = string.Empty;
    public Guid TargetScheduleId { get; set; }
    public string TargetScheduleCode { get; set; } = string.Empty;
    public Guid? TargetBookingId { get; set; }
    public Guid? SourceCancellationId { get; set; }
    public decimal? PriceDifferenceAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? FailureReason { get; set; }
}
