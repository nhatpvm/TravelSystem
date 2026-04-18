// FILE #058: TicketBooking.Infrastructure/Persistence/Fleet/FleetConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Fleet;

namespace TicketBooking.Infrastructure.Persistence.Fleet
{
    public sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
    {
        public void Configure(EntityTypeBuilder<VehicleModel> b)
        {
            b.ToTable("VehicleModels", "fleet");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.VehicleType).HasConversion<int>().IsRequired();

            b.Property(x => x.Manufacturer).HasMaxLength(100).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(120).IsRequired();

            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.VehicleType, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.Manufacturer, x.ModelName });
        }
    }

    public sealed class SeatMapConfiguration : IEntityTypeConfiguration<SeatMap>
    {
        public void Configure(EntityTypeBuilder<SeatMap> b)
        {
            b.ToTable("SeatMaps", "fleet");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.VehicleType).HasConversion<int>().IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.DeckCount).HasDefaultValue(1);
            b.Property(x => x.LayoutVersion).HasMaxLength(50);
            b.Property(x => x.SeatLabelScheme).HasMaxLength(50);

            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.VehicleType, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.VehicleType, x.IsActive });
        }
    }

    public sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
    {
        public void Configure(EntityTypeBuilder<Seat> b)
        {
            b.ToTable("Seats", "fleet");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.SeatMapId).IsRequired();

            b.Property(x => x.SeatNumber).HasMaxLength(20).IsRequired();

            b.Property(x => x.SeatType).HasConversion<int>().IsRequired();
            b.Property(x => x.SeatClass).HasConversion<int>().IsRequired();

            b.Property(x => x.PriceModifier).HasColumnType("decimal(18,2)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            // Unique seat code within a seat map
            b.HasIndex(x => new { x.SeatMapId, x.SeatNumber }).IsUnique();
            b.HasIndex(x => x.SeatMapId);

            b.HasOne<SeatMap>()
                .WithMany()
                .HasForeignKey(x => x.SeatMapId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> b)
        {
            b.ToTable("Vehicles", "fleet");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.VehicleType).HasConversion<int>().IsRequired();

            b.Property(x => x.ProviderId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.PlateNumber).HasMaxLength(50);
            b.Property(x => x.RegistrationNumber).HasMaxLength(50);

            b.Property(x => x.Status).HasMaxLength(50);

            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.VehicleType, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.ProviderId });
            b.HasIndex(x => new { x.TenantId, x.VehicleType, x.IsActive });

            b.HasOne<Provider>()
                .WithMany()
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<VehicleModel>()
                .WithMany()
                .HasForeignKey(x => x.VehicleModelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<SeatMap>()
                .WithMany()
                .HasForeignKey(x => x.SeatMapId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class BusVehicleDetailConfiguration : IEntityTypeConfiguration<BusVehicleDetail>
    {
        public void Configure(EntityTypeBuilder<BusVehicleDetail> b)
        {
            b.ToTable("BusVehicleDetails", "fleet");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.VehicleId).IsRequired();

            b.Property(x => x.BusType).HasMaxLength(100);
            b.Property(x => x.AmenitiesJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.VehicleId }).IsUnique();

            b.HasOne<Vehicle>()
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}