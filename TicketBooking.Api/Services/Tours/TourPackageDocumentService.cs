using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageDocumentService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TourPackageDocumentService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<TourPackageCustomerItineraryView> GetItineraryAsync(
        Guid tourId,
        Guid bookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var context = await LoadDocumentContextAsync(tourId, bookingId, userId, isAdmin, ct);
        return BuildItinerary(context);
    }

    public async Task<TourPackageCustomerVoucherView> GetVoucherAsync(
        Guid tourId,
        Guid bookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var context = await LoadDocumentContextAsync(tourId, bookingId, userId, isAdmin, ct);
        return BuildVoucher(context);
    }

    private async Task<TourPackageDocumentContext> LoadDocumentContextAsync(
        Guid tourId,
        Guid bookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var booking = await _db.TourPackageBookings
                .IgnoreQueryFilters()
                .Include(x => x.TourPackage)
                .Include(x => x.TourSchedule)
                .Include(x => x.TourPackageReservation)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == bookingId &&
                    x.TourId == tourId &&
                    !x.IsDeleted, ct);

            if (booking is null)
                throw new KeyNotFoundException("Package booking not found.");

            if (!isAdmin && userId.HasValue && booking.UserId.HasValue && booking.UserId.Value != userId.Value)
                throw new KeyNotFoundException("Package booking not found.");

            if (!isAdmin && booking.UserId.HasValue == false)
                throw new KeyNotFoundException("Package booking not found.");

            _tenantContext.SetTenant(booking.TenantId);

            var tour = await _db.Tours
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.Id == booking.TourId &&
                    x.TenantId == booking.TenantId &&
                    !x.IsDeleted, ct);

            if (tour is null)
                throw new KeyNotFoundException("Tour not found.");

            var itineraryDays = await _db.TourItineraryDays
                .IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == booking.TenantId &&
                    x.TourId == booking.TourId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderBy(x => x.DayNumber)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToListAsync(ct);

            var dayIds = itineraryDays.Select(x => x.Id).ToList();
            var itineraryItems = dayIds.Count == 0
                ? new List<TourItineraryItem>()
                : await _db.TourItineraryItems
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.TenantId == booking.TenantId &&
                        dayIds.Contains(x.TourItineraryDayId) &&
                        x.IsActive &&
                        !x.IsDeleted)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.StartTime)
                    .ThenBy(x => x.Title)
                    .ToListAsync(ct);

            var contacts = await _db.TourContacts
                .IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == booking.TenantId &&
                    x.TourId == booking.TourId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);

            var pickupPoints = await _db.TourPickupPoints
                .IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == booking.TenantId &&
                    x.TourId == booking.TourId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);

            var dropoffPoints = await _db.TourDropoffPoints
                .IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == booking.TenantId &&
                    x.TourId == booking.TourId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);

            var policies = await _db.TourPolicies
                .IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == booking.TenantId &&
                    x.TourId == booking.TourId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.IsHighlighted)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(ct);

            var reschedules = await _db.TourPackageReschedules
                .IgnoreQueryFilters()
                .Include(x => x.SourceTourSchedule)
                .Include(x => x.TargetTourSchedule)
                .Include(x => x.TargetTourPackageBooking)
                .Include(x => x.SourceTourPackageCancellation)
                .Where(x =>
                    x.TenantId == booking.TenantId &&
                    !x.IsDeleted &&
                    (x.SourceTourPackageBookingId == bookingId || x.TargetTourPackageBookingId == bookingId))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            return new TourPackageDocumentContext
            {
                Tour = tour,
                Booking = booking,
                ItineraryDays = itineraryDays,
                ItineraryItems = itineraryItems,
                Contacts = contacts,
                PickupPoints = pickupPoints,
                DropoffPoints = dropoffPoints,
                Policies = policies,
                Reschedules = reschedules
            };
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private static TourPackageCustomerItineraryView BuildItinerary(TourPackageDocumentContext context)
    {
        var itemsByDay = context.ItineraryItems
            .GroupBy(x => x.TourItineraryDayId)
            .ToDictionary(x => x.Key, x => x.ToList());

        return new TourPackageCustomerItineraryView
        {
            TourId = context.Tour.Id,
            TourCode = context.Tour.Code,
            TourName = context.Tour.Name,
            BookingId = context.Booking.Id,
            BookingCode = context.Booking.Code,
            BookingStatus = context.Booking.Status,
            PackageId = context.Booking.TourPackageId,
            PackageCode = context.Booking.TourPackage?.Code ?? string.Empty,
            PackageName = context.Booking.TourPackage?.Name ?? string.Empty,
            PackageMode = context.Booking.TourPackage?.Mode ?? TourPackageMode.Fixed,
            ScheduleId = context.Booking.TourScheduleId,
            ScheduleCode = context.Booking.TourSchedule?.Code ?? string.Empty,
            ScheduleName = context.Booking.TourSchedule?.Name,
            DepartureDate = context.Booking.TourSchedule?.DepartureDate ?? default,
            ReturnDate = context.Booking.TourSchedule?.ReturnDate ?? default,
            DepartureTime = context.Booking.TourSchedule?.DepartureTime,
            ReturnTime = context.Booking.TourSchedule?.ReturnTime,
            RequestedPax = context.Booking.RequestedPax,
            ConfirmedCapacitySlots = context.Booking.ConfirmedCapacitySlots,
            CurrencyCode = context.Booking.CurrencyCode,
            PackageSubtotalAmount = context.Booking.PackageSubtotalAmount,
            ConfirmedAt = context.Booking.ConfirmedAt,
            MeetingPointSummary = context.Booking.TourSchedule?.MeetingPointSummary ?? context.Tour.MeetingPointSummary,
            PickupSummary = context.Booking.TourSchedule?.PickupSummary,
            DropoffSummary = context.Booking.TourSchedule?.DropoffSummary,
            Warnings = BuildWarnings(context),
            Contacts = context.Contacts.Select(MapContact).ToList(),
            PickupPoints = context.PickupPoints.Select(MapPickupPoint).ToList(),
            DropoffPoints = context.DropoffPoints.Select(MapDropoffPoint).ToList(),
            Services = context.Booking.Items
                .Where(x => !x.IsDeleted && x.Status != TourPackageBookingItemStatus.Failed)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(MapServiceLine)
                .ToList(),
            Days = context.ItineraryDays.Select(day =>
            {
                itemsByDay.TryGetValue(day.Id, out var dayItems);
                dayItems ??= new List<TourItineraryItem>();

                return new TourPackageCustomerItineraryDayView
                {
                    DayId = day.Id,
                    DayNumber = day.DayNumber,
                    Title = day.Title,
                    ShortDescription = day.ShortDescription,
                    DescriptionMarkdown = day.DescriptionMarkdown,
                    DescriptionHtml = day.DescriptionHtml,
                    StartLocation = day.StartLocation,
                    EndLocation = day.EndLocation,
                    AccommodationName = day.AccommodationName,
                    IncludesBreakfast = day.IncludesBreakfast,
                    IncludesLunch = day.IncludesLunch,
                    IncludesDinner = day.IncludesDinner,
                    TransportationSummary = day.TransportationSummary,
                    Notes = day.Notes,
                    Items = dayItems.Select(MapItineraryItem).ToList()
                };
            }).ToList(),
            Reschedules = context.Reschedules.Select(MapRescheduleSummary).ToList()
        };
    }

    private static TourPackageCustomerVoucherView BuildVoucher(TourPackageDocumentContext context)
    {
        var supportContacts = context.Contacts
            .Where(x => x.ContactType is TourContactType.Hotline or TourContactType.Support or TourContactType.Operation or TourContactType.General)
            .Select(MapContact)
            .ToList();

        if (supportContacts.Count == 0)
            supportContacts = context.Contacts.Select(MapContact).ToList();

        return new TourPackageCustomerVoucherView
        {
            TourId = context.Tour.Id,
            TourCode = context.Tour.Code,
            TourName = context.Tour.Name,
            BookingId = context.Booking.Id,
            BookingCode = context.Booking.Code,
            VoucherNumber = context.Booking.Code,
            BookingStatus = context.Booking.Status,
            PackageId = context.Booking.TourPackageId,
            PackageCode = context.Booking.TourPackage?.Code ?? string.Empty,
            PackageName = context.Booking.TourPackage?.Name ?? string.Empty,
            PackageMode = context.Booking.TourPackage?.Mode ?? TourPackageMode.Fixed,
            ScheduleId = context.Booking.TourScheduleId,
            ScheduleCode = context.Booking.TourSchedule?.Code ?? string.Empty,
            ScheduleName = context.Booking.TourSchedule?.Name,
            DepartureDate = context.Booking.TourSchedule?.DepartureDate ?? default,
            ReturnDate = context.Booking.TourSchedule?.ReturnDate ?? default,
            DepartureTime = context.Booking.TourSchedule?.DepartureTime,
            ReturnTime = context.Booking.TourSchedule?.ReturnTime,
            RequestedPax = context.Booking.RequestedPax,
            ConfirmedCapacitySlots = context.Booking.ConfirmedCapacitySlots,
            CurrencyCode = context.Booking.CurrencyCode,
            PackageSubtotalAmount = context.Booking.PackageSubtotalAmount,
            ConfirmedAt = context.Booking.ConfirmedAt,
            IssuedAt = DateTimeOffset.UtcNow,
            Summary = new TourPackageCustomerVoucherSummaryView
            {
                MeetingPointSummary = context.Booking.TourSchedule?.MeetingPointSummary ?? context.Tour.MeetingPointSummary,
                PickupSummary = context.Booking.TourSchedule?.PickupSummary,
                DropoffSummary = context.Booking.TourSchedule?.DropoffSummary,
                ScheduleNotes = context.Booking.TourSchedule?.Notes,
                BookingNotes = context.Booking.Notes
            },
            IncludedItems = ParseJsonStringList(context.Tour.IncludesJson),
            ExcludedItems = ParseJsonStringList(context.Tour.ExcludesJson),
            Terms = ParseJsonStringList(context.Tour.TermsJson),
            Highlights = ParseJsonStringList(context.Tour.HighlightsJson),
            Policies = context.Policies
                .Where(x => x.Type is TourPolicyType.General or TourPolicyType.Booking or TourPolicyType.Cancellation or TourPolicyType.ChangeDate or TourPolicyType.Payment)
                .Select(MapPolicy)
                .ToList(),
            PickupPoints = context.PickupPoints.Select(MapPickupPoint).ToList(),
            DropoffPoints = context.DropoffPoints.Select(MapDropoffPoint).ToList(),
            Contacts = supportContacts,
            ServiceLines = context.Booking.Items
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(MapServiceLine)
                .ToList(),
            Warnings = BuildWarnings(context),
            Reschedules = context.Reschedules.Select(MapRescheduleSummary).ToList()
        };
    }

    private static TourPackageCustomerServiceLineView MapServiceLine(TourPackageBookingItem item)
    {
        var snapshot = ParseJsonObject(item.SnapshotJson);
        return new TourPackageCustomerServiceLineView
        {
            BookingItemId = item.Id,
            ComponentId = item.TourPackageComponentId,
            OptionId = item.TourPackageComponentOptionId,
            ComponentType = item.ComponentType,
            SourceType = item.SourceType,
            Status = item.Status,
            Quantity = item.Quantity,
            CurrencyCode = item.CurrencyCode,
            UnitPrice = item.UnitPrice,
            LineAmount = item.LineAmount,
            SourceEntityId = item.SourceEntityId,
            SourceHoldToken = item.SourceHoldToken,
            Title = snapshot.TryGetValue("name", out var name)
                ? name
                : snapshot.TryGetValue("sourceName", out var sourceName)
                    ? sourceName
                    : item.ComponentType.ToString(),
            ConfirmationReference = snapshot.TryGetValue("correlationId", out var correlationId)
                ? correlationId
                : item.SourceHoldToken,
            Notes = item.Notes,
            ErrorMessage = item.ErrorMessage,
            SourceSummary = BuildSourceSummary(item.SourceType, snapshot),
            SnapshotJson = item.SnapshotJson
        };
    }

    private static TourPackageCustomerItineraryItemView MapItineraryItem(TourItineraryItem item)
    {
        return new TourPackageCustomerItineraryItemView
        {
            Id = item.Id,
            Type = item.Type,
            Title = item.Title,
            ShortDescription = item.ShortDescription,
            DescriptionMarkdown = item.DescriptionMarkdown,
            DescriptionHtml = item.DescriptionHtml,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            LocationName = item.LocationName,
            AddressLine = item.AddressLine,
            TransportationMode = item.TransportationMode,
            IncludesTicket = item.IncludesTicket,
            IncludesMeal = item.IncludesMeal,
            IsOptional = item.IsOptional,
            RequiresAdditionalFee = item.RequiresAdditionalFee,
            Notes = item.Notes
        };
    }

    private static TourPackageCustomerContactView MapContact(TourContact contact)
    {
        return new TourPackageCustomerContactView
        {
            Id = contact.Id,
            Name = contact.Name,
            Title = contact.Title,
            Department = contact.Department,
            Phone = contact.Phone,
            Email = contact.Email,
            ContactType = contact.ContactType,
            IsPrimary = contact.IsPrimary,
            Notes = contact.Notes
        };
    }

    private static TourPackageCustomerPickupPointView MapPickupPoint(TourPickupPoint point)
    {
        return new TourPackageCustomerPickupPointView
        {
            Id = point.Id,
            Code = point.Code,
            Name = point.Name,
            AddressLine = point.AddressLine,
            Ward = point.Ward,
            District = point.District,
            Province = point.Province,
            CountryCode = point.CountryCode,
            PickupTime = point.PickupTime,
            IsDefault = point.IsDefault,
            Notes = point.Notes
        };
    }

    private static TourPackageCustomerDropoffPointView MapDropoffPoint(TourDropoffPoint point)
    {
        return new TourPackageCustomerDropoffPointView
        {
            Id = point.Id,
            Code = point.Code,
            Name = point.Name,
            AddressLine = point.AddressLine,
            Ward = point.Ward,
            District = point.District,
            Province = point.Province,
            CountryCode = point.CountryCode,
            DropoffTime = point.DropoffTime,
            IsDefault = point.IsDefault,
            Notes = point.Notes
        };
    }

    private static TourPackageCustomerPolicyView MapPolicy(TourPolicy policy)
    {
        return new TourPackageCustomerPolicyView
        {
            Id = policy.Id,
            Code = policy.Code,
            Name = policy.Name,
            Type = policy.Type,
            ShortDescription = policy.ShortDescription,
            DescriptionMarkdown = policy.DescriptionMarkdown,
            DescriptionHtml = policy.DescriptionHtml,
            PolicyJson = policy.PolicyJson,
            IsHighlighted = policy.IsHighlighted
        };
    }

    private static TourPackageCustomerRescheduleSummaryView MapRescheduleSummary(TourPackageReschedule reschedule)
    {
        return new TourPackageCustomerRescheduleSummaryView
        {
            Id = reschedule.Id,
            Code = reschedule.Code,
            Status = reschedule.Status,
            SourceScheduleId = reschedule.SourceTourScheduleId,
            SourceScheduleCode = reschedule.SourceTourSchedule?.Code ?? string.Empty,
            TargetScheduleId = reschedule.TargetTourScheduleId,
            TargetScheduleCode = reschedule.TargetTourSchedule?.Code ?? string.Empty,
            TargetBookingId = reschedule.TargetTourPackageBookingId,
            SourceCancellationId = reschedule.SourceTourPackageCancellationId,
            PriceDifferenceAmount = reschedule.PriceDifferenceAmount,
            CurrencyCode = reschedule.CurrencyCode,
            ConfirmedAt = reschedule.ConfirmedAt,
            FailureReason = reschedule.FailureReason
        };
    }

    private static List<string> BuildWarnings(TourPackageDocumentContext context)
    {
        var warnings = new List<string>();

        if (context.Booking.Status == TourPackageBookingStatus.PartiallyCancelled)
            warnings.Add("Some package services were cancelled after booking confirmation.");
        else if (context.Booking.Status == TourPackageBookingStatus.Cancelled)
            warnings.Add("This package booking has been cancelled and is kept for historical reference.");

        if (context.Booking.Items.Any(x => x.Status == TourPackageBookingItemStatus.RefundPending))
            warnings.Add("One or more services are waiting for refund processing.");

        if (context.Reschedules.Any(x => x.Status == TourPackageRescheduleStatus.AttentionRequired))
            warnings.Add("A reschedule request requires operator follow-up before the package is fully settled.");

        if (context.Reschedules.Any(x =>
                x.TargetTourPackageBookingId == context.Booking.Id &&
                x.Status is TourPackageRescheduleStatus.Completed or TourPackageRescheduleStatus.AttentionRequired))
        {
            warnings.Add("This booking was created from a reschedule request.");
        }

        return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static Dictionary<string, string> ParseJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                result[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => property.Value.GetRawText()
                };
            }

            return result;
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static List<string> ParseJsonStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return new List<string> { json.Trim() };

            var result = new List<string>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value.Trim());
                    continue;
                }

                if (item.ValueKind == JsonValueKind.Object)
                {
                    var candidate = TryGetFirstText(item, "title")
                        ?? TryGetFirstText(item, "name")
                        ?? TryGetFirstText(item, "text")
                        ?? TryGetFirstText(item, "label")
                        ?? TryGetFirstText(item, "value")
                        ?? TryGetFirstText(item, "description");

                    if (!string.IsNullOrWhiteSpace(candidate))
                        result.Add(candidate.Trim());
                }
            }

            return result;
        }
        catch (JsonException)
        {
            return new List<string> { json.Trim() };
        }
    }

    private static string? TryGetFirstText(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        return value.GetString();
    }

    private static string? BuildSourceSummary(
        TourPackageSourceType sourceType,
        IReadOnlyDictionary<string, string> snapshot)
    {
        return sourceType switch
        {
            TourPackageSourceType.Flight => BuildFlightSummary(snapshot),
            TourPackageSourceType.Hotel => BuildHotelSummary(snapshot),
            TourPackageSourceType.Bus => BuildBusOrTrainSummary("Bus", snapshot),
            TourPackageSourceType.Train => BuildBusOrTrainSummary("Train", snapshot),
            _ => snapshot.TryGetValue("note", out var note) ? note : null
        };
    }

    private static string? BuildFlightSummary(IReadOnlyDictionary<string, string> snapshot)
    {
        var parts = new List<string>();
        if (snapshot.TryGetValue("FlightId", out var flightId))
            parts.Add($"Flight {flightId}");
        if (snapshot.TryGetValue("FareClassId", out var fareClassId))
            parts.Add($"FareClass {fareClassId}");
        if (snapshot.TryGetValue("requestedQuantity", out var quantity))
            parts.Add($"Qty {quantity}");

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static string? BuildHotelSummary(IReadOnlyDictionary<string, string> snapshot)
    {
        var parts = new List<string>();
        if (snapshot.TryGetValue("checkInDate", out var checkIn))
            parts.Add($"Check-in {checkIn}");
        if (snapshot.TryGetValue("checkOutDate", out var checkOut))
            parts.Add($"Check-out {checkOut}");
        if (snapshot.TryGetValue("units", out var units))
            parts.Add($"Units {units}");

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static string? BuildBusOrTrainSummary(string mode, IReadOnlyDictionary<string, string> snapshot)
    {
        var parts = new List<string>();
        if (snapshot.TryGetValue("holdIds", out var holdIds))
            parts.Add($"HoldIds {holdIds}");
        if (snapshot.TryGetValue("seatIds", out var seatIds))
            parts.Add($"Seats {seatIds}");

        return parts.Count == 0 ? $"{mode} service confirmed." : $"{mode}: {string.Join(" | ", parts)}";
    }

    private sealed class TourPackageDocumentContext
    {
        public Tour Tour { get; init; } = null!;
        public TourPackageBooking Booking { get; init; } = null!;
        public List<TourItineraryDay> ItineraryDays { get; init; } = new();
        public List<TourItineraryItem> ItineraryItems { get; init; } = new();
        public List<TourContact> Contacts { get; init; } = new();
        public List<TourPickupPoint> PickupPoints { get; init; } = new();
        public List<TourDropoffPoint> DropoffPoints { get; init; } = new();
        public List<TourPolicy> Policies { get; init; } = new();
        public List<TourPackageReschedule> Reschedules { get; init; } = new();
    }
}
