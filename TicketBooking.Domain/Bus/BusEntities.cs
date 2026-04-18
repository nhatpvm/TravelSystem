// FILE #065: TicketBooking.Domain/Bus/BusEntities.cs  (UPDATE - rename Route -> BusRoute to avoid ASP.NET Route conflict)
using System;

namespace TicketBooking.Domain.Bus
{
    public enum StopPointType
    {
        Terminal = 1,
        Pickup = 2,
        Dropoff = 3,
        RestStop = 4,
        Other = 99
    }

    public enum TripStatus
    {
        Draft = 1,
        Published = 2,
        Suspended = 3,
        Cancelled = 4
    }

    public enum SeatHoldStatus
    {
        Held = 1,
        Confirmed = 2,
        Cancelled = 3,
        Expired = 4
    }

    /// <summary>
    /// bus.StopPoints
    /// Linked to catalog.Locations (as you requested).
    /// Represents a stop/terminal/pickup/dropoff place a bus operator uses.
    /// </summary>
    public sealed class StopPoint
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid LocationId { get; set; }                 // catalog.Locations (shared)
        public StopPointType Type { get; set; }

        public string Name { get; set; } = "";               // display name (can differ from Location.Name)
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
    /// bus.Routes
    /// Route A->B for a bus provider.
    ///
    /// IMPORTANT: Renamed from "Route" to "BusRoute" to avoid conflict with Microsoft.AspNetCore.Routing.Route.
    /// </summary>
    public sealed class BusRoute
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid ProviderId { get; set; }                 // catalog.Providers (tenant-owned)

        public string Code { get; set; } = "";               // unique per tenant
        public string Name { get; set; } = "";

        public Guid FromStopPointId { get; set; }            // bus.StopPoints
        public Guid ToStopPointId { get; set; }              // bus.StopPoints

        public int EstimatedMinutes { get; set; }            // optional
        public int DistanceKm { get; set; }                  // optional

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// bus.RouteStops
    /// Ordered stops of a route (for building segment prices i->j).
    /// </summary>
    public sealed class RouteStop
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RouteId { get; set; }                    // bus.Routes (BusRoute)
        public Guid StopPointId { get; set; }

        public int StopIndex { get; set; }                   // 0..n-1
        public int? DistanceFromStartKm { get; set; }         // optional
        public int? MinutesFromStart { get; set; }            // optional

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// bus.Trips
    /// One departure instance. Level 3 pricing/hold works via TripStopTimes + SegmentPrices + SeatHolds.
    /// Policy JSON are stored at trip-level (Option A you chose).
    /// </summary>
    public sealed class Trip
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid ProviderId { get; set; }                 // catalog.Providers
        public Guid RouteId { get; set; }                    // bus.Routes (BusRoute)
        public Guid VehicleId { get; set; }                  // fleet.Vehicles (seatmap capacity)

        public string Code { get; set; } = "";               // unique per tenant
        public string Name { get; set; } = "";

        public TripStatus Status { get; set; } = TripStatus.Published;

        public DateTimeOffset DepartureAt { get; set; }       // first stop depart
        public DateTimeOffset ArrivalAt { get; set; }         // last stop arrive (approx)

        // Trip policies (JSON)
        public string? FareRulesJson { get; set; }
        public string? BaggagePolicyJson { get; set; }
        public string? BoardingPolicyJson { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// bus.TripStopTimes
    /// Stop schedule for a trip (ordered by StopIndex).
    /// </summary>
    public sealed class TripStopTime
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripId { get; set; }
        public Guid StopPointId { get; set; }

        public int StopIndex { get; set; }                   // 0..n-1

        public DateTimeOffset? ArriveAt { get; set; }
        public DateTimeOffset? DepartAt { get; set; }

        public int? MinutesFromStart { get; set; }            // optional for quick calc
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// bus.TripStopPickupPoints
    /// Pickup variants at a specific trip stop (for real-world operations).
    /// </summary>
    public sealed class TripStopPickupPoint
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripStopTimeId { get; set; }

        public string Name { get; set; } = "";
        public string? AddressLine { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsDefault { get; set; } = false;
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
    /// bus.TripStopDropoffPoints
    /// Dropoff variants at a specific trip stop.
    /// </summary>
    public sealed class TripStopDropoffPoint
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripStopTimeId { get; set; }

        public string Name { get; set; } = "";
        public string? AddressLine { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsDefault { get; set; } = false;
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
    /// bus.TripSegmentPrices (Level 3)
    /// Price for segment i->j (FromStopIndex < ToStopIndex).
    /// CurrencyCode FK -> common.Currencies(Code) later (Phase common already exists in your plan).
    /// </summary>
    public sealed class TripSegmentPrice
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripId { get; set; }

        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }

        public int FromStopIndex { get; set; }
        public int ToStopIndex { get; set; }

        public string CurrencyCode { get; set; } = "VND";     // char(3) in config
        public decimal BaseFare { get; set; }                 // base price
        public decimal? TaxesFees { get; set; }               // optional breakdown later
        public decimal TotalPrice { get; set; }               // base + taxes/fees (snapshot-friendly)

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// bus.TripSeatHolds (Level 3)
    /// Hold seat per segment i->j (anti double booking).
    /// When expires, system uses lazy release (your choice).
    /// </summary>
    public sealed class TripSeatHold
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid TripId { get; set; }
        public Guid SeatId { get; set; }                       // fleet.Seats

        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }

        public int FromStopIndex { get; set; }
        public int ToStopIndex { get; set; }

        public SeatHoldStatus Status { get; set; } = SeatHoldStatus.Held;

        public Guid? UserId { get; set; }                      // who holds
        public Guid? BookingId { get; set; }                   // link later when booking created

        public string HoldToken { get; set; } = "";            // unique token per hold (for idempotency)
        public DateTimeOffset HoldExpiresAt { get; set; }      // now + tenant.HoldMinutes

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}