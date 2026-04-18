// FILE #077 (FIXED): TicketBooking.Infrastructure/Persistence/Train/TrainConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Train;

namespace TicketBooking.Infrastructure.Persistence.Train
{
    public sealed class TrainStopPointConfiguration : IEntityTypeConfiguration<TrainStopPoint>
    {
        public void Configure(EntityTypeBuilder<TrainStopPoint> b)
        {
            b.ToTable("StopPoints", "train");

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
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.LocationId });
            b.HasIndex(x => new { x.TenantId, x.Type, x.IsActive });

            b.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainRouteConfiguration : IEntityTypeConfiguration<TrainRoute>
    {
        public void Configure(EntityTypeBuilder<TrainRoute> b)
        {
            b.ToTable("Routes", "train");

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
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.ProviderId });

            b.HasOne<TrainStopPoint>()
                .WithMany()
                .HasForeignKey(x => x.FromStopPointId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainStopPoint>()
                .WithMany()
                .HasForeignKey(x => x.ToStopPointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainRouteStopConfiguration : IEntityTypeConfiguration<TrainRouteStop>
    {
        public void Configure(EntityTypeBuilder<TrainRouteStop> b)
        {
            b.ToTable("RouteStops", "train");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.RouteId).IsRequired();
            b.Property(x => x.StopPointId).IsRequired();
            b.Property(x => x.StopIndex).IsRequired();

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.RouteId, x.StopIndex }).IsUnique();
            b.HasIndex(x => new { x.RouteId, x.StopPointId });

            b.HasOne<TrainRoute>()
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainStopPoint>()
                .WithMany()
                .HasForeignKey(x => x.StopPointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainTripConfiguration : IEntityTypeConfiguration<TrainTrip>
    {
        public void Configure(EntityTypeBuilder<TrainTrip> b)
        {
            b.ToTable("Trips", "train");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.ProviderId).IsRequired();
            b.Property(x => x.RouteId).IsRequired();

            b.Property(x => x.TrainNumber).HasMaxLength(20).IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.Property(x => x.FareRulesJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.BaggagePolicyJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.BoardingPolicyJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.DepartureAt });

            b.HasOne<TrainRoute>()
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainTripStopTimeConfiguration : IEntityTypeConfiguration<TrainTripStopTime>
    {
        public void Configure(EntityTypeBuilder<TrainTripStopTime> b)
        {
            b.ToTable("TripStopTimes", "train");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();
            b.Property(x => x.StopPointId).IsRequired();
            b.Property(x => x.StopIndex).IsRequired();

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TripId, x.StopIndex }).IsUnique();
            b.HasIndex(x => new { x.TripId, x.StopPointId });

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainStopPoint>()
                .WithMany()
                .HasForeignKey(x => x.StopPointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainTripSegmentPriceConfiguration : IEntityTypeConfiguration<TrainTripSegmentPrice>
    {
        public void Configure(EntityTypeBuilder<TrainTripSegmentPrice> b)
        {
            b.ToTable("TripSegmentPrices", "train", tb =>
            {
                tb.HasCheckConstraint("CK_train_TripSegmentPrices_StopIndex", "[FromStopIndex] < [ToStopIndex]");
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
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TripId, x.FromStopIndex, x.ToStopIndex }).IsUnique();
            b.HasIndex(x => new { x.TripId, x.IsActive });

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainTripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.FromTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainTripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.ToTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainCarConfiguration : IEntityTypeConfiguration<TrainCar>
    {
        public void Configure(EntityTypeBuilder<TrainCar> b)
        {
            b.ToTable("TrainCars", "train");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();

            b.Property(x => x.CarNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.CarType).HasConversion<int>().IsRequired();
            b.Property(x => x.CabinClass).HasMaxLength(50);

            b.Property(x => x.SortOrder).HasDefaultValue(0);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TripId, x.CarNumber }).IsUnique();
            b.HasIndex(x => new { x.TripId, x.SortOrder });

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainCarSeatConfiguration : IEntityTypeConfiguration<TrainCarSeat>
    {
        public void Configure(EntityTypeBuilder<TrainCarSeat> b)
        {
            b.ToTable("TrainCarSeats", "train");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.CarId).IsRequired();

            b.Property(x => x.SeatNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.SeatType).HasConversion<int>().IsRequired();

            b.Property(x => x.CompartmentCode).HasMaxLength(20);
            b.Property(x => x.SeatClass).HasMaxLength(50);

            b.Property(x => x.PriceModifier).HasColumnType("decimal(18,2)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.CarId, x.SeatNumber }).IsUnique();
            b.HasIndex(x => new { x.CarId, x.CompartmentCode });

            b.HasOne<TrainCar>()
                .WithMany()
                .HasForeignKey(x => x.CarId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainTripSeatHoldConfiguration : IEntityTypeConfiguration<TrainTripSeatHold>
    {
        public void Configure(EntityTypeBuilder<TrainTripSeatHold> b)
        {
            b.ToTable("TripSeatHolds", "train", tb =>
            {
                tb.HasCheckConstraint("CK_train_TripSeatHolds_StopIndex", "[FromStopIndex] < [ToStopIndex]");
            });

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();
            b.Property(x => x.TrainCarSeatId).IsRequired();

            b.Property(x => x.FromTripStopTimeId).IsRequired();
            b.Property(x => x.ToTripStopTimeId).IsRequired();

            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.Property(x => x.HoldToken).HasMaxLength(100).IsRequired();

            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TripId, x.TrainCarSeatId });
            b.HasIndex(x => new { x.TripId, x.Status, x.HoldExpiresAt });

            // FIX: HoldToken không được unique toàn bảng vì 1 request giữ nhiều ghế sẽ tạo nhiều row cùng token
            b.HasIndex(x => new { x.TenantId, x.HoldToken });
            b.HasIndex(x => new { x.TenantId, x.TripId, x.HoldToken });
            b.HasIndex(x => new { x.TenantId, x.TripId, x.TrainCarSeatId, x.Status, x.HoldExpiresAt });


            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainCarSeat>()
                .WithMany()
                .HasForeignKey(x => x.TrainCarSeatId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainTripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.FromTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainTripStopTime>()
                .WithMany()
                .HasForeignKey(x => x.ToTripStopTimeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}