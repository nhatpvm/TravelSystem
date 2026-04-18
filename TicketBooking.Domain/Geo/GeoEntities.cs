// FILE #044: TicketBooking.Domain/Geo/GeoEntities.cs
using System;

namespace TicketBooking.Domain.Geo
{
    /// <summary>
    /// geo.Provinces
    /// Source: provinces.open-api.vn (V1 pre-07/2025)
    /// </summary>
    public sealed class Province
    {
        public Guid Id { get; set; }                 // sequential guid
        public int Code { get; set; }                // API code
        public string Name { get; set; } = "";       // Tỉnh/Thành
        public string? NameEn { get; set; }
        public string? Slug { get; set; }            // optional for search
        public string? Type { get; set; }            // "tinh", "thanh-pho" (if you store)
        public int? RegionCode { get; set; }         // optional
        public bool IsActive { get; set; } = true;

        // Standard columns (Phase 5 conventions will apply defaults for Id/CreatedAt/IsDeleted if present)
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    /// <summary>
    /// geo.Districts
    /// </summary>
    public sealed class District
    {
        public Guid Id { get; set; }
        public int Code { get; set; }                 // API code
        public string Name { get; set; } = "";
        public string? NameEn { get; set; }
        public string? Slug { get; set; }
        public string? Type { get; set; }             // "quan", "huyen", ...
        public Guid ProvinceId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    /// <summary>
    /// geo.Wards
    /// </summary>
    public sealed class Ward
    {
        public Guid Id { get; set; }
        public int Code { get; set; }                 // API code
        public string Name { get; set; } = "";
        public string? NameEn { get; set; }
        public string? Slug { get; set; }
        public string? Type { get; set; }             // "phuong", "xa", ...
        public Guid DistrictId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}