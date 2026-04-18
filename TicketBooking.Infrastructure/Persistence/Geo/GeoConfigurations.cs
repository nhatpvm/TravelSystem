// FILE #045: TicketBooking.Infrastructure/Persistence/Geo/GeoConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Geo;

namespace TicketBooking.Infrastructure.Persistence.Geo
{
    public sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
    {
        public void Configure(EntityTypeBuilder<Province> b)
        {
            b.ToTable("Provinces", "geo");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NameEn).HasMaxLength(200);
            b.Property(x => x.Slug).HasMaxLength(250);
            b.Property(x => x.Type).HasMaxLength(50);

            b.Property(x => x.IsActive).HasDefaultValue(true);

            // Unique API code
            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.Name);

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt);
        }
    }

    public sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
    {
        public void Configure(EntityTypeBuilder<District> b)
        {
            b.ToTable("Districts", "geo");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NameEn).HasMaxLength(200);
            b.Property(x => x.Slug).HasMaxLength(250);
            b.Property(x => x.Type).HasMaxLength(50);

            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.ProvinceId);
            b.HasIndex(x => x.Name);

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt);

            b.HasOne<Province>()
                .WithMany()
                .HasForeignKey(x => x.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
    {
        public void Configure(EntityTypeBuilder<Ward> b)
        {
            b.ToTable("Wards", "geo");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NameEn).HasMaxLength(200);
            b.Property(x => x.Slug).HasMaxLength(250);
            b.Property(x => x.Type).HasMaxLength(50);

            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.DistrictId);
            b.HasIndex(x => x.Name);

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt);

            b.HasOne<District>()
                .WithMany()
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}