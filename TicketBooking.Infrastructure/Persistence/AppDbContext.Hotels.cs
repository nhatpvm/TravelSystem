// FILE: TicketBooking.Infrastructure/Persistence/AppDbContext.Hotels.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Persistence.Hotels;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 11 - Hotels PRO DbSets + model hook
    /// NOTE:
    /// - Mapping is done in HotelConfigurations.cs (schema "hotels")
    /// - This file registers DbSets + exposes ApplyHotelsModel(builder) like other modules.
    /// </summary>
    public partial class AppDbContext
    {
        // ===== hotels (core) =====
        public DbSet<Hotel> Hotels => Set<Hotel>();
        public DbSet<HotelImage> HotelImages => Set<HotelImage>();
        public DbSet<HotelAmenity> HotelAmenities => Set<HotelAmenity>();
        public DbSet<HotelAmenityLink> HotelAmenityLinks => Set<HotelAmenityLink>();

        public DbSet<RoomType> RoomTypes => Set<RoomType>();
        public DbSet<RoomTypeImage> RoomTypeImages => Set<RoomTypeImage>();
        public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();
        public DbSet<RoomAmenityLink> RoomAmenityLinks => Set<RoomAmenityLink>();

        public DbSet<BedType> BedTypes => Set<BedType>();
        public DbSet<RoomTypeBed> RoomTypeBeds => Set<RoomTypeBed>();
        public DbSet<RoomTypeOccupancyRule> RoomTypeOccupancyRules => Set<RoomTypeOccupancyRule>();

        public DbSet<MealPlan> MealPlans => Set<MealPlan>();
        public DbSet<RoomTypeMealPlan> RoomTypeMealPlans => Set<RoomTypeMealPlan>();

        public DbSet<RatePlan> RatePlans => Set<RatePlan>();
        public DbSet<RatePlanRoomType> RatePlanRoomTypes => Set<RatePlanRoomType>();
        public DbSet<RatePlanPolicy> RatePlanPolicies => Set<RatePlanPolicy>();

        public DbSet<CancellationPolicy> CancellationPolicies => Set<CancellationPolicy>();
        public DbSet<CancellationPolicyRule> CancellationPolicyRules => Set<CancellationPolicyRule>();

        public DbSet<CheckInOutRule> CheckInOutRules => Set<CheckInOutRule>();
        public DbSet<PropertyPolicy> PropertyPolicies => Set<PropertyPolicy>();

        public DbSet<ExtraService> ExtraServices => Set<ExtraService>();
        public DbSet<ExtraServicePrice> ExtraServicePrices => Set<ExtraServicePrice>();

        public DbSet<RoomTypeInventory> RoomTypeInventories => Set<RoomTypeInventory>();
        public DbSet<DailyRate> DailyRates => Set<DailyRate>();
        public DbSet<InventoryHold> InventoryHolds => Set<InventoryHold>();

        // ===== hotels (4 add-on production tables) =====
        public DbSet<HotelContact> HotelContacts => Set<HotelContact>();
        public DbSet<RoomTypePolicy> RoomTypePolicies => Set<RoomTypePolicy>();
        public DbSet<PromoRateOverride> PromoRateOverrides => Set<PromoRateOverride>();
        public DbSet<HotelReview> HotelReviews => Set<HotelReview>();
    }

    public static class AppDbContextHotelsModel
    {
        public static void ApplyHotelsModel(ModelBuilder builder)
        {
            // Apply all IEntityTypeConfiguration<> in the Infrastructure assembly that contains HotelConfigurations
            builder.ApplyConfigurationsFromAssembly(typeof(HotelConfigurations).Assembly);
        }
    }
}
