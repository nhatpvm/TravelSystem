// FILE #163: TicketBooking.Infrastructure/Persistence/Hotels/HotelConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Hotels;

namespace TicketBooking.Infrastructure.Persistence.Hotels
{
    /// <summary>
    /// Phase 11 - Hotel PRO configurations (schema "hotels")
    /// Notes:
    /// - Multi-tenant: all tables have TenantId and are filtered by global query filters.
    /// - Soft delete: IsDeleted + global query filter is applied in AppDbContext.
    /// - Concurrency: RowVersion is marked as concurrency token.
    /// - Avoid cascade cycles: use Restrict/NoAction to be safe with tenant-wide graphs.
    /// - DateOnly: mapped to SQL 'date'. TimeOnly: mapped to SQL 'time(0)'.
    /// </summary>
    public sealed class HotelConfigurations :
        IEntityTypeConfiguration<Hotel>,
        IEntityTypeConfiguration<HotelImage>,
        IEntityTypeConfiguration<HotelAmenity>,
        IEntityTypeConfiguration<HotelAmenityLink>,
        IEntityTypeConfiguration<RoomType>,
        IEntityTypeConfiguration<RoomTypeImage>,
        IEntityTypeConfiguration<RoomAmenity>,
        IEntityTypeConfiguration<RoomAmenityLink>,
        IEntityTypeConfiguration<BedType>,
        IEntityTypeConfiguration<RoomTypeBed>,
        IEntityTypeConfiguration<RoomTypeOccupancyRule>,
        IEntityTypeConfiguration<MealPlan>,
        IEntityTypeConfiguration<RoomTypeMealPlan>,
        IEntityTypeConfiguration<RatePlan>,
        IEntityTypeConfiguration<RatePlanRoomType>,
        IEntityTypeConfiguration<RatePlanPolicy>,
        IEntityTypeConfiguration<CancellationPolicy>,
        IEntityTypeConfiguration<CancellationPolicyRule>,
        IEntityTypeConfiguration<CheckInOutRule>,
        IEntityTypeConfiguration<PropertyPolicy>,
        IEntityTypeConfiguration<ExtraService>,
        IEntityTypeConfiguration<ExtraServicePrice>,
        IEntityTypeConfiguration<RoomTypeInventory>,
        IEntityTypeConfiguration<DailyRate>,
        IEntityTypeConfiguration<InventoryHold>,
        // Add-on production tables
        IEntityTypeConfiguration<HotelContact>,
        IEntityTypeConfiguration<RoomTypePolicy>,
        IEntityTypeConfiguration<PromoRateOverride>,
        IEntityTypeConfiguration<HotelReview>
    {
        // -----------------------
        // hotels.Hotels
        // -----------------------
        public void Configure(EntityTypeBuilder<Hotel> b)
        {
            b.ToTable("Hotels", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(300);

            b.Property(x => x.AddressLine).HasMaxLength(500);
            b.Property(x => x.City).HasMaxLength(200);
            b.Property(x => x.Province).HasMaxLength(200);
            b.Property(x => x.CountryCode).HasMaxLength(10);
            b.Property(x => x.TimeZone).HasMaxLength(100);

            b.Property(x => x.ShortDescription).HasMaxLength(2000);
            b.Property(x => x.DescriptionMarkdown).HasColumnType("nvarchar(max)");
            b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");

            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.WebsiteUrl).HasMaxLength(1000);
            b.Property(x => x.Latitude).HasPrecision(9, 6);
            b.Property(x => x.Longitude).HasPrecision(9, 6);

            // SEO
            b.Property(x => x.SeoTitle).HasMaxLength(300);
            b.Property(x => x.SeoDescription).HasMaxLength(2000);
            b.Property(x => x.SeoKeywords).HasMaxLength(2000);
            b.Property(x => x.CanonicalUrl).HasMaxLength(1000);
            b.Property(x => x.Robots).HasMaxLength(200);
            b.Property(x => x.OgImageUrl).HasMaxLength(1000);
            b.Property(x => x.SchemaJsonLd).HasColumnType("nvarchar(max)");

            // Media
            b.Property(x => x.CoverImageUrl).HasMaxLength(1000);

            b.Property(x => x.PoliciesJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.DefaultCheckInTime).HasColumnType("time(0)");
            b.Property(x => x.DefaultCheckOutTime).HasColumnType("time(0)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique().HasFilter("[Slug] IS NOT NULL");
            b.HasIndex(x => new { x.TenantId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.LocationId });
        }

        // -----------------------
        // hotels.HotelImages
        // -----------------------
        public void Configure(EntityTypeBuilder<HotelImage> b)
        {
            b.ToTable("HotelImages", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.ImageUrl).HasMaxLength(1000);
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.AltText).HasMaxLength(300);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.SortOrder });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Kind });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.HotelAmenities
        // -----------------------
        public void Configure(EntityTypeBuilder<HotelAmenity> b)
        {
            b.ToTable("HotelAmenities", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.IconKey).HasMaxLength(200);
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Scope, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Scope, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.Scope, x.SortOrder });
        }

        // -----------------------
        // hotels.HotelAmenityLinks
        // -----------------------
        public void Configure(EntityTypeBuilder<HotelAmenityLink> b)
        {
            b.ToTable("HotelAmenityLinks", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.AmenityId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.HotelId });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<HotelAmenity>()
                .WithMany()
                .HasForeignKey(x => x.AmenityId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.RoomTypes
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomType> b)
        {
            b.ToTable("RoomTypes", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();

            b.Property(x => x.DescriptionMarkdown).HasColumnType("nvarchar(max)");
            b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");

            b.Property(x => x.CoverImageUrl).HasMaxLength(1000);
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.SortOrder });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Status });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.RoomTypeImages
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomTypeImage> b)
        {
            b.ToTable("RoomTypeImages", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.ImageUrl).HasMaxLength(1000);
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.AltText).HasMaxLength(300);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.SortOrder });
            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.Kind });

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.RoomAmenities
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomAmenity> b)
        {
            b.ToTable("RoomAmenities", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.IconKey).HasMaxLength(200);
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.SortOrder });
        }

        // -----------------------
        // hotels.RoomAmenityLinks
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomAmenityLink> b)
        {
            b.ToTable("RoomAmenityLinks", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.AmenityId }).IsUnique();

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<RoomAmenity>()
                .WithMany()
                .HasForeignKey(x => x.AmenityId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.BedTypes
        // -----------------------
        public void Configure(EntityTypeBuilder<BedType> b)
        {
            b.ToTable("BedTypes", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        // -----------------------
        // hotels.RoomTypeBeds
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomTypeBed> b)
        {
            b.ToTable("RoomTypeBeds", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.BedTypeId }).IsUnique();

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<BedType>()
                .WithMany()
                .HasForeignKey(x => x.BedTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.RoomTypeOccupancyRules
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomTypeOccupancyRule> b)
        {
            b.ToTable("RoomTypeOccupancyRules", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.IsActive });

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.MealPlans
        // -----------------------
        public void Configure(EntityTypeBuilder<MealPlan> b)
        {
            b.ToTable("MealPlans", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        // -----------------------
        // hotels.RoomTypeMealPlans
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomTypeMealPlan> b)
        {
            b.ToTable("RoomTypeMealPlans", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.MealPlanId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.IsActive });

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<MealPlan>()
                .WithMany()
                .HasForeignKey(x => x.MealPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.RatePlans
        // -----------------------
        public void Configure(EntityTypeBuilder<RatePlan> b)
        {
            b.ToTable("RatePlans", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);

            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Type });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional policy references (no navigation => configure via FK without cascade)
            b.HasOne<CancellationPolicy>()
                .WithMany()
                .HasForeignKey(x => x.CancellationPolicyId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne<CheckInOutRule>()
                .WithMany()
                .HasForeignKey(x => x.CheckInOutRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne<PropertyPolicy>()
                .WithMany()
                .HasForeignKey(x => x.PropertyPolicyId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        // -----------------------
        // hotels.RatePlanRoomTypes
        // -----------------------
        public void Configure(EntityTypeBuilder<RatePlanRoomType> b)
        {
            b.ToTable("RatePlanRoomTypes", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            b.Property(x => x.BasePrice).HasPrecision(18, 2);

            b.HasIndex(x => new { x.TenantId, x.RatePlanId, x.RoomTypeId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.IsActive });

            b.HasOne<RatePlan>()
                .WithMany()
                .HasForeignKey(x => x.RatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.RatePlanPolicies
        // -----------------------
        public void Configure(EntityTypeBuilder<RatePlanPolicy> b)
        {
            b.ToTable("RatePlanPolicies", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.PolicyJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RatePlanId }).IsUnique();

            b.HasOne<RatePlan>()
                .WithMany()
                .HasForeignKey(x => x.RatePlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.CancellationPolicies
        // -----------------------
        public void Configure(EntityTypeBuilder<CancellationPolicy> b)
        {
            b.ToTable("CancellationPolicies", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Type });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.CancellationPolicyRules
        // -----------------------
        public void Configure(EntityTypeBuilder<CancellationPolicyRule> b)
        {
            b.ToTable("CancellationPolicyRules", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            b.Property(x => x.ChargeValue).HasPrecision(18 , 2);

            b.HasIndex(x => new { x.TenantId, x.CancellationPolicyId, x.Priority });
            b.HasIndex(x => new { x.TenantId, x.CancellationPolicyId, x.IsActive });

            b.HasOne<CancellationPolicy>()
                .WithMany()
                .HasForeignKey(x => x.CancellationPolicyId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.CheckInOutRules
        // -----------------------
        public void Configure(EntityTypeBuilder<CheckInOutRule> b)
        {
            b.ToTable("CheckInOutRules", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.Property(x => x.CheckInFrom).HasColumnType("time(0)");
            b.Property(x => x.CheckInTo).HasColumnType("time(0)");
            b.Property(x => x.CheckOutFrom).HasColumnType("time(0)");
            b.Property(x => x.CheckOutTo).HasColumnType("time(0)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.PropertyPolicies
        // -----------------------
        public void Configure(EntityTypeBuilder<PropertyPolicy> b)
        {
            b.ToTable("PropertyPolicies", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.PolicyJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.ExtraServices
        // -----------------------
        public void Configure(EntityTypeBuilder<ExtraService> b)
        {
            b.ToTable("ExtraServices", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Type });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.ExtraServicePrices
        // -----------------------
        public void Configure(EntityTypeBuilder<ExtraServicePrice> b)
        {
            b.ToTable("ExtraServicePrices", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.StartDate).HasColumnType("date");
            b.Property(x => x.EndDate).HasColumnType("date");
            b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(x => x.Price).HasPrecision(18, 2);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.ExtraServiceId, x.StartDate, x.EndDate });

            b.HasOne<ExtraService>()
                .WithMany()
                .HasForeignKey(x => x.ExtraServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.RoomTypeInventories
        // -----------------------
        public void Configure(EntityTypeBuilder<RoomTypeInventory> b)
        {
            b.ToTable("RoomTypeInventories", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Date).HasColumnType("date");
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.Date }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.Status });

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.DailyRates
        // -----------------------
        public void Configure(EntityTypeBuilder<DailyRate> b)
        {
            b.ToTable("DailyRates", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Date).HasColumnType("date");
            b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.BasePrice).HasPrecision(18, 2);
            b.Property(x => x.Fees).HasPrecision(18, 2);
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.Property(x => x.Taxes).HasPrecision(18, 2);
            b.Property(x => x.Price).HasPrecision(18, 2);



            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RatePlanRoomTypeId, x.Date }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RatePlanRoomTypeId, x.IsActive });

            b.HasOne<RatePlanRoomType>()
                .WithMany()
                .HasForeignKey(x => x.RatePlanRoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // -----------------------
        // hotels.InventoryHolds
        // -----------------------
        public void Configure(EntityTypeBuilder<InventoryHold> b)
        {
            b.ToTable("InventoryHolds", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.CheckInDate).HasColumnType("date");
            b.Property(x => x.CheckOutDate).HasColumnType("date");

            b.Property(x => x.CorrelationId).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.RoomTypeId, x.HoldExpiresAt });
            b.HasIndex(x => new { x.TenantId, x.BookingId });

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // =====================================================
        // Add-on production tables
        // =====================================================

        // hotels.HotelContacts
        public void Configure(EntityTypeBuilder<HotelContact> b)
        {
            b.ToTable("HotelContacts", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.ContactName).HasMaxLength(200).IsRequired();
            b.Property(x => x.RoleTitle).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsPrimary });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // hotels.RoomTypePolicies
        public void Configure(EntityTypeBuilder<RoomTypePolicy> b)
        {
            b.ToTable("RoomTypePolicies", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.PolicyJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RoomTypeId }).IsUnique();

            b.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey(x => x.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // hotels.PromoRateOverrides
        public void Configure(EntityTypeBuilder<PromoRateOverride> b)
        {
            b.ToTable("PromoRateOverrides", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.PromoCode).HasMaxLength(80);
            b.Property(x => x.StartDate).HasColumnType("date");
            b.Property(x => x.EndDate).HasColumnType("date");
            b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(x => x.ConditionsJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            b.Property(x => x.OverridePrice).HasPrecision(18, 2);

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RatePlanRoomTypeId, x.StartDate, x.EndDate });
            b.HasIndex(x => new { x.TenantId, x.PromoCodeId });

            b.HasOne<RatePlanRoomType>()
                .WithMany()
                .HasForeignKey(x => x.RatePlanRoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // hotels.HotelReviews
        public void Configure(EntityTypeBuilder<HotelReview> b)
        {
            b.ToTable("HotelReviews", "hotels");
            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(300);
            b.Property(x => x.Content).HasMaxLength(8000);
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Rating });
            b.HasIndex(x => new { x.TenantId, x.BookingId });

            b.HasOne<Hotel>()
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
