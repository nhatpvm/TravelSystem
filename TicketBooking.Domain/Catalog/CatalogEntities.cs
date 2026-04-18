// FILE #052: TicketBooking.Domain/Catalog/CatalogEntities.cs
using System;

namespace TicketBooking.Domain.Catalog
{
    public enum ProviderType
    {
        Bus = 1,
        Train = 2,
        Flight = 3,
        Tour = 4,
        Hotel = 5
    }

    public enum LocationType
    {
        City = 1,
        BusStation = 2,
        TrainStation = 3,
        Airport = 4,
        Hotel = 5,
        Attraction = 6,
        PickupPoint = 7,
        DropoffPoint = 8,
        Other = 99
    }

    /// <summary>
    /// catalog.Locations (shared across all modules)
    /// - Links to geo.* (Province/District/Ward)
    /// - Supports search by normalized name
    /// - Supports airport/station code (IATA/ICAO/StationCode) via Code fields
    /// </summary>
    public sealed class Location
    {
        public Guid Id { get; set; }                         // NEWSEQUENTIALID (Phase 5 defaults)
        public Guid TenantId { get; set; }                   // tenant-owned? -> We'll use tenant override pattern later.
                                                             // For now: tenant-owned to keep data scoped & simple.

        public LocationType Type { get; set; }
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";     // for search (uppercase no-diacritics)
        public string? ShortName { get; set; }

        // Codes (optional)
        public string? Code { get; set; }                    // general code (station code / location code)
        public string? AirportIataCode { get; set; }         // "SGN"
        public string? AirportIcaoCode { get; set; }         // "VVTS"
        public string? TrainStationCode { get; set; }        // optional
        public string? BusStationCode { get; set; }          // optional

        // Timezone (important for flight/hotel)
        public string? TimeZone { get; set; }                // e.g. "Asia/Ho_Chi_Minh"

        // Address / geo link
        public string? AddressLine { get; set; }
        public Guid? ProvinceId { get; set; }
        public Guid? DistrictId { get; set; }
        public Guid? WardId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? MetadataJson { get; set; }            // flexible extensions

        public bool IsActive { get; set; } = true;

        // Standard columns
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // keep varbinary(max) or rowversion later
    }

    /// <summary>
    /// catalog.Providers (tenant-owned) - FULL fields.
    /// A provider can be a bus company, airline, hotel operator, tour company...
    /// </summary>
    public sealed class Provider
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public ProviderType Type { get; set; }

        public string Code { get; set; } = "";               // unique per tenant
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";               // unique per tenant (SEO)
        public string? LegalName { get; set; }

        // Branding
        public string? LogoUrl { get; set; }
        public string? CoverUrl { get; set; }

        // Support contacts
        public string? SupportPhone { get; set; }
        public string? SupportEmail { get; set; }
        public string? WebsiteUrl { get; set; }

        // Address
        public string? AddressLine { get; set; }
        public Guid? LocationId { get; set; }                // link to catalog.Locations (city/base)
        public Guid? ProvinceId { get; set; }
        public Guid? DistrictId { get; set; }
        public Guid? WardId { get; set; }

        // Rating (optional)
        public decimal? RatingAverage { get; set; }          // 0..5
        public int RatingCount { get; set; }

        // Policies/config
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        // Standard columns
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}