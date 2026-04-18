// FILE #243: TicketBooking.Infrastructure/Persistence/AppDbContext.Tour.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence.Tours;

namespace TicketBooking.Infrastructure.Persistence;

public partial class AppDbContext
{
    // ===== tours =====
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourSchedule> TourSchedules => Set<TourSchedule>();
    public DbSet<TourPackage> TourPackages => Set<TourPackage>();
    public DbSet<TourPackageComponent> TourPackageComponents => Set<TourPackageComponent>();
    public DbSet<TourPackageComponentOption> TourPackageComponentOptions => Set<TourPackageComponentOption>();
    public DbSet<TourPackageScheduleOptionOverride> TourPackageScheduleOptionOverrides => Set<TourPackageScheduleOptionOverride>();
    public DbSet<TourPackageReservation> TourPackageReservations => Set<TourPackageReservation>();
    public DbSet<TourPackageReservationItem> TourPackageReservationItems => Set<TourPackageReservationItem>();
    public DbSet<TourPackageBooking> TourPackageBookings => Set<TourPackageBooking>();
    public DbSet<TourPackageBookingItem> TourPackageBookingItems => Set<TourPackageBookingItem>();
    public DbSet<TourPackageReschedule> TourPackageReschedules => Set<TourPackageReschedule>();
    public DbSet<TourPackageCancellation> TourPackageCancellations => Set<TourPackageCancellation>();
    public DbSet<TourPackageCancellationItem> TourPackageCancellationItems => Set<TourPackageCancellationItem>();
    public DbSet<TourPackageRefund> TourPackageRefunds => Set<TourPackageRefund>();
    public DbSet<TourPackageRefundAttempt> TourPackageRefundAttempts => Set<TourPackageRefundAttempt>();
    public DbSet<TourPackageAuditEvent> TourPackageAuditEvents => Set<TourPackageAuditEvent>();
    public DbSet<TourSchedulePrice> TourSchedulePrices => Set<TourSchedulePrice>();
    public DbSet<TourScheduleCapacity> TourScheduleCapacities => Set<TourScheduleCapacity>();

    public DbSet<TourItineraryDay> TourItineraryDays => Set<TourItineraryDay>();
    public DbSet<TourItineraryItem> TourItineraryItems => Set<TourItineraryItem>();

    public DbSet<TourAddon> TourAddons => Set<TourAddon>();
    public DbSet<TourScheduleAddonPrice> TourScheduleAddonPrices => Set<TourScheduleAddonPrice>();

    public DbSet<TourPolicy> TourPolicies => Set<TourPolicy>();
    public DbSet<TourImage> TourImages => Set<TourImage>();
    public DbSet<TourContact> TourContacts => Set<TourContact>();
    public DbSet<TourReview> TourReviews => Set<TourReview>();
    public DbSet<TourFaq> TourFaqs => Set<TourFaq>();

    public DbSet<TourPickupPoint> TourPickupPoints => Set<TourPickupPoint>();
    public DbSet<TourDropoffPoint> TourDropoffPoints => Set<TourDropoffPoint>();
}

public static class AppDbContextTourModel
{
    public static void ApplyTourModel(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(TourConfiguration).Assembly);
    }
}

