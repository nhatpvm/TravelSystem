// FILE #162: TicketBooking.Domain/Hotels/HotelEntities.cs
using System;

namespace TicketBooking.Domain.Hotels
{
    // =========================
    // Phase 11 - HOTEL PRO (schema "hotels")
    // Multi-tenant + soft delete + audit + rowversion
    // =========================

    public enum HotelStatus
    {
        Draft = 1,
        Active = 2,
        Inactive = 3,
        Suspended = 4
    }

    public enum ImageKind
    {
        Cover = 1,
        Gallery = 2,
        Logo = 3,
        Room = 4,
        Other = 99
    }

    public enum AmenityScope
    {
        Hotel = 1,
        Room = 2
    }

    public enum AmenityKind
    {
        General = 1,
        Safety = 2,
        Bathroom = 3,
        Bedroom = 4,
        FoodDrink = 5,
        Service = 6,
        Accessibility = 7,
        Other = 99
    }

    public enum RoomTypeStatus
    {
        Draft = 1,
        Active = 2,
        Inactive = 3
    }

    public enum InventoryStatus
    {
        Open = 1,
        Closed = 2
    }

    public enum RatePlanStatus
    {
        Draft = 1,
        Active = 2,
        Inactive = 3
    }

    public enum RatePlanType
    {
        Public = 1,
        Corporate = 2,
        Package = 3,
        Promo = 4
    }

    public enum CancellationPolicyType
    {
        FreeCancellation = 1,
        NonRefundable = 2,
        Custom = 3
    }

    public enum PenaltyChargeType
    {
        PercentOfNight = 1, // percent of 1 night base
        PercentOfTotal = 2, // percent of total
        FixedAmount = 3,    // fixed currency amount
        NightCount = 4      // charge N nights
    }

    public enum ExtraServiceType
    {
        ExtraBed = 1,
        Breakfast = 2,
        AirportPickup = 3,
        LateCheckout = 4,
        EarlyCheckin = 5,
        Other = 99
    }

    public enum PricingUnit
    {
        PerNight = 1,
        PerStay = 2,
        PerGuest = 3,
        PerRoom = 4,
        PerUnit = 99
    }

    public enum HoldStatus
    {
        Held = 1,
        Confirmed = 2,
        Cancelled = 3,
        Expired = 4
    }

    public enum ReviewStatus
    {
        Pending = 1,
        Published = 2,
        Hidden = 3
    }

    // =========================
    // 1) hotels.Hotels
    // =========================
    public sealed class Hotel
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Slug { get; set; }

        // Location (link to geo/catalog if you have it; keep as raw fields for now)
        public Guid? LocationId { get; set; }              // optional link to catalog.Locations
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? CountryCode { get; set; } = "VN";
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? TimeZone { get; set; } = "Asia/Ho_Chi_Minh";

        // Content / basic info
        public string? ShortDescription { get; set; }
        public string? DescriptionMarkdown { get; set; }
        public string? DescriptionHtml { get; set; }

        public int StarRating { get; set; }                // 0..5
        public HotelStatus Status { get; set; } = HotelStatus.Draft;

        // Check-in/out defaults
        public TimeOnly? DefaultCheckInTime { get; set; }  // e.g. 14:00
        public TimeOnly? DefaultCheckOutTime { get; set; } // e.g. 12:00

        // Contact + operational
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }

        // SEO
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? CanonicalUrl { get; set; }
        public string? Robots { get; set; }
        public string? OgImageUrl { get; set; }
        public string? SchemaJsonLd { get; set; }

        // Media
        public Guid? CoverMediaAssetId { get; set; }       // optional link to cms.MediaAssets
        public string? CoverImageUrl { get; set; }         // optional external

        // Policies/metadata snapshots
        public string? PoliciesJson { get; set; }          // property rules, child policy, pet policy... (json)
        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        // Soft delete + audit
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 2) hotels.HotelImages
    // =========================
    public sealed class HotelImage
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public ImageKind Kind { get; set; } = ImageKind.Gallery;
        public int SortOrder { get; set; }

        // Prefer MediaAssetId; Url for legacy/external
        public Guid? MediaAssetId { get; set; }            // cms.MediaAssets
        public string? ImageUrl { get; set; }
        public string? Title { get; set; }
        public string? AltText { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 3) hotels.HotelAmenities
    // =========================
    public sealed class HotelAmenity
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public AmenityScope Scope { get; set; } = AmenityScope.Hotel;
        public AmenityKind Kind { get; set; } = AmenityKind.General;

        public string Code { get; set; } = "";             // unique per tenant + scope
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int SortOrder { get; set; }

        public string? IconKey { get; set; }               // for UI mapping
        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 4) hotels.HotelAmenityLinks
    // =========================
    public sealed class HotelAmenityLink
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }
        public Guid AmenityId { get; set; }

        public bool IsHighlighted { get; set; }
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 5) hotels.RoomTypes
    // =========================
    public sealed class RoomType
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? DescriptionMarkdown { get; set; }
        public string? DescriptionHtml { get; set; }

        public int SortOrder { get; set; }

        // Physical attributes
        public int? AreaSquareMeters { get; set; }
        public bool? HasBalcony { get; set; }
        public bool? HasWindow { get; set; }
        public bool? SmokingAllowed { get; set; }

        // Capacity defaults
        public int DefaultAdults { get; set; }
        public int DefaultChildren { get; set; }
        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        public int MaxGuests { get; set; }

        // Quantity model (no room numbers here)
        public int TotalUnits { get; set; }                // base inventory

        // Media
        public Guid? CoverMediaAssetId { get; set; }
        public string? CoverImageUrl { get; set; }

        public RoomTypeStatus Status { get; set; } = RoomTypeStatus.Draft;
        public bool IsActive { get; set; } = true;

        public string? MetadataJson { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 6) hotels.RoomTypeImages
    // =========================
    public sealed class RoomTypeImage
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }

        public ImageKind Kind { get; set; } = ImageKind.Room;
        public int SortOrder { get; set; }

        public Guid? MediaAssetId { get; set; }            // cms.MediaAssets
        public string? ImageUrl { get; set; }
        public string? Title { get; set; }
        public string? AltText { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 7) hotels.RoomAmenities (amenity catalog for room scope)
    // =========================
    public sealed class RoomAmenity
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public AmenityKind Kind { get; set; } = AmenityKind.General;

        public string Code { get; set; } = "";             // unique per tenant
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int SortOrder { get; set; }

        public string? IconKey { get; set; }
        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 8) hotels.RoomAmenityLinks
    // =========================
    public sealed class RoomAmenityLink
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }
        public Guid AmenityId { get; set; }

        public bool IsHighlighted { get; set; }
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 9) hotels.BedTypes
    // =========================
    public sealed class BedType
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Code { get; set; } = "";             // unique per tenant
        public string Name { get; set; } = "";             // e.g. "Queen", "Twin"
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 10) hotels.RoomTypeBeds
    // =========================
    public sealed class RoomTypeBed
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }
        public Guid BedTypeId { get; set; }
        public int Quantity { get; set; } = 1;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 11) hotels.RoomTypeOccupancyRules
    // =========================
    public sealed class RoomTypeOccupancyRule
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }

        public int MinAdults { get; set; }
        public int MaxAdults { get; set; }
        public int MinChildren { get; set; }
        public int MaxChildren { get; set; }
        public int MinGuests { get; set; }
        public int MaxGuests { get; set; }

        public bool AllowsInfants { get; set; } = true;
        public int? MaxInfants { get; set; }

        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 12) hotels.MealPlans
    // =========================
    public sealed class MealPlan
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Code { get; set; } = "";             // unique per tenant
        public string Name { get; set; } = "";             // e.g. "Room only", "Breakfast included"
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 13) hotels.RoomTypeMealPlans
    // =========================
    public sealed class RoomTypeMealPlan
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }
        public Guid MealPlanId { get; set; }

        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 14) hotels.RatePlans
    // =========================
    public sealed class RatePlan
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public string Code { get; set; } = "";             // unique per tenant+hotel
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        public RatePlanType Type { get; set; } = RatePlanType.Public;
        public RatePlanStatus Status { get; set; } = RatePlanStatus.Draft;

        public Guid? CancellationPolicyId { get; set; }     // optional
        public Guid? CheckInOutRuleId { get; set; }         // optional
        public Guid? PropertyPolicyId { get; set; }         // optional

        public bool Refundable { get; set; } = true;
        public bool BreakfastIncluded { get; set; }

        // Constraints
        public int? MinNights { get; set; }
        public int? MaxNights { get; set; }
        public int? MinAdvanceDays { get; set; }            // book before X days
        public int? MaxAdvanceDays { get; set; }
        public bool RequiresGuarantee { get; set; }         // for later payment flows

        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 15) hotels.RatePlanRoomTypes
    // =========================
    public sealed class RatePlanRoomType
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RatePlanId { get; set; }
        public Guid RoomTypeId { get; set; }

        // Base pricing hints (actual daily price in DailyRates)
        public decimal? BasePrice { get; set; }
        public string CurrencyCode { get; set; } = "VND";

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 16) hotels.RatePlanPolicies
    // =========================
    public sealed class RatePlanPolicy
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RatePlanId { get; set; }

        public string? PolicyJson { get; set; }             // merged policy snapshot for this rate plan
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 17) hotels.CancellationPolicies
    // =========================
    public sealed class CancellationPolicy
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public string Code { get; set; } = "";             // unique per tenant+hotel
        public string Name { get; set; } = "";
        public CancellationPolicyType Type { get; set; } = CancellationPolicyType.Custom;

        public string? Description { get; set; }
        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 18) hotels.CancellationPolicyRules
    // =========================
    public sealed class CancellationPolicyRule
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid CancellationPolicyId { get; set; }

        // Rule window: cancel before check-in X hours/days => penalty
        public int? CancelBeforeHours { get; set; }         // e.g. 24 => free if >=24h
        public int? CancelBeforeDays { get; set; }          // optional alternative

        public PenaltyChargeType ChargeType { get; set; } = PenaltyChargeType.PercentOfTotal;
        public decimal ChargeValue { get; set; }            // percent/fixed/nights count (interpret by ChargeType)
        public string CurrencyCode { get; set; } = "VND";   // used when FixedAmount

        public string? Notes { get; set; }
        public int Priority { get; set; }                   // smaller => evaluated first
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 19) hotels.CheckInOutRules
    // =========================
    public sealed class CheckInOutRule
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public string Code { get; set; } = "";             // unique per tenant+hotel
        public string Name { get; set; } = "";

        public TimeOnly CheckInFrom { get; set; }          // e.g. 14:00
        public TimeOnly CheckInTo { get; set; }            // e.g. 23:00
        public TimeOnly CheckOutFrom { get; set; }         // e.g. 07:00
        public TimeOnly CheckOutTo { get; set; }           // e.g. 12:00

        public bool AllowsEarlyCheckIn { get; set; }
        public bool AllowsLateCheckOut { get; set; }

        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 20) hotels.PropertyPolicies
    // =========================
    public sealed class PropertyPolicy
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public string Code { get; set; } = "";             // unique per tenant+hotel
        public string Name { get; set; } = "";

        public string? PolicyJson { get; set; }            // pet/smoking/children/extra guest/security deposit...
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 21) hotels.ExtraServices
    // =========================
    public sealed class ExtraService
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public string Code { get; set; } = "";             // unique per tenant+hotel
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        public ExtraServiceType Type { get; set; } = ExtraServiceType.Other;
        public PricingUnit Unit { get; set; } = PricingUnit.PerStay;

        public bool Taxable { get; set; }
        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 22) hotels.ExtraServicePrices
    // =========================
    public sealed class ExtraServicePrice
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid ExtraServiceId { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }              // inclusive range by convention (config will clarify)
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = "VND";

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 23) hotels.RoomTypeInventories (ARI)
    // =========================
    public sealed class RoomTypeInventory
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }

        public DateOnly Date { get; set; }                 // per day
        public int TotalUnits { get; set; }
        public int SoldUnits { get; set; }
        public int HeldUnits { get; set; }
        public InventoryStatus Status { get; set; } = InventoryStatus.Open;

        public int? MinNights { get; set; }
        public int? MaxNights { get; set; }

        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 24) hotels.DailyRates (ARI)
    // =========================
    public sealed class DailyRate
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RatePlanRoomTypeId { get; set; }

        public DateOnly Date { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = "VND";

        // Optional breakdown
        public decimal? BasePrice { get; set; }
        public decimal? Taxes { get; set; }
        public decimal? Fees { get; set; }

        public bool IsActive { get; set; } = true;
        public string? MetadataJson { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // 25) hotels.InventoryHolds (ARI hold)
    // =========================
    public sealed class InventoryHold
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }

        // Optional links to booking module (Phase 12)
        public Guid? BookingId { get; set; }
        public Guid? BookingItemId { get; set; }

        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }          // exclusive end
        public int Units { get; set; } = 1;

        public HoldStatus Status { get; set; } = HoldStatus.Held;
        public DateTimeOffset HoldExpiresAt { get; set; }

        public string? CorrelationId { get; set; }          // idempotency/correlation
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================================================
    // 4 add-on production tables (requested)
    // =========================================================

    // =========================
    // A1) hotels.HotelContacts
    // =========================
    public sealed class HotelContact
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        public string ContactName { get; set; } = "";
        public string? RoleTitle { get; set; }              // e.g. Manager, Reception
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // A2) hotels.RoomTypePolicies
    // =========================
    public sealed class RoomTypePolicy
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RoomTypeId { get; set; }

        public string? PolicyJson { get; set; }             // e.g. "extra guests", "children", "crib", "deposit"
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // A3) hotels.PromoRateOverrides
    // =========================
    public sealed class PromoRateOverride
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid RatePlanRoomTypeId { get; set; }

        // Optional link to promo module (common.PromoCodes) later
        public Guid? PromoCodeId { get; set; }
        public string? PromoCode { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        // Override rule
        public decimal? OverridePrice { get; set; }
        public decimal? DiscountPercent { get; set; }       // 0..100
        public string CurrencyCode { get; set; } = "VND";

        public string? ConditionsJson { get; set; }         // min nights, day-of-week, advance purchase...
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // =========================
    // A4) hotels.HotelReviews
    // =========================
    public sealed class HotelReview
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid HotelId { get; set; }

        // From booking module later (Phase 12)
        public Guid? BookingId { get; set; }
        public Guid? CustomerUserId { get; set; }

        public int Rating { get; set; }                     // 1..5
        public string? Title { get; set; }
        public string? Content { get; set; }

        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
        public bool IsVerifiedStay { get; set; }            // derived from BookingId in future

        public int HelpfulCount { get; set; }
        public string? MetadataJson { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
