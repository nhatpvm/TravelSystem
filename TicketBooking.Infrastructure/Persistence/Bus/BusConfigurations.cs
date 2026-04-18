// FILE #066: TicketBooking.Infrastructure/Persistence/Bus/BusConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Fleet;

namespace TicketBooking.Infrastructure.Persistence.Bus
{
    public sealed class StopPointConfiguration : IEntityTypeConfiguration<StopPoint>
    {
        public void Configure(EntityTypeBuilder<StopPoint> b)
        {
            b.ToTable("StopPoints", "bus");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.LocationId).IsRequired();

            b.Property(x => x.Type).HasConversion<int>().IsRequired();

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.AddressLine).HasMaxLength(300);
            b.Property(x => x.Notes).HasColumnType("nvarchar(max)");

            b.Property(x => x.SortOrder).HasDefaultValue(0);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
      .IsRowVersion()
      .IsConcurrencyToken();


            b.HasIndex(x => new { x.TenantId, x.Type, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.LocationId });

            b.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class RouteConfiguration : IEntityTypeConfiguration<BusRoute>
    {
        public void Configure(EntityTypeBuilder<BusRoute> b)
        {
            b.ToTable("Routes", "bus");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.ProviderId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.EstimatedMinutes).HasDefaultValue(0);
            b.Property(x => x.DistanceKm).HasDefaultValue(0);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
    .IsRowVersion()
    .IsConcurrencyToken();


            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.ProviderId });
            b.HasIndex(x => new { x.TenantId, x.IsActive });

            b.HasOne<Provider>()
                .WithMany()
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<StopPoint>()
                .WithMany()
                .HasForeignKey(x => x.FromStopPointId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<StopPoint>()
                .WithMany()
                .HasForeignKey(x => x.ToStopPointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
    {
        public void Configure(EntityTypeBuilder<RouteStop> b)
        {
            b.ToTable("RouteStops", "bus");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.RouteId).IsRequired();
            b.Property(x => x.StopPointId).IsRequired();

            b.Property(x => x.StopIndex).IsRequired();

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
      .IsRowVersion()
      .IsConcurrencyToken();


            b.HasIndex(x => new { x.RouteId, x.StopIndex }).IsUnique();
            b.HasIndex(x => new { x.RouteId, x.StopPointId });

            b.HasOne<BusRoute>()
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<StopPoint>()
                .WithMany()
                .HasForeignKey(x => x.StopPointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
    {
        public void Configure(EntityTypeBuilder<Trip> b)
        {
            b.ToTable("Trips", "bus");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.ProviderId).IsRequired();
            b.Property(x => x.RouteId).IsRequired();
            b.Property(x => x.VehicleId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.Property(x => x.FareRulesJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.BaggagePolicyJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.BoardingPolicyJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.Notes).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
     .IsRowVersion()
     .IsConcurrencyToken();


            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.ProviderId });
            b.HasIndex(x => new { x.TenantId, x.RouteId });
            b.HasIndex(x => new { x.TenantId, x.DepartureAt });

            b.HasOne<Provider>()
                .WithMany()
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<BusRoute>()
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Vehicle>()
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TripStopTimeConfiguration : IEntityTypeConfiguration<TripStopTime>
    {
        public void Configure(EntityTypeBuilder<TripStopTime> b)
        {
            b.ToTable("TripStopTimes", "bus");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();
            b.Property(x => x.StopPointId).IsRequired();
            b.Property(x => x.StopIndex).IsRequired();

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
          .IsRowVersion()
          .IsConcurrencyToken();


            b.HasIndex(x => new { x.TripId, x.StopIndex }).IsUnique();
            b.HasIndex(x => new { x.TripId, x.StopPointId });

            b.HasOne<Trip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<StopPoint>()
                .WithMany()
                .HasForeignKey(x => x.StopPointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TripStopPickupPointConfiguration : IEntityTypeConfiguration<TripStopPickupPoint>
    {
        public void Configure(EntityTypeBuilder<TripStopPickupPoint> b)
        {
            b.ToTable("TripStopPickupPoints", "bus");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripStopTimeId).IsRequired();

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.AddressLine).HasMaxLength(300);

            b.Property(x => x.IsDefault).HasDefaultValue(false);
            b.Property(x => x.SortOrder).HasDefaultValue(0);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
        .IsRowVersion()
        .IsConcurrencyToken();


            b.HasIndex(x => new { x.TripStopTimeId, x.SortOrder });
            b.HasIndex(x => new { x.TripStopTimeId, x.IsDefault });

            b.HasOne<TripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.TripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TripStopDropoffPointConfiguration : IEntityTypeConfiguration<TripStopDropoffPoint>
    {
        public void Configure(EntityTypeBuilder<TripStopDropoffPoint> b)
        {
            b.ToTable("TripStopDropoffPoints", "bus");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripStopTimeId).IsRequired();

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.AddressLine).HasMaxLength(300);

            b.Property(x => x.IsDefault).HasDefaultValue(false);
            b.Property(x => x.SortOrder).HasDefaultValue(0);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
      .IsRowVersion()
      .IsConcurrencyToken();


            b.HasIndex(x => new { x.TripStopTimeId, x.SortOrder });
            b.HasIndex(x => new { x.TripStopTimeId, x.IsDefault });

            b.HasOne<TripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.TripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TripSegmentPriceConfiguration : IEntityTypeConfiguration<TripSegmentPrice>
    {
        public void Configure(EntityTypeBuilder<TripSegmentPrice> b)
        {
            b.ToTable("TripSegmentPrices", "bus", tb =>
            {
                tb.HasCheckConstraint("CK_bus_TripSegmentPrices_StopIndex", "[FromStopIndex] < [ToStopIndex]");
            });
            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();

            b.Property(x => x.FromTripStopTimeId).IsRequired();
            b.Property(x => x.ToTripStopTimeId).IsRequired();

            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

            b.Property(x => x.BaseFare).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxesFees).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
             .IsRowVersion()
             .IsConcurrencyToken();


            // One price row per (trip, fromIndex, toIndex)
            b.HasIndex(x => new { x.TripId, x.FromStopIndex, x.ToStopIndex }).IsUnique();
            b.HasIndex(x => new { x.TripId, x.IsActive });

            b.HasOne<Trip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.FromTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.ToTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TripSeatHoldConfiguration : IEntityTypeConfiguration<TripSeatHold>
    {
        public void Configure(EntityTypeBuilder<TripSeatHold> b)
        {
            b.ToTable("TripSeatHolds", "bus", tb =>
            {
                tb.HasCheckConstraint("CK_bus_TripSeatHolds_StopIndex", "[FromStopIndex] < [ToStopIndex]");
            });
            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();
            b.Property(x => x.SeatId).IsRequired();

            b.Property(x => x.FromTripStopTimeId).IsRequired();
            b.Property(x => x.ToTripStopTimeId).IsRequired();

            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.Property(x => x.HoldToken).HasMaxLength(100).IsRequired();


            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
     .IsRowVersion()
     .IsConcurrencyToken();

            b.HasIndex(x => new { x.TripId, x.SeatId });
            b.HasIndex(x => new { x.TripId, x.Status, x.HoldExpiresAt });
            b.HasIndex(x => new { x.TenantId, x.HoldToken });
            b.HasIndex(x => new { x.TenantId, x.TripId, x.HoldToken });
            b.HasIndex(x => new { x.TenantId, x.TripId, x.SeatId, x.Status, x.HoldExpiresAt });


            b.HasOne<Trip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Seat>()
                .WithMany()
                .HasForeignKey(x => x.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.FromTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.ToTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}