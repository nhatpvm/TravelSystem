// FILE #053: TicketBooking.Infrastructure/Persistence/Catalog/CatalogConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Geo;

namespace TicketBooking.Infrastructure.Persistence.Catalog
{
    public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> b)
        {
            b.ToTable("Locations", "catalog");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.Type).HasConversion<int>().IsRequired();

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(250).IsRequired();
            b.Property(x => x.ShortName).HasMaxLength(100);

            b.Property(x => x.Code).HasMaxLength(50);
            b.Property(x => x.AirportIataCode).HasMaxLength(10);
            b.Property(x => x.AirportIcaoCode).HasMaxLength(10);
            b.Property(x => x.TrainStationCode).HasMaxLength(50);
            b.Property(x => x.BusStationCode).HasMaxLength(50);

            b.Property(x => x.TimeZone).HasMaxLength(64);

            b.Property(x => x.AddressLine).HasMaxLength(300);

            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            // Concurrency token (keep varbinary(max) for now - we can migrate to rowversion later)
            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            // Indexes for search
            b.HasIndex(x => new { x.TenantId, x.Type, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.NormalizedName });
            b.HasIndex(x => new { x.TenantId, x.Code });
            b.HasIndex(x => new { x.TenantId, x.AirportIataCode });
            b.HasIndex(x => new { x.TenantId, x.TrainStationCode });
            b.HasIndex(x => new { x.TenantId, x.BusStationCode });

            // Geo links (optional)
            b.HasOne<Province>()
                .WithMany()
                .HasForeignKey(x => x.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<District>()
                .WithMany()
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Ward>()
                .WithMany()
                .HasForeignKey(x => x.WardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
    {
        public void Configure(EntityTypeBuilder<Provider> b)
        {
            b.ToTable("Providers", "catalog");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.Type).HasConversion<int>().IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(200).IsRequired();

            b.Property(x => x.LegalName).HasMaxLength(250);

            b.Property(x => x.LogoUrl).HasMaxLength(500);
            b.Property(x => x.CoverUrl).HasMaxLength(500);

            b.Property(x => x.SupportPhone).HasMaxLength(50);
            b.Property(x => x.SupportEmail).HasMaxLength(200);
            b.Property(x => x.WebsiteUrl).HasMaxLength(300);

            b.Property(x => x.AddressLine).HasMaxLength(300);

            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.RatingAverage).HasColumnType("decimal(3,2)");
            b.Property(x => x.RatingCount).HasDefaultValue(0);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            // Unique per tenant
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();

            b.HasIndex(x => new { x.TenantId, x.Type, x.IsActive });

            // Location/Geo links
            b.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Province>()
                .WithMany()
                .HasForeignKey(x => x.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<District>()
                .WithMany()
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Ward>()
                .WithMany()
                .HasForeignKey(x => x.WardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}