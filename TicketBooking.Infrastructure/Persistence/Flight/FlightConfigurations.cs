// FILE #091 (FIX): TicketBooking.Infrastructure/Persistence/Flight/FlightConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Flight;
using FlightEntity = TicketBooking.Domain.Flight.Flight;

namespace TicketBooking.Infrastructure.Persistence.Flight
{
    /// <summary>
    /// Phase 10 - Flight module EF Core configurations (multi-schema: flight.*)
    /// Conventions:
    /// - PK: UNIQUEIDENTIFIER with NEWSEQUENTIALID()
    /// - Soft delete: IsDeleted default 0
    /// - Audit: CreatedAt default VN time (datetimeoffset +07:00)
    /// - RowVersion: rowversion concurrency token
    /// - Multi-tenant: TenantId required, indexed, unique constraints include TenantId
    /// </summary>
    public sealed class FlightConfigurations :
        IEntityTypeConfiguration<Airline>,
        IEntityTypeConfiguration<Airport>,
        IEntityTypeConfiguration<AircraftModel>,
        IEntityTypeConfiguration<Aircraft>,
        IEntityTypeConfiguration<CabinSeatMap>,
        IEntityTypeConfiguration<CabinSeat>,
        IEntityTypeConfiguration<FlightEntity>,
        IEntityTypeConfiguration<FareClass>,
        IEntityTypeConfiguration<FareRule>,
        IEntityTypeConfiguration<Offer>,
        IEntityTypeConfiguration<OfferSegment>,
        IEntityTypeConfiguration<AncillaryDefinition>,
        IEntityTypeConfiguration<OfferTaxFeeLine>
    {
        private const string Schema = "flight";
        private const string VnNowSql = "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')";

        public void Configure(EntityTypeBuilder<Airline> b)
        {
            b.ToTable("Airlines", Schema);

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.IataCode).HasMaxLength(8);
            b.Property(x => x.IcaoCode).HasMaxLength(8);

            b.Property(x => x.LogoUrl).HasMaxLength(1000);
            b.Property(x => x.WebsiteUrl).HasMaxLength(1000);
            b.Property(x => x.SupportPhone).HasMaxLength(50);
            b.Property(x => x.SupportEmail).HasMaxLength(200);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IataCode })
                .IsUnique()
                .HasFilter("[IataCode] IS NOT NULL");
            b.HasIndex(x => new { x.TenantId, x.IcaoCode })
                .IsUnique()
                .HasFilter("[IcaoCode] IS NOT NULL");
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<Airport> b)
        {
            b.ToTable("Airports", Schema);

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.LocationId).IsRequired();

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.IataCode).HasMaxLength(8);
            b.Property(x => x.IcaoCode).HasMaxLength(8);
            b.Property(x => x.TimeZone).HasMaxLength(64);

            b.Property(x => x.Latitude);
            b.Property(x => x.Longitude);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IataCode })
                .IsUnique()
                .HasFilter("[IataCode] IS NOT NULL");
            b.HasIndex(x => new { x.TenantId, x.IcaoCode })
                .IsUnique()
                .HasFilter("[IcaoCode] IS NOT NULL");
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<AircraftModel> b)
        {
            b.ToTable("AircraftModels", Schema);

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Manufacturer).HasMaxLength(100).IsRequired();
            b.Property(x => x.Model).HasMaxLength(100).IsRequired();

            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<Aircraft> b)
        {
            b.ToTable("Aircrafts", Schema);

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.AircraftModelId).IsRequired();
            b.Property(x => x.AirlineId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Registration).HasMaxLength(30);
            b.Property(x => x.Name).HasMaxLength(200);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<AircraftModel>()
                .WithMany()
                .HasForeignKey(x => x.AircraftModelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Airline>()
                .WithMany()
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Registration })
                .IsUnique()
                .HasFilter("[Registration] IS NOT NULL");
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<CabinSeatMap> b)
        {
            b.ToTable("CabinSeatMaps", Schema, tb =>
            {
                tb.HasCheckConstraint("CK_flight_CabinSeatMaps_Rows", "[TotalRows] > 0");
                tb.HasCheckConstraint("CK_flight_CabinSeatMaps_Cols", "[TotalColumns] > 0");
                tb.HasCheckConstraint("CK_flight_CabinSeatMaps_DeckCount", "[DeckCount] > 0");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.AircraftModelId).IsRequired();
            b.Property(x => x.CabinClass).HasConversion<int>();

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.TotalRows).IsRequired();
            b.Property(x => x.TotalColumns).IsRequired();
            b.Property(x => x.DeckCount).HasDefaultValue(1).IsRequired();

            b.Property(x => x.LayoutVersion).HasMaxLength(50);
            b.Property(x => x.SeatLabelScheme).HasMaxLength(50);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<AircraftModel>()
                .WithMany()
                .HasForeignKey(x => x.AircraftModelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.AircraftModelId, x.CabinClass });
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<CabinSeat> b)
        {
            b.ToTable("CabinSeats", Schema, tb =>
            {
                tb.HasCheckConstraint("CK_flight_CabinSeats_RowIndex", "[RowIndex] >= 0");
                tb.HasCheckConstraint("CK_flight_CabinSeats_ColumnIndex", "[ColumnIndex] >= 0");
                tb.HasCheckConstraint("CK_flight_CabinSeats_DeckIndex", "[DeckIndex] > 0");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.CabinSeatMapId).IsRequired();
            b.Property(x => x.SeatNumber).HasMaxLength(10).IsRequired();

            b.Property(x => x.RowIndex).IsRequired();
            b.Property(x => x.ColumnIndex).IsRequired();
            b.Property(x => x.DeckIndex).HasDefaultValue(1).IsRequired();

            b.Property(x => x.SeatType).HasMaxLength(50);
            b.Property(x => x.SeatClass).HasMaxLength(50);
            b.Property(x => x.PriceModifier).HasColumnType("decimal(18,2)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<CabinSeatMap>()
                .WithMany()
                .HasForeignKey(x => x.CabinSeatMapId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.CabinSeatMapId, x.SeatNumber }).IsUnique();
            b.HasIndex(x => new { x.CabinSeatMapId, x.DeckIndex, x.RowIndex, x.ColumnIndex }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<FlightEntity> b)
        {
            b.ToTable("Flights", Schema, tb =>
            {
                tb.HasCheckConstraint("CK_flight_Flights_ArrivalAfterDeparture", "[ArrivalAt] > [DepartureAt]");
                tb.HasCheckConstraint("CK_flight_Flights_FromToDifferent", "[FromAirportId] <> [ToAirportId]");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.AirlineId).IsRequired();
            b.Property(x => x.AircraftId).IsRequired();
            b.Property(x => x.FromAirportId).IsRequired();
            b.Property(x => x.ToAirportId).IsRequired();

            b.Property(x => x.FlightNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.DepartureAt).HasColumnType("datetimeoffset").IsRequired();
            b.Property(x => x.ArrivalAt).HasColumnType("datetimeoffset").IsRequired();
            b.Property(x => x.Status).HasConversion<int>();

            b.Property(x => x.Notes).HasMaxLength(2000);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<Airline>()
                .WithMany()
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Aircraft>()
                .WithMany()
                .HasForeignKey(x => x.AircraftId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Airport>()
                .WithMany()
                .HasForeignKey(x => x.FromAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Airport>()
                .WithMany()
                .HasForeignKey(x => x.ToAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.FlightNumber, x.DepartureAt }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.AirlineId, x.DepartureAt });
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<FareClass> b)
        {
            b.ToTable("FareClasses", Schema);

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.AirlineId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(10).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.CabinClass).HasConversion<int>();

            b.Property(x => x.IsRefundable).HasDefaultValue(false);
            b.Property(x => x.IsChangeable).HasDefaultValue(false);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<Airline>()
                .WithMany()
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.AirlineId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<FareRule> b)
        {
            b.ToTable("FareRules", Schema);

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.FareClassId).IsRequired();
            b.Property(x => x.RulesJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}");
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<FareClass>()
                .WithMany()
                .HasForeignKey(x => x.FareClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.FareClassId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<Offer> b)
        {
            b.ToTable("Offers", Schema, tb =>
            {
                tb.HasCheckConstraint("CK_flight_Offers_ExpiresAt", "[ExpiresAt] > [RequestedAt]");
                tb.HasCheckConstraint("CK_flight_Offers_TotalPrice", "[TotalPrice] >= 0");
                tb.HasCheckConstraint("CK_flight_Offers_SeatsAvailable", "[SeatsAvailable] >= 0");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.AirlineId).IsRequired();
            b.Property(x => x.FlightId).IsRequired();
            b.Property(x => x.FareClassId).IsRequired();

            b.Property(x => x.Status).HasConversion<int>();

            b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsRequired();
            b.Property(x => x.BaseFare).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxesFees).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");

            b.Property(x => x.SeatsAvailable).HasDefaultValue(9);

            b.Property(x => x.RequestedAt).HasColumnType("datetimeoffset").IsRequired();
            b.Property(x => x.ExpiresAt).HasColumnType("datetimeoffset").IsRequired();

            b.Property(x => x.ConditionsJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<Airline>()
                .WithMany()
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<FlightEntity>()
                .WithMany()
                .HasForeignKey(x => x.FlightId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<FareClass>()
                .WithMany()
                .HasForeignKey(x => x.FareClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.FlightId });
            b.HasIndex(x => new { x.TenantId, x.ExpiresAt });
        }

        public void Configure(EntityTypeBuilder<OfferSegment> b)
        {
            b.ToTable("OfferSegments", Schema, tb =>
            {
                tb.HasCheckConstraint("CK_flight_OfferSegments_ArrivalAfterDeparture", "[ArrivalAt] > [DepartureAt]");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.OfferId).IsRequired();
            b.Property(x => x.SegmentIndex).IsRequired();

            b.Property(x => x.FlightId);
            b.Property(x => x.AirlineId);
            b.Property(x => x.FareClassId);
            b.Property(x => x.CabinSeatMapId);

            b.Property(x => x.FromAirportId).IsRequired();
            b.Property(x => x.ToAirportId).IsRequired();
            b.Property(x => x.DepartureAt).HasColumnType("datetimeoffset").IsRequired();
            b.Property(x => x.ArrivalAt).HasColumnType("datetimeoffset").IsRequired();

            b.Property(x => x.FlightNumber).HasMaxLength(20);
            b.Property(x => x.CabinClass).HasConversion<int?>();

            b.Property(x => x.BaggagePolicyJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.FareRulesJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<Offer>()
                .WithMany()
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<FlightEntity>()
                .WithMany()
                .HasForeignKey(x => x.FlightId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Airline>()
                .WithMany()
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<FareClass>()
                .WithMany()
                .HasForeignKey(x => x.FareClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<CabinSeatMap>()
                .WithMany()
                .HasForeignKey(x => x.CabinSeatMapId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Airport>()
                .WithMany()
                .HasForeignKey(x => x.FromAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Airport>()
                .WithMany()
                .HasForeignKey(x => x.ToAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.OfferId, x.SegmentIndex }).IsUnique();
        }

        public void Configure(EntityTypeBuilder<AncillaryDefinition> b)
        {
            b.ToTable("AncillaryDefinitions", Schema, tb =>
            {
                tb.HasCheckConstraint("CK_flight_AncillaryDefinitions_Price", "[Price] >= 0");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.AirlineId).IsRequired();

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Type).HasConversion<int>();

            b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsRequired();
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");

            b.Property(x => x.RulesJson).HasColumnType("nvarchar(max)");

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<Airline>()
                .WithMany()
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.AirlineId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<OfferTaxFeeLine> b)
        {
            b.ToTable("OfferTaxFeeLines", Schema, tb =>
            {
                tb.HasCheckConstraint("CK_flight_OfferTaxFeeLines_Amount", "[Amount] >= 0");
                tb.HasCheckConstraint("CK_flight_OfferTaxFeeLines_SortOrder", "[SortOrder] >= 0");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            b.Property(x => x.TenantId).IsRequired();

            b.Property(x => x.OfferId).IsRequired();
            b.Property(x => x.LineType).HasConversion<int>();

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsRequired();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");

            b.Property(x => x.SortOrder).HasDefaultValue(0);

            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset").HasDefaultValueSql(VnNowSql);
            b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");
            b.Property(x => x.CreatedByUserId);
            b.Property(x => x.UpdatedByUserId);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasOne<Offer>()
                .WithMany()
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.OfferId, x.SortOrder, x.Code }).IsUnique();
        }
    }
}

