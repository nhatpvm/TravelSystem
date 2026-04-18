// FILE #057: TicketBooking.Domain/Fleet/FleetEntities.cs
using System;

namespace TicketBooking.Domain.Fleet
{
    public enum VehicleType
    {
        Bus = 1,
        Train = 2,
        Airplane = 3,
        TourBus = 4
    }

    public enum SeatType
    {
        Standard = 1,
        Vip = 2,
        SleeperLower = 3,
        SleeperUpper = 4,
        Business = 5,
        Economy = 6
    }

    public enum SeatClass
    {
        Any = 0,
        Economy = 1,
        PremiumEconomy = 2,
        Business = 3,
        First = 4
    }

    /// <summary>
    /// fleet.VehicleModels
    /// Shared model/spec for vehicles (manufacturer/model/year, etc.)
    /// </summary>
    public sealed class VehicleModel
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public VehicleType VehicleType { get; set; }

        public string Manufacturer { get; set; } = "";     // e.g. Thaco, Hyundai, Airbus, Boeing
        public string ModelName { get; set; } = "";        // e.g. Universe, A320
        public int? ModelYear { get; set; }

        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// fleet.SeatMaps
    /// A reusable seat layout per vehicle type (rows/cols/decks).
    /// </summary>
    public sealed class SeatMap
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public VehicleType VehicleType { get; set; }

        public string Code { get; set; } = "";             // unique per tenant
        public string Name { get; set; } = "";

        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }

        public int DeckCount { get; set; } = 1;            // Bus sleeper 2 floors => 2
        public string? LayoutVersion { get; set; }         // optional
        public string? SeatLabelScheme { get; set; }       // optional: A1/A2...

        public bool IsActive { get; set; } = true;

        public string? MetadataJson { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// fleet.Seats
    /// Seats belonging to a SeatMap.
    /// </summary>
    public sealed class Seat
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid SeatMapId { get; set; }

        public string SeatNumber { get; set; } = "";       // e.g. A01, 1A, 12C
        public int RowIndex { get; set; }                  // 0-based
        public int ColumnIndex { get; set; }               // 0-based
        public int DeckIndex { get; set; } = 1;            // 1..DeckCount

        public SeatType SeatType { get; set; } = SeatType.Standard;
        public SeatClass SeatClass { get; set; } = SeatClass.Any;

        public bool IsAisle { get; set; }
        public bool IsWindow { get; set; }

        public decimal? PriceModifier { get; set; }        // optional for premium seats

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// fleet.Vehicles
    /// Actual vehicle owned/operated by provider (tenant-owned).
    /// </summary>
    public sealed class Vehicle
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public VehicleType VehicleType { get; set; }

        public Guid ProviderId { get; set; }               // catalog.Providers
        public Guid? VehicleModelId { get; set; }          // fleet.VehicleModels
        public Guid? SeatMapId { get; set; }               // fleet.SeatMaps (required for bus/train/airplane seat selection)

        public string Code { get; set; } = "";             // unique per tenant
        public string Name { get; set; } = "";

        public string? PlateNumber { get; set; }           // bus/tourbus
        public string? RegistrationNumber { get; set; }    // aircraft registration
        public int SeatCapacity { get; set; }

        // Ops
        public DateTimeOffset? InServiceFrom { get; set; }
        public DateTimeOffset? InServiceTo { get; set; }
        public string? Status { get; set; }                // e.g. Active/Maintenance

        public bool IsActive { get; set; } = true;

        public string? MetadataJson { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// fleet.BusVehicleDetails
    /// Bus-specific details (amenities, sleeper type, etc.)
    /// </summary>
    public sealed class BusVehicleDetail
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid VehicleId { get; set; }                // fleet.Vehicles (VehicleType=Bus/TourBus)

        public string? BusType { get; set; }               // e.g. "Giường nằm", "Ghế ngồi", "Limousine"
        public string? AmenitiesJson { get; set; }         // WiFi, Water, TV, WC...

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}