// FILE #077 (FIXED): TicketBooking.Infrastructure/Persistence/Train/TrainConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Commerce;
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
            b.Property(x => x.PlatformCode).HasMaxLength(30);
            b.Property(x => x.TrackCode).HasMaxLength(30);
            b.Property(x => x.BoardingGate).HasMaxLength(30);
            b.Property(x => x.BoardingStatus).HasMaxLength(50);
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

    public sealed class TrainSeatBlockConfiguration : IEntityTypeConfiguration<TrainSeatBlock>
    {
        public void Configure(EntityTypeBuilder<TrainSeatBlock> b)
        {
            b.ToTable("SeatBlocks", "train", tb =>
            {
                tb.HasCheckConstraint("CK_train_SeatBlocks_StopIndex", "[FromStopIndex] < [ToStopIndex]");
            });

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();
            b.Property(x => x.TrainCarSeatId).IsRequired();
            b.Property(x => x.FromTripStopTimeId).IsRequired();
            b.Property(x => x.ToTripStopTimeId).IsRequired();
            b.Property(x => x.Reason).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.ReasonText).HasMaxLength(200);
            b.Property(x => x.Note).HasColumnType("nvarchar(max)");
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.TripId, x.Status });
            b.HasIndex(x => new { x.TripId, x.TrainCarSeatId, x.Status, x.FromStopIndex, x.ToStopIndex });
            b.HasIndex(x => new { x.TripId, x.FromStopIndex, x.ToStopIndex });

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

    public sealed class TrainOperationalEventConfiguration : IEntityTypeConfiguration<TrainOperationalEvent>
    {
        public void Configure(EntityTypeBuilder<TrainOperationalEvent> b)
        {
            b.ToTable("OperationalEvents", "train");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();
            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.OldPlatformCode).HasMaxLength(30);
            b.Property(x => x.NewPlatformCode).HasMaxLength(30);
            b.Property(x => x.OldTrackCode).HasMaxLength(30);
            b.Property(x => x.NewTrackCode).HasMaxLength(30);
            b.Property(x => x.ReasonCode).HasMaxLength(50).IsRequired();
            b.Property(x => x.ReasonText).HasMaxLength(300);
            b.Property(x => x.InternalNote).HasColumnType("nvarchar(max)");
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.TripId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.Type, x.Status });

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainFareClassConfiguration : IEntityTypeConfiguration<TrainFareClass>
    {
        public void Configure(EntityTypeBuilder<TrainFareClass> b)
        {
            b.ToTable("FareClasses", "train");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(120).IsRequired();
            b.Property(x => x.SeatType).HasConversion<int>().IsRequired();
            b.Property(x => x.Description).HasMaxLength(300);
            b.Property(x => x.DefaultModifier).HasColumnType("decimal(18,2)");
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.SeatType, x.IsActive });
        }
    }

    public sealed class TrainFareRuleConfiguration : IEntityTypeConfiguration<TrainFareRule>
    {
        public void Configure(EntityTypeBuilder<TrainFareRule> b)
        {
            b.ToTable("FareRules", "train", tb =>
            {
                tb.HasCheckConstraint("CK_train_FareRules_StopIndex", "[FromStopIndex] < [ToStopIndex]");
            });

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.FareClassId).IsRequired();
            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(x => x.BaseFare).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxesFees).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.RouteId, x.FareClassId, x.FromStopIndex, x.ToStopIndex });
            b.HasIndex(x => new { x.TenantId, x.TripId, x.FareClassId, x.FromStopIndex, x.ToStopIndex });
            b.HasIndex(x => new { x.TenantId, x.IsActive });

            b.HasOne<TrainRoute>()
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainFareClass>()
                .WithMany()
                .HasForeignKey(x => x.FareClassId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainTicketCheckInConfiguration : IEntityTypeConfiguration<TrainTicketCheckIn>
    {
        public void Configure(EntityTypeBuilder<TrainTicketCheckIn> b)
        {
            b.ToTable("TicketCheckIns", "train");

            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TripId).IsRequired();
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.TicketId).IsRequired();
            b.Property(x => x.TicketCode).HasMaxLength(80).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.CarNumber).HasMaxLength(20);
            b.Property(x => x.SeatNumber).HasMaxLength(30);
            b.Property(x => x.PassengerName).HasMaxLength(200);
            b.Property(x => x.DocumentNumber).HasMaxLength(80);
            b.Property(x => x.PlatformCode).HasMaxLength(30);
            b.Property(x => x.GateCode).HasMaxLength(30);
            b.Property(x => x.DeviceCode).HasMaxLength(80);
            b.Property(x => x.Note).HasColumnType("nvarchar(max)");
            b.Property(x => x.RejectReason).HasMaxLength(300);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.TicketCode });
            b.HasIndex(x => new { x.TenantId, x.TicketCode, x.TrainCarSeatId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.TripId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.OrderId });

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<CustomerOrder>()
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<CustomerTicket>()
                .WithMany()
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainCarSeat>()
                .WithMany()
                .HasForeignKey(x => x.TrainCarSeatId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainTicketChangeRequestConfiguration : IEntityTypeConfiguration<TrainTicketChangeRequest>
    {
        public void Configure(EntityTypeBuilder<TrainTicketChangeRequest> b)
        {
            b.ToTable("TicketChangeRequests", "train");

            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.OriginalOrderId).IsRequired();
            b.Property(x => x.OriginalTripId).IsRequired();
            b.Property(x => x.NewTripId).IsRequired();
            b.Property(x => x.NewHoldToken).HasMaxLength(100).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(x => x.OriginalAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.NewAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.ChangeFeeAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.FareDifferenceAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.PayableDifferenceAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.ReasonText).HasMaxLength(300);
            b.Property(x => x.StaffNote).HasColumnType("nvarchar(max)");
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.HasIndex(x => new { x.TenantId, x.OriginalOrderId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.NewHoldToken });
            b.HasIndex(x => new { x.TenantId, x.NewTripId });

            b.HasOne<CustomerOrder>()
                .WithMany()
                .HasForeignKey(x => x.OriginalOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.OriginalTripId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TrainTrip>()
                .WithMany()
                .HasForeignKey(x => x.NewTripId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainSetConfiguration : IEntityTypeConfiguration<TrainSet>
    {
        public void Configure(EntityTypeBuilder<TrainSet> b)
        {
            b.ToTable("TrainSets", "train");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion).HasColumnType("varbinary(max)").IsConcurrencyToken();
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Status, x.IsActive });
        }
    }

    public sealed class TrainSetCarTemplateConfiguration : IEntityTypeConfiguration<TrainSetCarTemplate>
    {
        public void Configure(EntityTypeBuilder<TrainSetCarTemplate> b)
        {
            b.ToTable("TrainSetCarTemplates", "train");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TrainSetId).IsRequired();
            b.Property(x => x.CarNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.CarType).HasConversion<int>().IsRequired();
            b.Property(x => x.CabinClass).HasMaxLength(50);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion).HasColumnType("varbinary(max)").IsConcurrencyToken();
            b.HasIndex(x => new { x.TrainSetId, x.CarNumber }).IsUnique();
            b.HasIndex(x => new { x.TrainSetId, x.SortOrder });
            b.HasOne<TrainSet>().WithMany().HasForeignKey(x => x.TrainSetId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TrainSetSeatTemplateConfiguration : IEntityTypeConfiguration<TrainSetSeatTemplate>
    {
        public void Configure(EntityTypeBuilder<TrainSetSeatTemplate> b)
        {
            b.ToTable("TrainSetSeatTemplates", "train");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.TrainSetCarTemplateId).IsRequired();
            b.Property(x => x.SeatNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.SeatType).HasConversion<int>().IsRequired();
            b.Property(x => x.CompartmentCode).HasMaxLength(20);
            b.Property(x => x.SeatClass).HasMaxLength(50);
            b.Property(x => x.PriceModifier).HasColumnType("decimal(18,2)");
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.RowVersion).HasColumnType("varbinary(max)").IsConcurrencyToken();
            b.HasIndex(x => new { x.TrainSetCarTemplateId, x.SeatNumber }).IsUnique();
            b.HasIndex(x => new { x.TrainSetCarTemplateId, x.CompartmentCode });
            b.HasOne<TrainSetCarTemplate>().WithMany().HasForeignKey(x => x.TrainSetCarTemplateId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
