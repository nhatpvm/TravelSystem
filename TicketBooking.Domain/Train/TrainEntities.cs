// FILE #076: TicketBooking.Domain/Train/TrainEntities.cs
using System;

namespace TicketBooking.Domain.Train
{
    public enum TrainStopPointType
    {
        Station = 1,
        Other = 99
    }

    public enum TrainTripStatus
    {
        Draft = 1,
        Published = 2,
        Suspended = 3,
        Cancelled = 4
    }

    public enum TrainSeatHoldStatus
    {
        Held = 1,
        Confirmed = 2,
        Cancelled = 3,
        Expired = 4
    }

    public enum TrainCarType
    {
        SeatCoach = 1,
        Sleeper = 2,
        Business = 3,
        Other = 99
    }

    public enum TrainSeatType
    {
        Seat = 1,
        UpperBerth = 2,
        LowerBerth = 3
    }

    /// <summary>
    /// train.StopPoints (Ga tàu)
    /// Linked to catalog.Locations (you required).
    /// </summary>
    public sealed class TrainStopPoint
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid LocationId { get; set; } // catalog.Locations
        public TrainStopPointType Type { get; set; } = TrainStopPointType.Station;

        public string Name { get; set; } = "";
        public string? AddressLine { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? Notes { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.Routes
    /// Renamed to TrainRoute to avoid conflict with ASP.NET Route type.
    /// </summary>
    public sealed class TrainRoute
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid ProviderId { get; set; } // catalog.Providers (tenant-owned, Type=Train)

        public string Code { get; set; } = "";
        public string Name { get; set; } = "";

        public Guid FromStopPointId { get; set; } // train.StopPoints
        public Guid ToStopPointId { get; set; }   // train.StopPoints

        public int EstimatedMinutes { get; set; }
        public int DistanceKm { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.RouteStops
    /// Ordered stations of a route (for segment prices i->j).
    /// </summary>
    public sealed class TrainRouteStop
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RouteId { get; set; }       // train.Routes (TrainRoute)
        public Guid StopPointId { get; set; }   // train.StopPoints

        public int StopIndex { get; set; }      // 0..n-1
        public int? DistanceFromStartKm { get; set; }
        public int? MinutesFromStart { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.Trips
    /// One scheduled train departure instance.
    /// </summary>
    public sealed class TrainTrip
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid ProviderId { get; set; } // catalog.Providers
        public Guid RouteId { get; set; }    // train.Routes
        public string TrainNumber { get; set; } = ""; // e.g. SE1/SE2...

        public string Code { get; set; } = ""; // unique per tenant
        public string Name { get; set; } = "";

        public TrainTripStatus Status { get; set; } = TrainTripStatus.Published;

        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }

        public string? FareRulesJson { get; set; }
        public string? BaggagePolicyJson { get; set; }
        public string? BoardingPolicyJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.TripStopTimes
    /// Station schedule for a trip (ordered by StopIndex).
    /// </summary>
    public sealed class TrainTripStopTime
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripId { get; set; }        // train.Trips
        public Guid StopPointId { get; set; }   // train.StopPoints

        public int StopIndex { get; set; }

        public DateTimeOffset? ArriveAt { get; set; }
        public DateTimeOffset? DepartAt { get; set; }

        public int? MinutesFromStart { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.TripSegmentPrices (Level 3)
    /// Price per segment i->j.
    /// </summary>
    public sealed class TrainTripSegmentPrice
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripId { get; set; }

        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }

        public int FromStopIndex { get; set; }
        public int ToStopIndex { get; set; }

        public string CurrencyCode { get; set; } = "VND"; // char(3) later FK to common.Currencies(Code)
        public decimal BaseFare { get; set; }
        public decimal? TaxesFees { get; set; }
        public decimal TotalPrice { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.TrainCars
    /// Cars/coaches for a specific trip.
    /// </summary>
    public sealed class TrainCar
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripId { get; set; } // train.Trips

        public string CarNumber { get; set; } = ""; // e.g. "01", "02", "A1"...
        public TrainCarType CarType { get; set; } = TrainCarType.SeatCoach;

        public string? CabinClass { get; set; } // e.g. "Economy", "Business"
        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.TrainCarSeats
    /// Seats/berths per car. Supports sleeper (upper/lower) + compartments.
    /// </summary>
    public sealed class TrainCarSeat
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid CarId { get; set; } // train.TrainCars

        public string SeatNumber { get; set; } = ""; // e.g. "01A", "12B", "K4-01"
        public TrainSeatType SeatType { get; set; } = TrainSeatType.Seat;

        public string? CompartmentCode { get; set; } // e.g. "K4", "K6"
        public int? CompartmentIndex { get; set; }   // 1..n

        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        public bool IsWindow { get; set; }
        public bool IsAisle { get; set; }

        public string? SeatClass { get; set; }       // optional (e.g. "SoftSeat", "HardSeat")
        public decimal? PriceModifier { get; set; }  // optional

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// train.TripSeatHolds (Level 3)
    /// Hold a seat/berth per segment i->j (anti double booking).
    /// Uses lazy expiry like BUS.
    /// </summary>
    public sealed class TrainTripSeatHold
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripId { get; set; }
        public Guid TrainCarSeatId { get; set; } // train.TrainCarSeats

        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }

        public int FromStopIndex { get; set; }
        public int ToStopIndex { get; set; }

        public TrainSeatHoldStatus Status { get; set; } = TrainSeatHoldStatus.Held;

        public Guid? UserId { get; set; }
        public Guid? BookingId { get; set; }

        public string HoldToken { get; set; } = "";
        public DateTimeOffset HoldExpiresAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}