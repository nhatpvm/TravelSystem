// FILE #242: TicketBooking.Infrastructure/Persistence/Tours/TourConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Infrastructure.Persistence.Tours;

public sealed class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> b)
    {
        b.ToTable("Tours", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(300).IsRequired();

        b.Property(x => x.CountryCode).HasMaxLength(10);
        b.Property(x => x.Province).HasMaxLength(200);
        b.Property(x => x.City).HasMaxLength(200);
        b.Property(x => x.MeetingPointSummary).HasMaxLength(1000);

        b.Property(x => x.ShortDescription).HasMaxLength(2000);
        b.Property(x => x.DescriptionMarkdown).HasColumnType("nvarchar(max)");
        b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");

        b.Property(x => x.HighlightsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.IncludesJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ExcludesJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.TermsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.CoverImageUrl).HasMaxLength(1000);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsActive, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.PrimaryLocationId });
        b.HasIndex(x => new { x.TenantId, x.ProviderId });
        b.HasIndex(x => new { x.TenantId, x.IsFeatured, x.IsFeaturedOnHome });

        b.HasMany(x => x.Images)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Contacts)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Policies)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Faqs)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Reviews)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.ItineraryDays)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Schedules)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Addons)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.PickupPoints)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.DropoffPoints)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourScheduleConfiguration : IEntityTypeConfiguration<TourSchedule>
{
    public void Configure(EntityTypeBuilder<TourSchedule> b)
    {
        b.ToTable("TourSchedules", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300);

        b.Property(x => x.MeetingPointSummary).HasMaxLength(1000);
        b.Property(x => x.PickupSummary).HasMaxLength(1000);
        b.Property(x => x.DropoffSummary).HasMaxLength(1000);
        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.InternalNotes).HasMaxLength(4000);
        b.Property(x => x.CancellationNotes).HasMaxLength(4000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourId, x.DepartureDate });
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsActive, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.BookingCutoffAt });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Prices)
            .WithOne(x => x.TourSchedule)
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Capacities)
            .WithOne(x => x.TourSchedule)
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.AddonPrices)
            .WithOne(x => x.TourSchedule)
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourSchedulePriceConfiguration : IEntityTypeConfiguration<TourSchedulePrice>
{
    public void Configure(EntityTypeBuilder<TourSchedulePrice> b)
    {
        b.ToTable("TourSchedulePrices", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.OriginalPrice).HasPrecision(18, 2);
        b.Property(x => x.Taxes).HasPrecision(18, 2);
        b.Property(x => x.Fees).HasPrecision(18, 2);

        b.Property(x => x.Label).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.PriceType });
        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.IsDefault });
        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.TourSchedule)
            .WithMany(x => x.Prices)
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourScheduleCapacityConfiguration : IEntityTypeConfiguration<TourScheduleCapacity>
{
    public void Configure(EntityTypeBuilder<TourScheduleCapacity> b)
    {
        b.ToTable("TourScheduleCapacities", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourScheduleId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.TourSchedule)
            .WithMany(x => x.Capacities)
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourItineraryDayConfiguration : IEntityTypeConfiguration<TourItineraryDay>
{
    public void Configure(EntityTypeBuilder<TourItineraryDay> b)
    {
        b.ToTable("TourItineraryDays", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(2000);
        b.Property(x => x.DescriptionMarkdown).HasColumnType("nvarchar(max)");
        b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");

        b.Property(x => x.StartLocation).HasMaxLength(300);
        b.Property(x => x.EndLocation).HasMaxLength(300);
        b.Property(x => x.AccommodationName).HasMaxLength(300);
        b.Property(x => x.TransportationSummary).HasMaxLength(1000);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.DayNumber }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.ItineraryDays)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Items)
            .WithOne(x => x.TourItineraryDay)
            .HasForeignKey(x => x.TourItineraryDayId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourItineraryItemConfiguration : IEntityTypeConfiguration<TourItineraryItem>
{
    public void Configure(EntityTypeBuilder<TourItineraryItem> b)
    {
        b.ToTable("TourItineraryItems", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(2000);
        b.Property(x => x.DescriptionMarkdown).HasColumnType("nvarchar(max)");
        b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");
        b.Property(x => x.LocationName).HasMaxLength(300);
        b.Property(x => x.AddressLine).HasMaxLength(500);
        b.Property(x => x.TransportationMode).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourItineraryDayId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.TourItineraryDayId, x.Type });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.TourItineraryDay)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.TourItineraryDayId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourAddonConfiguration : IEntityTypeConfiguration<TourAddon>
{
    public void Configure(EntityTypeBuilder<TourAddon> b)
    {
        b.ToTable("TourAddons", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(2000);
        b.Property(x => x.DescriptionMarkdown).HasColumnType("nvarchar(max)");
        b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.BasePrice).HasPrecision(18, 2);
        b.Property(x => x.OriginalPrice).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourId, x.Type });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Addons)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.SchedulePrices)
            .WithOne(x => x.TourAddon)
            .HasForeignKey(x => x.TourAddonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourScheduleAddonPriceConfiguration : IEntityTypeConfiguration<TourScheduleAddonPrice>
{
    public void Configure(EntityTypeBuilder<TourScheduleAddonPrice> b)
    {
        b.ToTable("TourScheduleAddonPrices", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.OriginalPrice).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.TourAddonId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.TourSchedule)
            .WithMany(x => x.AddonPrices)
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourAddon)
            .WithMany(x => x.SchedulePrices)
            .HasForeignKey(x => x.TourAddonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPolicyConfiguration : IEntityTypeConfiguration<TourPolicy>
{
    public void Configure(EntityTypeBuilder<TourPolicy> b)
    {
        b.ToTable("TourPolicies", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(2000);
        b.Property(x => x.DescriptionMarkdown).HasColumnType("nvarchar(max)");
        b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");
        b.Property(x => x.PolicyJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourId, x.Type });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Policies)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourImageConfiguration : IEntityTypeConfiguration<TourImage>
{
    public void Configure(EntityTypeBuilder<TourImage> b)
    {
        b.ToTable("TourImages", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.ImageUrl).HasMaxLength(1000);
        b.Property(x => x.Caption).HasMaxLength(500);
        b.Property(x => x.AltText).HasMaxLength(500);
        b.Property(x => x.Title).HasMaxLength(500);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsPrimary });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsCover });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourContactConfiguration : IEntityTypeConfiguration<TourContact>
{
    public void Configure(EntityTypeBuilder<TourContact> b)
    {
        b.ToTable("TourContacts", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Department).HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.ContactType });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsPrimary });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Contacts)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourReviewConfiguration : IEntityTypeConfiguration<TourReview>
{
    public void Configure(EntityTypeBuilder<TourReview> b)
    {
        b.ToTable("TourReviews", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Rating).HasPrecision(4, 2);
        b.Property(x => x.Title).HasMaxLength(300);
        b.Property(x => x.Content).HasMaxLength(4000);
        b.Property(x => x.ReviewerName).HasMaxLength(200);
        b.Property(x => x.ModerationNote).HasMaxLength(2000);
        b.Property(x => x.ReplyContent).HasMaxLength(4000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsApproved, x.IsPublic });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.PublishedAt });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourFaqConfiguration : IEntityTypeConfiguration<TourFaq>
{
    public void Configure(EntityTypeBuilder<TourFaq> b)
    {
        b.ToTable("TourFaqs", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Question).HasMaxLength(1000).IsRequired();
        b.Property(x => x.AnswerMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.AnswerHtml).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Type });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsHighlighted });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Faqs)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPickupPointConfiguration : IEntityTypeConfiguration<TourPickupPoint>
{
    public void Configure(EntityTypeBuilder<TourPickupPoint> b)
    {
        b.ToTable("TourPickupPoints", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.AddressLine).HasMaxLength(500);
        b.Property(x => x.Ward).HasMaxLength(200);
        b.Property(x => x.District).HasMaxLength(200);
        b.Property(x => x.Province).HasMaxLength(200);
        b.Property(x => x.CountryCode).HasMaxLength(10);
        b.Property(x => x.Latitude).HasPrecision(18, 6);
        b.Property(x => x.Longitude).HasPrecision(18, 6);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsDefault });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.PickupPoints)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourDropoffPointConfiguration : IEntityTypeConfiguration<TourDropoffPoint>
{
    public void Configure(EntityTypeBuilder<TourDropoffPoint> b)
    {
        b.ToTable("TourDropoffPoints", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.AddressLine).HasMaxLength(500);
        b.Property(x => x.Ward).HasMaxLength(200);
        b.Property(x => x.District).HasMaxLength(200);
        b.Property(x => x.Province).HasMaxLength(200);
        b.Property(x => x.CountryCode).HasMaxLength(10);
        b.Property(x => x.Latitude).HasPrecision(18, 6);
        b.Property(x => x.Longitude).HasPrecision(18, 6);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsDefault });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.DropoffPoints)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
