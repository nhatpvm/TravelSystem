// FILE #090 (UPGRADE): TicketBooking.Domain/Flight/FlightEntities.cs
using System;

namespace TicketBooking.Domain.Flight
{
    public enum CabinClass
    {
        Economy = 1,
        PremiumEconomy = 2,
        Business = 3,
        First = 4
    }

    public enum FlightStatus
    {
        Draft = 1,
        Published = 2,
        Suspended = 3,
        Cancelled = 4
    }

    public enum OfferStatus
    {
        Active = 1,
        Expired = 2,
        Cancelled = 3
    }

    public enum AncillaryType
    {
        Baggage = 1,
        Meal = 2,
        Seat = 3,
        Insurance = 4,
        Lounge = 5,
        Priority = 6,
        Other = 99
    }

    public enum TaxFeeLineType
    {
        BaseFare = 1,
        Tax = 2,
        Fee = 3,
        Surcharge = 4,
        Discount = 5,
        Other = 99
    }

    /// <summary>
    /// flight.Airlines
    /// Tenant-owned (TenantId). In practice airlines can be global, but V3 rule = all business tables have TenantId.
    /// </summary>
    public sealed class Airline
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Code { get; set; } = "";              // unique per tenant
        public string Name { get; set; } = "";

        public string? IataCode { get; set; }               // e.g. VN
        public string? IcaoCode { get; set; }               // e.g. HVN

        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? SupportPhone { get; set; }
        public string? SupportEmail { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.Airports
    /// Linked to catalog.Locations via LocationId.
    /// </summary>
    public sealed class Airport
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid LocationId { get; set; }                // catalog.Locations

        public string Name { get; set; } = "";
        public string Code { get; set; } = "";              // internal code unique per tenant
        public string? IataCode { get; set; }               // SGN, HAN...
        public string? IcaoCode { get; set; }               // VVTS...

        public string? TimeZone { get; set; }               // Asia/Ho_Chi_Minh
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.AircraftModels
    /// </summary>
    public sealed class AircraftModel
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Code { get; set; } = "";              // A320, B738...
        public string Manufacturer { get; set; } = "";      // Airbus, Boeing...
        public string Model { get; set; } = "";             // A320-200...
        public int? TypicalSeatCapacity { get; set; }

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
    /// flight.Aircrafts
    /// </summary>
    public sealed class Aircraft
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid AircraftModelId { get; set; }
        public Guid AirlineId { get; set; }

        public string Code { get; set; } = "";              // internal code unique per tenant
        public string? Registration { get; set; }           // VN-A123
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.CabinSeatMaps
    /// Seat map per aircraft model + cabin class.
    /// </summary>
    public sealed class CabinSeatMap
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid AircraftModelId { get; set; }
        public CabinClass CabinClass { get; set; }

        public string Code { get; set; } = "";              // unique per tenant (e.g. A320-ECO)
        public string Name { get; set; } = "";

        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }

        /// <summary>
        /// Support future wide-body / special layouts.
        /// Current default = 1.
        /// </summary>
        public int DeckCount { get; set; } = 1;

        public string? LayoutVersion { get; set; }
        public string? SeatLabelScheme { get; set; }        // optional (A,B,C...)
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.CabinSeats
    /// </summary>
    public sealed class CabinSeat
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid CabinSeatMapId { get; set; }

        public string SeatNumber { get; set; } = "";        // e.g. 12A
        public int RowIndex { get; set; }                   // 0-based
        public int ColumnIndex { get; set; }                // 0-based

        /// <summary>
        /// Default = 1. Useful for future multi-deck aircraft layouts.
        /// </summary>
        public int DeckIndex { get; set; } = 1;

        public bool IsAisle { get; set; }
        public bool IsWindow { get; set; }

        /// <summary>
        /// Standard / ExitRow / ExtraLegroom / Bassinet / Preferred...
        /// </summary>
        public string? SeatType { get; set; }

        /// <summary>
        /// Optional seat class override on top of cabin map, e.g. Standard / XL / ExitRow.
        /// </summary>
        public string? SeatClass { get; set; }

        public decimal? PriceModifier { get; set; }         // optional

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.Flights
    /// One scheduled flight instance.
    /// </summary>
    public sealed class Flight
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid AirlineId { get; set; }
        public Guid AircraftId { get; set; }

        public Guid FromAirportId { get; set; }
        public Guid ToAirportId { get; set; }

        public string FlightNumber { get; set; } = "";      // e.g. VN123
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }

        public FlightStatus Status { get; set; } = FlightStatus.Published;

        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.FareClasses
    /// Basic fare class info (Y, M, C...).
    /// </summary>
    public sealed class FareClass
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid AirlineId { get; set; }

        public string Code { get; set; } = "";              // Y/M/C...
        public string Name { get; set; } = "";
        public CabinClass CabinClass { get; set; } = CabinClass.Economy;

        public bool IsRefundable { get; set; }
        public bool IsChangeable { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.FareRules
    /// Detailed rules (change/refund/baggage/no-show...) stored as JSON for flexibility.
    /// </summary>
    public sealed class FareRule
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid FareClassId { get; set; }

        public string RulesJson { get; set; } = "{}";
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.Offers
    /// Stored offer snapshot for a search result (simulated airline booking).
    /// </summary>
    public sealed class Offer
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid AirlineId { get; set; }
        public Guid FlightId { get; set; }
        public Guid FareClassId { get; set; }

        public OfferStatus Status { get; set; } = OfferStatus.Active;

        public string CurrencyCode { get; set; } = "VND";   // char(3) FK -> common.Currencies(Code) in config
        public decimal BaseFare { get; set; }
        public decimal TaxesFees { get; set; }
        public decimal TotalPrice { get; set; }

        public int SeatsAvailable { get; set; } = 9;

        public DateTimeOffset RequestedAt { get; set; }     // when offer generated
        public DateTimeOffset ExpiresAt { get; set; }       // short TTL

        public string? ConditionsJson { get; set; }         // policy snapshot
        public string? MetadataJson { get; set; }           // extra snapshot data

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.OfferSegments
    /// For multi-leg itineraries (transit). For direct flights, 1 segment.
    /// Upgraded so customer/public/admin controllers can work cleanly without reflection.
    /// </summary>
    public sealed class OfferSegment
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid OfferId { get; set; }

        public int SegmentIndex { get; set; }               // 0..n-1

        /// <summary>
        /// Optional references for direct / transit details.
        /// </summary>
        public Guid? FlightId { get; set; }
        public Guid? AirlineId { get; set; }
        public Guid? FareClassId { get; set; }
        public Guid? CabinSeatMapId { get; set; }

        public Guid FromAirportId { get; set; }
        public Guid ToAirportId { get; set; }
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }

        public string? FlightNumber { get; set; }
        public CabinClass? CabinClass { get; set; }

        /// <summary>
        /// Snapshot data at offer time.
        /// </summary>
        public string? BaggagePolicyJson { get; set; }
        public string? FareRulesJson { get; set; }
        public string? MetadataJson { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.AncillaryDefinitions
    /// Extra services (baggage/meal/seat selection...) definition.
    /// </summary>
    public sealed class AncillaryDefinition
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid AirlineId { get; set; }

        public string Code { get; set; } = "";              // unique per tenant
        public string Name { get; set; } = "";
        public AncillaryType Type { get; set; } = AncillaryType.Other;

        public string CurrencyCode { get; set; } = "VND";   // char(3) FK -> common.Currencies(Code) in config
        public decimal Price { get; set; }

        public string? RulesJson { get; set; }              // optional conditions
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// flight.OfferTaxFeeLines
    /// Breakdown lines for an offer.
    /// </summary>
    public sealed class OfferTaxFeeLine
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid OfferId { get; set; }

        public TaxFeeLineType LineType { get; set; } = TaxFeeLineType.Tax;

        public string Code { get; set; } = "";              // VAT, AIRPORT_TAX...
        public string Name { get; set; } = "";
        public string CurrencyCode { get; set; } = "VND";   // char(3) FK -> common.Currencies(Code)
        public decimal Amount { get; set; }

        public int SortOrder { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
