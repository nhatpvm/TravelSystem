using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageDocumentServiceTests
{
    [Fact]
    public async Task GetItineraryAsync_ReturnsCustomerFacingScheduleServicesAndWarnings()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedAsync(context);
        var service = new TourPackageDocumentService(context, tenantContext);

        var result = await service.GetItineraryAsync(
            seeded.TourId,
            seeded.BookingId,
            seeded.UserId,
            isAdmin: false);

        Assert.Equal(seeded.TourId, result.TourId);
        Assert.Equal(seeded.BookingId, result.BookingId);
        Assert.Equal("Tour Combo Da Nang", result.TourName);
        Assert.Equal("PKG-DOC", result.PackageCode);
        Assert.Equal("SCH-DOC", result.ScheduleCode);
        Assert.Equal(2, result.Days.Count);
        Assert.Equal("Ngay 1 - Den Da Nang", result.Days[0].Title);
        Assert.Single(result.Days[0].Items);
        Assert.Equal(2, result.Services.Count);
        Assert.Contains(result.Services, x => x.Title == "Khach san 4 sao");
        Assert.Contains(result.Services, x => (x.SourceSummary ?? string.Empty).Contains("Check-in 2026-04-10", StringComparison.Ordinal));
        Assert.Equal(2, result.Contacts.Count);
        Assert.Equal("San bay Da Nang", result.PickupPoints[0].Name);
        Assert.Equal("Trung tam thanh pho", result.DropoffPoints[0].Name);
        Assert.Single(result.Reschedules);
        Assert.Contains(result.Warnings, x => x.Contains("cancelled after booking confirmation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, x => x.Contains("refund processing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, x => x.Contains("created from a reschedule", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetVoucherAsync_ReturnsSupportContactsPoliciesAndCommercialSections()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedAsync(context);
        var service = new TourPackageDocumentService(context, tenantContext);

        var result = await service.GetVoucherAsync(
            seeded.TourId,
            seeded.BookingId,
            seeded.UserId,
            isAdmin: false);

        Assert.Equal(seeded.BookingId, result.BookingId);
        Assert.Equal("TPB-DOC", result.VoucherNumber);
        Assert.Equal("Gap tai san bay luc 08:00", result.Summary.MeetingPointSummary);
        Assert.Equal(3, result.ServiceLines.Count);
        Assert.Single(result.Contacts);
        Assert.Equal(TourContactType.Hotline, result.Contacts[0].ContactType);
        Assert.Equal(3, result.IncludedItems.Count);
        Assert.Contains("Ve tham quan Ba Na", result.IncludedItems);
        Assert.Contains("Khong bao gom chi phi ca nhan", result.ExcludedItems);
        Assert.Contains("Mang theo CCCD/Passport", result.Terms);
        Assert.Contains("Don san bay", result.Highlights);
        Assert.Equal(2, result.Policies.Count);
        Assert.DoesNotContain(result.Policies, x => x.Type == TourPolicyType.Child);
        Assert.Contains(result.Warnings, x => x.Contains("refund processing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetItineraryAsync_ForForeignUser_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedAsync(context);
        var service = new TourPackageDocumentService(context, tenantContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItineraryAsync(
            seeded.TourId,
            seeded.BookingId,
            Guid.NewGuid(),
            isAdmin: false));
    }

    private static AppDbContext CreateDbContext(out TenantContext tenantContext)
    {
        tenantContext = new TenantContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private static async Task<DocumentSeedContext> SeedAsync(AppDbContext context)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        var tourId = Guid.NewGuid();
        var sourceScheduleId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var sourceReservationId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var sourceBookingId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var itineraryDay1Id = Guid.NewGuid();
        var itineraryDay2Id = Guid.NewGuid();

        context.Tours.Add(new Tour
        {
            Id = tourId,
            TenantId = tenantId,
            Code = "TOUR-DOC",
            Name = "Tour Combo Da Nang",
            Slug = "tour-combo-da-nang",
            Status = TourStatus.Active,
            Type = TourType.Combo,
            CurrencyCode = "VND",
            DurationDays = 3,
            DurationNights = 2,
            MeetingPointSummary = "Gap tai san bay luc 08:00",
            IncludesJson = """["Xe dua don", "Khach san 4 sao", {"title":"Ve tham quan Ba Na"}]""",
            ExcludesJson = """["Khong bao gom chi phi ca nhan"]""",
            TermsJson = """["Mang theo CCCD/Passport"]""",
            HighlightsJson = """["Don san bay", {"name":"Huong dan vien dia phuong"}]""",
            IsActive = true,
            CreatedAt = now
        });

        context.TourSchedules.AddRange(
            new TourSchedule
            {
                Id = sourceScheduleId,
                TenantId = tenantId,
                TourId = tourId,
                Code = "SCH-DOC-OLD",
                Name = "Khoi hanh cu",
                DepartureDate = new DateOnly(2026, 4, 8),
                ReturnDate = new DateOnly(2026, 4, 10),
                DepartureTime = new TimeOnly(7, 30),
                ReturnTime = new TimeOnly(17, 30),
                Status = TourScheduleStatus.Open,
                MeetingPointSummary = "Gap tai san bay luc 07:30",
                PickupSummary = "Don tai san bay Da Nang",
                DropoffSummary = "Tra khach tai trung tam Da Nang",
                Notes = "Lich cu",
                IsActive = true,
                CreatedAt = now
            },
            new TourSchedule
            {
                Id = scheduleId,
                TenantId = tenantId,
                TourId = tourId,
                Code = "SCH-DOC",
                Name = "Khoi hanh Thang 4",
                DepartureDate = new DateOnly(2026, 4, 10),
                ReturnDate = new DateOnly(2026, 4, 12),
                DepartureTime = new TimeOnly(8, 0),
                ReturnTime = new TimeOnly(18, 0),
                Status = TourScheduleStatus.Open,
                MeetingPointSummary = "Gap tai san bay luc 08:00",
                PickupSummary = "Don tai san bay Da Nang",
                DropoffSummary = "Tra khach tai trung tam Da Nang",
                Notes = "Co mat truoc 30 phut",
                IsActive = true,
                CreatedAt = now
            });

        context.TourPackages.Add(new TourPackage
        {
            Id = packageId,
            TenantId = tenantId,
            TourId = tourId,
            Code = "PKG-DOC",
            Name = "Combo Tieu Chuan",
            Mode = TourPackageMode.Configurable,
            Status = TourPackageStatus.Active,
            CurrencyCode = "VND",
            IsDefault = true,
            IsActive = true,
            CreatedAt = now
        });

        context.TourPackageReservations.AddRange(
            new TourPackageReservation
            {
                Id = sourceReservationId,
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = sourceScheduleId,
                TourPackageId = packageId,
                UserId = userId,
                Code = "TPR-DOC-OLD",
                HoldToken = "hold-doc-old",
                Status = TourPackageReservationStatus.Confirmed,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 2,
                HeldCapacitySlots = 0,
                PackageSubtotalAmount = 4_200_000m,
                IsDeleted = false,
                CreatedAt = now
            },
            new TourPackageReservation
            {
                Id = reservationId,
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                UserId = userId,
                Code = "TPR-DOC",
                HoldToken = "hold-doc",
                Status = TourPackageReservationStatus.Confirmed,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 2,
                HeldCapacitySlots = 0,
                PackageSubtotalAmount = 4_500_000m,
                IsDeleted = false,
                CreatedAt = now
            });

        context.TourPackageBookings.Add(new TourPackageBooking
        {
            Id = sourceBookingId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = sourceScheduleId,
            TourPackageId = packageId,
            TourPackageReservationId = sourceReservationId,
            UserId = userId,
            Code = "TPB-DOC-OLD",
            Status = TourPackageBookingStatus.Cancelled,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 2,
            ConfirmedCapacitySlots = 2,
            PackageSubtotalAmount = 4_200_000m,
            ConfirmedAt = now.AddMinutes(5),
            IsDeleted = false,
            CreatedAt = now
        });

        context.TourPackageBookings.Add(new TourPackageBooking
        {
            Id = bookingId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = scheduleId,
            TourPackageId = packageId,
            TourPackageReservationId = reservationId,
            UserId = userId,
            Code = "TPB-DOC",
            Status = TourPackageBookingStatus.PartiallyCancelled,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 2,
            ConfirmedCapacitySlots = 2,
            PackageSubtotalAmount = 4_500_000m,
            ConfirmedAt = now.AddMinutes(20),
            Notes = "Lien he hotline neu can doi gio don",
            SnapshotJson = """{"packageType":"combo"}""",
            IsDeleted = false,
            CreatedAt = now.AddMinutes(10),
            Items =
            {
                new TourPackageBookingItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TourPackageReservationItemId = Guid.NewGuid(),
                    TourPackageComponentId = Guid.NewGuid(),
                    TourPackageComponentOptionId = Guid.NewGuid(),
                    ComponentType = TourPackageComponentType.Accommodation,
                    SourceType = TourPackageSourceType.Hotel,
                    Status = TourPackageBookingItemStatus.Confirmed,
                    Quantity = 1,
                    CurrencyCode = "VND",
                    UnitPrice = 2_500_000m,
                    LineAmount = 2_500_000m,
                    SnapshotJson = """{"name":"Khach san 4 sao","checkInDate":"2026-04-10","checkOutDate":"2026-04-12","units":"1","correlationId":"HTL-001"}""",
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(10)
                },
                new TourPackageBookingItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TourPackageReservationItemId = Guid.NewGuid(),
                    TourPackageComponentId = Guid.NewGuid(),
                    TourPackageComponentOptionId = Guid.NewGuid(),
                    ComponentType = TourPackageComponentType.OutboundTransport,
                    SourceType = TourPackageSourceType.Flight,
                    Status = TourPackageBookingItemStatus.RefundPending,
                    Quantity = 2,
                    CurrencyCode = "VND",
                    UnitPrice = 900_000m,
                    LineAmount = 1_800_000m,
                    SourceHoldToken = "FLT-HOLD-01",
                    SnapshotJson = """{"name":"Ve may bay khu hoi","FlightId":"VN123","FareClassId":"Y","requestedQuantity":"2","correlationId":"FLT-001"}""",
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(11)
                },
                new TourPackageBookingItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TourPackageReservationItemId = Guid.NewGuid(),
                    TourPackageComponentId = Guid.NewGuid(),
                    TourPackageComponentOptionId = Guid.NewGuid(),
                    ComponentType = TourPackageComponentType.Activity,
                    SourceType = TourPackageSourceType.Other,
                    Status = TourPackageBookingItemStatus.Failed,
                    Quantity = 1,
                    CurrencyCode = "VND",
                    UnitPrice = 200_000m,
                    LineAmount = 200_000m,
                    SnapshotJson = """{"name":"Suat spa bonus"}""",
                    ErrorMessage = "Source unavailable",
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(12)
                }
            }
        });

        context.TourItineraryDays.AddRange(
            new TourItineraryDay
            {
                Id = itineraryDay1Id,
                TenantId = tenantId,
                TourId = tourId,
                DayNumber = 1,
                Title = "Ngay 1 - Den Da Nang",
                StartLocation = "Ha Noi",
                EndLocation = "Da Nang",
                AccommodationName = "Khach san ven bien",
                IncludesLunch = true,
                TransportationSummary = "Bay toi Da Nang va nhan phong",
                IsActive = true,
                CreatedAt = now
            },
            new TourItineraryDay
            {
                Id = itineraryDay2Id,
                TenantId = tenantId,
                TourId = tourId,
                DayNumber = 2,
                Title = "Ngay 2 - Ba Na",
                StartLocation = "Da Nang",
                EndLocation = "Da Nang",
                IncludesBreakfast = true,
                IncludesDinner = true,
                TransportationSummary = "Di Ba Na bang xe du lich",
                IsActive = true,
                CreatedAt = now
            });

        context.TourItineraryItems.AddRange(
            new TourItineraryItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourItineraryDayId = itineraryDay1Id,
                Type = TourItineraryItemType.CheckIn,
                Title = "Nhan phong khach san",
                StartTime = new TimeOnly(14, 0),
                LocationName = "Da Nang",
                IsActive = true,
                CreatedAt = now
            },
            new TourItineraryItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourItineraryDayId = itineraryDay2Id,
                Type = TourItineraryItemType.Sightseeing,
                Title = "Tham quan Ba Na Hills",
                StartTime = new TimeOnly(8, 30),
                IncludesTicket = true,
                IsActive = true,
                CreatedAt = now
            });

        context.TourContacts.AddRange(
            new TourContact
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                Name = "Hotline dieu hanh",
                Phone = "0909000111",
                ContactType = TourContactType.Hotline,
                IsPrimary = true,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now
            },
            new TourContact
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                Name = "Huong dan vien",
                Phone = "0909222333",
                ContactType = TourContactType.Guide,
                IsPrimary = false,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now
            });

        context.TourPickupPoints.Add(new TourPickupPoint
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = "PICKUP-DOC",
            Name = "San bay Da Nang",
            AddressLine = "Ga den quoc noi",
            PickupTime = new TimeOnly(8, 0),
            IsDefault = true,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = now
        });

        context.TourDropoffPoints.Add(new TourDropoffPoint
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = "DROPOFF-DOC",
            Name = "Trung tam thanh pho",
            AddressLine = "Hai Chau, Da Nang",
            DropoffTime = new TimeOnly(18, 0),
            IsDefault = true,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = now
        });

        context.TourPolicies.AddRange(
            new TourPolicy
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                Code = "POL-GEN",
                Name = "Luu y chung",
                Type = TourPolicyType.General,
                ShortDescription = "Co mat dung gio",
                IsHighlighted = true,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now
            },
            new TourPolicy
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                Code = "POL-CAN",
                Name = "Chinh sach huy",
                Type = TourPolicyType.Cancellation,
                ShortDescription = "Ap dung theo thong bao",
                IsHighlighted = true,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now
            },
            new TourPolicy
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                Code = "POL-CHILD",
                Name = "Tre em",
                Type = TourPolicyType.Child,
                ShortDescription = "Tre em duoi 5 tuoi",
                IsHighlighted = false,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = now
            });

        context.TourPackageReschedules.Add(new TourPackageReschedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            SourceTourPackageBookingId = sourceBookingId,
            SourceTourPackageReservationId = sourceReservationId,
            SourceTourScheduleId = sourceScheduleId,
            SourceTourPackageId = packageId,
            TargetTourScheduleId = scheduleId,
            TargetTourPackageId = packageId,
            TargetTourPackageReservationId = reservationId,
            TargetTourPackageBookingId = bookingId,
            RequestedByUserId = userId,
            Code = "RSCH-DOC",
            ClientToken = "reschedule-doc",
            Status = TourPackageRescheduleStatus.Completed,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 2,
            SourcePackageSubtotalAmount = 4_200_000m,
            TargetPackageSubtotalAmount = 4_500_000m,
            PriceDifferenceAmount = 300_000m,
            ConfirmedAt = now.AddMinutes(15),
            IsDeleted = false,
            CreatedAt = now.AddMinutes(14)
        });

        await context.SaveChangesAsync();

        return new DocumentSeedContext
        {
            TourId = tourId,
            BookingId = bookingId,
            UserId = userId
        };
    }

    private sealed class DocumentSeedContext
    {
        public Guid TourId { get; init; }
        public Guid BookingId { get; init; }
        public Guid UserId { get; init; }
    }
}
