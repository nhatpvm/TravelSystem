// FILE #056: TicketBooking.Api/Controllers/LocationsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using TicketBooking.Domain.Catalog;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/catalog/locations")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class LocationsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public LocationsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] LocationType? type = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        IQueryable<Location> query = _db.Locations;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = NormalizeSearch(q);
            query = query.Where(x => x.NormalizedName.Contains(keyword) || (x.Code != null && x.Code.Contains(q.Trim())));
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Type,
                x.Name,
                x.NormalizedName,
                x.Code,
                x.AirportIataCode,
                x.TrainStationCode,
                x.BusStationCode,
                x.TimeZone,
                x.ProvinceId,
                ProvinceName = _db.Provinces
                    .Where(p => p.Id == x.ProvinceId)
                    .Select(p => p.Name)
                    .FirstOrDefault(),
                x.DistrictId,
                DistrictName = _db.Districts
                    .Where(d => d.Id == x.DistrictId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                x.WardId,
                WardName = _db.Wards
                    .Where(w => w.Id == x.WardId)
                    .Select(w => w.Name)
                    .FirstOrDefault(),
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        IQueryable<Location> query = _db.Locations;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "Không tìm thấy địa điểm." });

        return Ok(item);
    }

    public sealed class UpsertLocationRequest
    {
        public LocationType Type { get; set; }
        public string Name { get; set; } = "";
        public string? ShortName { get; set; }

        public string? Code { get; set; }
        public string? AirportIataCode { get; set; }
        public string? AirportIcaoCode { get; set; }
        public string? TrainStationCode { get; set; }
        public string? BusStationCode { get; set; }

        public string? TimeZone { get; set; }

        public string? AddressLine { get; set; }
        public Guid? ProvinceId { get; set; }
        public Guid? DistrictId { get; set; }
        public Guid? WardId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertLocationRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        await ValidateGeoReferencesAsync(req, ct);

        var normalized = NormalizeSearch(req.Name);

        var entity = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,

            Type = req.Type,
            Name = req.Name.Trim(),
            NormalizedName = normalized,
            ShortName = req.ShortName?.Trim(),

            Code = req.Code?.Trim(),
            AirportIataCode = req.AirportIataCode?.Trim(),
            AirportIcaoCode = req.AirportIcaoCode?.Trim(),
            TrainStationCode = req.TrainStationCode?.Trim(),
            BusStationCode = req.BusStationCode?.Trim(),

            TimeZone = req.TimeZone?.Trim(),

            AddressLine = req.AddressLine?.Trim(),
            ProvinceId = req.ProvinceId,
            DistrictId = req.DistrictId,
            WardId = req.WardId,
            Latitude = req.Latitude,
            Longitude = req.Longitude,

            MetadataJson = req.MetadataJson,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.Locations.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertLocationRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        await ValidateGeoReferencesAsync(req, ct);

        var entity = await _db.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy địa điểm trong tenant này." });

        entity.Type = req.Type;
        entity.Name = req.Name.Trim();
        entity.NormalizedName = NormalizeSearch(req.Name);
        entity.ShortName = req.ShortName?.Trim();

        entity.Code = req.Code?.Trim();
        entity.AirportIataCode = req.AirportIataCode?.Trim();
        entity.AirportIcaoCode = req.AirportIcaoCode?.Trim();
        entity.TrainStationCode = req.TrainStationCode?.Trim();
        entity.BusStationCode = req.BusStationCode?.Trim();

        entity.TimeZone = req.TimeZone?.Trim();

        entity.AddressLine = req.AddressLine?.Trim();
        entity.ProvinceId = req.ProvinceId;
        entity.DistrictId = req.DistrictId;
        entity.WardId = req.WardId;
        entity.Latitude = req.Latitude;
        entity.Longitude = req.Longitude;

        entity.MetadataJson = req.MetadataJson;
        entity.IsActive = req.IsActive;

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var entity = await _db.Locations.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (entity is null) return NotFound(new { message = "Không tìm thấy địa điểm." });

        _db.Locations.Remove(entity); // interceptor converts to soft delete
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var entity = await _db.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy địa điểm." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private async Task ValidateGeoReferencesAsync(UpsertLocationRequest req, CancellationToken ct)
    {
        if (req.ProvinceId.HasValue && req.ProvinceId.Value != Guid.Empty)
        {
            var provinceExists = await _db.Provinces.AnyAsync(x => x.Id == req.ProvinceId.Value, ct);
            if (!provinceExists)
                throw new InvalidOperationException("ProvinceId không hợp lệ.");
        }

        Guid? districtProvinceId = null;
        if (req.DistrictId.HasValue && req.DistrictId.Value != Guid.Empty)
        {
            districtProvinceId = await _db.Districts
                .Where(x => x.Id == req.DistrictId.Value)
                .Select(x => (Guid?)x.ProvinceId)
                .FirstOrDefaultAsync(ct);

            if (!districtProvinceId.HasValue)
                throw new InvalidOperationException("DistrictId không hợp lệ.");

            if (req.ProvinceId.HasValue && req.ProvinceId.Value != districtProvinceId.Value)
                throw new InvalidOperationException("DistrictId không thuộc ProvinceId đã chọn.");
        }

        if (req.WardId.HasValue && req.WardId.Value != Guid.Empty)
        {
            var wardInfo = await _db.Wards
                .Where(x => x.Id == req.WardId.Value)
                .Select(x => new { x.DistrictId })
                .FirstOrDefaultAsync(ct);

            if (wardInfo is null)
                throw new InvalidOperationException("WardId không hợp lệ.");

            if (req.DistrictId.HasValue && req.DistrictId.Value != wardInfo.DistrictId)
                throw new InvalidOperationException("WardId không thuộc DistrictId đã chọn.");
        }
    }

    private static void Validate(UpsertLocationRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Tên là bắt buộc.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Tên tối đa 200 ký tự.");

        if (req.ShortName is not null && req.ShortName.Length > 100) throw new InvalidOperationException("Tên ngắn tối đa 100 ký tự.");
        if (req.Code is not null && req.Code.Length > 50) throw new InvalidOperationException("Mã tối đa 50 ký tự.");

        if (req.AirportIataCode is not null && req.AirportIataCode.Length > 10) throw new InvalidOperationException("Mã IATA tối đa 10 ký tự.");
        if (req.AirportIcaoCode is not null && req.AirportIcaoCode.Length > 10) throw new InvalidOperationException("Mã ICAO tối đa 10 ký tự.");

        if (req.TrainStationCode is not null && req.TrainStationCode.Length > 50) throw new InvalidOperationException("Mã ga tàu tối đa 50 ký tự.");
        if (req.BusStationCode is not null && req.BusStationCode.Length > 50) throw new InvalidOperationException("Mã bến xe tối đa 50 ký tự.");

        if (req.TimeZone is not null && req.TimeZone.Length > 64) throw new InvalidOperationException("Múi giờ tối đa 64 ký tự.");
        if (req.AddressLine is not null && req.AddressLine.Length > 300) throw new InvalidOperationException("Địa chỉ tối đa 300 ký tự.");
    }

    /// <summary>
    /// Normalize Vietnamese for search: uppercase + remove diacritics + collapse spaces.
    /// </summary>
    private static string NormalizeSearch(string input)
    {
        input = input.Trim();
        if (input.Length == 0) return "";

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);

        // Special Vietnamese char
        noDiacritics = noDiacritics.Replace('đ', 'd').Replace('Đ', 'D');

        // Upper + collapse spaces
        var upper = noDiacritics.ToUpperInvariant();
        return string.Join(' ', upper.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
