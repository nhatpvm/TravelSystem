// FILE #056: TicketBooking.Api/Controllers/LocationsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Geo;
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

    public sealed class ImportLocationsRequest
    {
        public IFormFile? File { get; set; }
        public LocationType Type { get; set; } = LocationType.BusStation;
        public bool UpdateExisting { get; set; } = true;
        public bool DryRun { get; set; }
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

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] ImportLocationsRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác import của admin." });

        if (req.File is null || req.File.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file Excel." });

        var extension = Path.GetExtension(req.File.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Hiện chỉ hỗ trợ file .xlsx." });

        var worksheet = await XlsxWorksheetReader.ReadFirstWorksheetAsync(req.File, ct);
        var result = await ImportRowsAsync(worksheet, req, ct);

        if (!req.DryRun)
            await _db.SaveChangesAsync(ct);

        return Ok(result);
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

    private async Task<ImportLocationsResult> ImportRowsAsync(
        IReadOnlyList<IReadOnlyList<XlsxCell>> rows,
        ImportLocationsRequest req,
        CancellationToken ct)
    {
        var result = new ImportLocationsResult
        {
            FileName = req.File?.FileName ?? "",
            DryRun = req.DryRun
        };

        var headerRowIndex = FindHeaderRowIndex(rows);
        if (headerRowIndex < 0)
        {
            result.Errors.Add(new ImportLocationRowError(0, "Không tìm thấy dòng tiêu đề trong file Excel."));
            return result;
        }

        var headerMap = BuildHeaderMap(rows[headerRowIndex]);
        if (!TryGetColumn(headerMap, HeaderKind.Name, out var nameColumn))
        {
            result.Errors.Add(new ImportLocationRowError(headerRowIndex + 1, "Thiếu cột tên địa điểm/bến xe."));
            return result;
        }

        var provinces = await _db.Set<Province>().AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var districts = await _db.Set<District>().AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var wards = await _db.Set<Ward>().AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);

        var districtsByProvince = districts.GroupBy(x => x.ProvinceId).ToDictionary(x => x.Key, x => x.ToList());
        var wardsByDistrict = wards.GroupBy(x => x.DistrictId).ToDictionary(x => x.Key, x => x.ToList());

        for (var i = headerRowIndex + 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 1;
            var name = GetCell(row, nameColumn).Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                if (row.All(x => string.IsNullOrWhiteSpace(x.Text)))
                    continue;

                result.Skipped++;
                result.Errors.Add(new ImportLocationRowError(rowNumber, "Dòng có dữ liệu nhưng thiếu tên địa điểm."));
                continue;
            }

            var rowType = ParseLocationType(GetMappedText(row, headerMap, HeaderKind.Type), req.Type);
            var code = TrimToNull(GetMappedText(row, headerMap, HeaderKind.Code));
            var provinceText = TrimToNull(GetMappedText(row, headerMap, HeaderKind.Province));
            var districtText = TrimToNull(GetMappedText(row, headerMap, HeaderKind.District));
            var wardText = TrimToNull(GetMappedText(row, headerMap, HeaderKind.Ward));
            var streetText = TrimToNull(GetMappedText(row, headerMap, HeaderKind.Street));
            var fullAddress = TrimToNull(GetMappedText(row, headerMap, HeaderKind.Address));
            var addressLine = BuildAddressLine(fullAddress, streetText, wardText, districtText, provinceText);

            Province? province = null;
            District? district = null;
            Ward? ward = null;

            if (!string.IsNullOrWhiteSpace(provinceText))
            {
                province = FindByGeoName(provinces, provinceText) ?? FindBestProvinceAlias(provinces, provinceText);
                if (province is null)
                    result.Warnings.Add(new ImportLocationRowError(rowNumber, $"Không khớp tỉnh/thành: {provinceText}."));
            }

            if (province is not null && !string.IsNullOrWhiteSpace(districtText))
            {
                district = FindByGeoName(districtsByProvince.GetValueOrDefault(province.Id), districtText)
                    ?? FindUniqueByGeoName(districts, districtText);
                if (district is null)
                    result.Warnings.Add(new ImportLocationRowError(rowNumber, $"Không khớp quận/huyện: {districtText}."));
            }

            if (district is not null && !string.IsNullOrWhiteSpace(wardText))
            {
                ward = FindByGeoName(wardsByDistrict.GetValueOrDefault(district.Id), wardText)
                    ?? FindUniqueByGeoName(wards, wardText);
                if (ward is null)
                    result.Warnings.Add(new ImportLocationRowError(rowNumber, $"Không khớp phường/xã: {wardText}."));
            }

            var latitude = ParseCoordinate(GetMappedCell(row, headerMap, HeaderKind.Latitude), isLatitude: true);
            var longitude = ParseCoordinate(GetMappedCell(row, headerMap, HeaderKind.Longitude), isLatitude: false);
            var normalizedName = NormalizeSearch(name);

            var existing = await FindExistingImportLocationAsync(rowType, code, normalizedName, ct);
            if (existing is not null && !req.UpdateExisting)
            {
                result.Skipped++;
                continue;
            }

            if (!req.DryRun)
            {
                var entity = existing ?? new Location
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId!.Value,
                    CreatedAt = DateTimeOffset.Now,
                    IsDeleted = false
                };

                entity.Type = rowType;
                entity.Name = name;
                entity.NormalizedName = normalizedName;
                entity.ShortName = null;
                entity.Code = code;
                entity.AirportIataCode = rowType == LocationType.Airport && !string.IsNullOrWhiteSpace(code) && code.Length <= 10 ? code : null;
                entity.TrainStationCode = rowType == LocationType.TrainStation ? code : null;
                entity.BusStationCode = rowType == LocationType.BusStation ? code : null;
                entity.TimeZone = "Asia/Ho_Chi_Minh";
                entity.AddressLine = addressLine;
                entity.ProvinceId = province?.Id;
                entity.DistrictId = district?.Id;
                entity.WardId = ward?.Id;
                entity.Latitude = latitude;
                entity.Longitude = longitude;
                entity.MetadataJson = JsonSerializer.Serialize(new
                {
                    source = "excel-import",
                    importedAt = DateTimeOffset.Now,
                    excelRow = rowNumber,
                    provinceText,
                    districtText,
                    wardText
                });
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.UpdatedAt = existing is null ? null : DateTimeOffset.Now;

                if (existing is null)
                    _db.Locations.Add(entity);
            }

            if (existing is null)
                result.Created++;
            else
                result.Updated++;
        }

        result.TotalRows = result.Created + result.Updated + result.Skipped;
        return result;
    }

    private async Task<Location?> FindExistingImportLocationAsync(
        LocationType type,
        string? code,
        string normalizedName,
        CancellationToken ct)
    {
        var query = _db.Locations.IgnoreQueryFilters()
            .Where(x => x.TenantId == _tenantContext.TenantId && x.Type == type);

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codeValue = code.Trim();
            var byCode = await query.FirstOrDefaultAsync(
                x => x.Code == codeValue || x.BusStationCode == codeValue || x.TrainStationCode == codeValue || x.AirportIataCode == codeValue,
                ct);

            if (byCode is not null)
                return byCode;
        }

        return await query.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, ct);
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

    private static int FindHeaderRowIndex(IReadOnlyList<IReadOnlyList<XlsxCell>> rows)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var map = BuildHeaderMap(rows[i]);
            if (map.ContainsKey(HeaderKind.Name) && (map.ContainsKey(HeaderKind.Code) || map.ContainsKey(HeaderKind.Province)))
                return i;
        }

        return -1;
    }

    private static Dictionary<HeaderKind, int> BuildHeaderMap(IReadOnlyList<XlsxCell> row)
    {
        var map = new Dictionary<HeaderKind, int>();

        for (var i = 0; i < row.Count; i++)
        {
            var kind = ResolveHeader(row[i].Text);
            if (kind.HasValue && !map.ContainsKey(kind.Value))
                map[kind.Value] = i;
        }

        return map;
    }

    private static HeaderKind? ResolveHeader(string value)
    {
        var header = NormalizeHeader(value);
        if (string.IsNullOrWhiteSpace(header)) return null;

        if (header is "LOAI" or "TYPE" || header.Contains("LOAI DIA DIEM"))
            return HeaderKind.Type;
        if ((header.Contains("TEN") && (header.Contains("BEN XE") || header.Contains("DIA DIEM") || header.Contains("LOCATION"))) || header == "NAME")
            return HeaderKind.Name;
        if (header is "MA" or "CODE" || header.Contains("MA BEN"))
            return HeaderKind.Code;
        if (header.Contains("SO NHA") || header.Contains("DUONG"))
            return HeaderKind.Street;
        if (header.Contains("DIA CHI"))
            return HeaderKind.Address;
        if (header.Contains("PHUONG") || header.Contains("XA"))
            return HeaderKind.Ward;
        if (header.Contains("QUAN") || header.Contains("HUYEN"))
            return HeaderKind.District;
        if (header.Contains("TINH") || header.Contains("THANH PHO"))
            return HeaderKind.Province;
        if (header.Contains("VI DO") || header.Contains("LAT"))
            return HeaderKind.Latitude;
        if (header.Contains("KINH DO") || header.Contains("LNG") || header.Contains("LONG"))
            return HeaderKind.Longitude;

        return null;
    }

    private static bool TryGetColumn(Dictionary<HeaderKind, int> headerMap, HeaderKind kind, out int column)
        => headerMap.TryGetValue(kind, out column);

    private static string GetMappedText(IReadOnlyList<XlsxCell> row, Dictionary<HeaderKind, int> headerMap, HeaderKind kind)
        => GetMappedCell(row, headerMap, kind).Text;

    private static XlsxCell GetMappedCell(IReadOnlyList<XlsxCell> row, Dictionary<HeaderKind, int> headerMap, HeaderKind kind)
        => headerMap.TryGetValue(kind, out var column) ? GetCell(row, column) : XlsxCell.Empty;

    private static XlsxCell GetCell(IReadOnlyList<XlsxCell> row, int index)
        => index >= 0 && index < row.Count ? row[index] : XlsxCell.Empty;

    private static string? TrimToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BuildAddressLine(params string?[] parts)
    {
        var clean = parts
            .Select(TrimToNull)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return clean.Length == 0 ? null : string.Join(", ", clean);
    }

    private static T? FindByGeoName<T>(IReadOnlyList<T>? items, string value) where T : class
    {
        if (items is null || items.Count == 0)
            return null;

        var key = NormalizeGeoName(value);
        return items.FirstOrDefault(x => IsGeoNameMatch(NormalizeGeoName(GetGeoName(x)), key));
    }

    private static T? FindUniqueByGeoName<T>(IReadOnlyList<T> items, string value) where T : class
    {
        var key = NormalizeGeoName(value);
        var matches = items
            .Where(x => IsGeoNameMatch(NormalizeGeoName(GetGeoName(x)), key))
            .Take(2)
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    private static Province? FindBestProvinceAlias(IReadOnlyList<Province> provinces, string value)
    {
        var key = NormalizeHeader(value);

        return provinces.FirstOrDefault(x =>
        {
            var candidate = NormalizeGeoName(x.Name);
            return IsGeoNameMatch(candidate, key) ||
                   (key.Contains("HUE", StringComparison.Ordinal) && candidate == "HUE");
        });
    }

    private static bool IsGeoNameMatch(string candidate, string key)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(key))
            return false;

        return candidate == key ||
               candidate.Contains(key, StringComparison.Ordinal) ||
               key.Contains(candidate, StringComparison.Ordinal);
    }

    private static string GetGeoName<T>(T item)
        => item switch
        {
            Province x => x.Name,
            District x => x.Name,
            Ward x => x.Name,
            _ => ""
        };

    private static double? ParseCoordinate(XlsxCell cell, bool isLatitude)
    {
        if (string.IsNullOrWhiteSpace(cell.Text))
            return null;

        if (!double.TryParse(cell.Text.Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) &&
            !double.TryParse(cell.Text.Replace(" ", ""), NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out value))
        {
            return null;
        }

        if (isLatitude && value > 300000 && cell.NumberFormat.Contains("m.yyyy", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var date = DateTime.FromOADate(value);
                value = double.Parse($"{date.Month}.{date.Year:0000}", CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
        else
        {
            while (Math.Abs(value) > 180)
                value /= 10;
        }

        if (isLatitude && (value < 5 || value > 30))
            return null;

        if (!isLatitude && (value < 95 || value > 120))
            return null;

        return Math.Round(value, 6);
    }

    private static LocationType ParseLocationType(string value, LocationType fallback)
    {
        var key = NormalizeHeader(value);
        if (string.IsNullOrWhiteSpace(key))
            return fallback;

        return key switch
        {
            "CITY" or "THANH" or "THANH PHO" => LocationType.City,
            "BUS STATION" or "BEN XE" => LocationType.BusStation,
            "TRAIN STATION" or "GA TAU" => LocationType.TrainStation,
            "AIRPORT" or "SAN BAY" => LocationType.Airport,
            "HOTEL" or "KHACH SAN" => LocationType.Hotel,
            "ATTRACTION" or "DIEM THAM QUAN" => LocationType.Attraction,
            "PICKUP POINT" or "DIEM DON" => LocationType.PickupPoint,
            "DROPOFF POINT" or "DIEM TRA" => LocationType.DropoffPoint,
            "OTHER" or "KHAC" => LocationType.Other,
            _ => Enum.TryParse<LocationType>(value, ignoreCase: true, out var parsed) ? parsed : fallback
        };
    }

    private static string NormalizeHeader(string input)
    {
        var normalized = NormalizeSearch(input);
        normalized = Regex.Replace(normalized, @"[^A-Z0-9]+", " ");
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeGeoName(string input)
    {
        var normalized = NormalizeHeader(input);
        var ignored = new HashSet<string>
        {
            "TINH", "THANH", "PHO", "TP", "QUAN", "HUYEN", "PHUONG", "XA", "THI", "TX", "Q", "P"
        };

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !ignored.Contains(x));

        return string.Join(' ', tokens);
    }

    private enum HeaderKind
    {
        Type,
        Name,
        Code,
        Street,
        Address,
        Ward,
        District,
        Province,
        Latitude,
        Longitude
    }

    private sealed record ImportLocationRowError(int Row, string Message);

    private sealed class ImportLocationsResult
    {
        public string FileName { get; set; } = "";
        public bool DryRun { get; set; }
        public int TotalRows { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<ImportLocationRowError> Warnings { get; } = new();
        public List<ImportLocationRowError> Errors { get; } = new();
    }

    private sealed record XlsxCell(string Text, string NumberFormat)
    {
        public static XlsxCell Empty { get; } = new("", "");
    }

    private static class XlsxWorksheetReader
    {
        private static readonly XNamespace MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static async Task<IReadOnlyList<IReadOnlyList<XlsxCell>>> ReadFirstWorksheetAsync(IFormFile file, CancellationToken ct)
        {
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            memory.Position = 0;

            using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
            var sharedStrings = ReadSharedStrings(archive);
            var styleFormats = ReadStyleFormats(archive);
            var worksheetPath = ResolveFirstWorksheetPath(archive);
            var sheetEntry = archive.GetEntry(worksheetPath)
                ?? throw new InvalidOperationException("Không đọc được worksheet đầu tiên trong file Excel.");

            await using var sheetStream = sheetEntry.Open();
            var sheetDoc = await XDocument.LoadAsync(sheetStream, LoadOptions.None, ct);

            var rows = new List<IReadOnlyList<XlsxCell>>();
            foreach (var rowElement in sheetDoc.Descendants(MainNs + "row"))
            {
                var cells = new List<XlsxCell>();
                foreach (var cellElement in rowElement.Elements(MainNs + "c"))
                {
                    var reference = (string?)cellElement.Attribute("r") ?? "";
                    var columnIndex = ResolveColumnIndex(reference);
                    while (cells.Count <= columnIndex)
                        cells.Add(XlsxCell.Empty);

                    var styleIndexText = (string?)cellElement.Attribute("s");
                    var numberFormat = styleIndexText is not null &&
                                       int.TryParse(styleIndexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var styleIndex) &&
                                       styleFormats.TryGetValue(styleIndex, out var format)
                        ? format
                        : "";

                    cells[columnIndex] = new XlsxCell(ReadCellText(cellElement, sharedStrings), numberFormat);
                }

                rows.Add(cells);
            }

            return rows;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry is null)
                return new List<string>();

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);

            return doc.Descendants(MainNs + "si")
                .Select(x => string.Concat(x.Descendants(MainNs + "t").Select(t => (string?)t ?? "")))
                .ToList();
        }

        private static Dictionary<int, string> ReadStyleFormats(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/styles.xml");
            if (entry is null)
                return new Dictionary<int, string>();

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            var customFormats = doc.Descendants(MainNs + "numFmt")
                .Select(x => new
                {
                    Id = (int?)x.Attribute("numFmtId") ?? -1,
                    Format = (string?)x.Attribute("formatCode") ?? ""
                })
                .Where(x => x.Id >= 0)
                .ToDictionary(x => x.Id, x => x.Format);

            var formats = new Dictionary<int, string>();
            var index = 0;
            foreach (var xf in doc.Descendants(MainNs + "cellXfs").Elements(MainNs + "xf"))
            {
                var id = (int?)xf.Attribute("numFmtId") ?? -1;
                formats[index++] = customFormats.GetValueOrDefault(id, BuiltInNumberFormat(id));
            }

            return formats;
        }

        private static string BuiltInNumberFormat(int id)
            => id switch
            {
                14 => "m/d/yyyy",
                15 => "d-mmm-yy",
                16 => "d-mmm",
                17 => "mmm-yy",
                22 => "m/d/yyyy h:mm",
                _ => ""
            };

        private static string ResolveFirstWorksheetPath(ZipArchive archive)
        {
            var workbookEntry = archive.GetEntry("xl/workbook.xml");
            var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (workbookEntry is null || relsEntry is null)
                return "xl/worksheets/sheet1.xml";

            using var workbookStream = workbookEntry.Open();
            using var relsStream = relsEntry.Open();
            var workbook = XDocument.Load(workbookStream);
            var rels = XDocument.Load(relsStream);
            var firstSheet = workbook.Descendants(MainNs + "sheet").FirstOrDefault();
            var relationshipId = (string?)firstSheet?.Attribute(RelNs + "id");
            if (string.IsNullOrWhiteSpace(relationshipId))
                return "xl/worksheets/sheet1.xml";

            var relationship = rels.Descendants(PackageRelNs + "Relationship")
                .FirstOrDefault(x => string.Equals((string?)x.Attribute("Id"), relationshipId, StringComparison.Ordinal));
            var target = ((string?)relationship?.Attribute("Target"))?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(target))
                return "xl/worksheets/sheet1.xml";

            return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target : $"xl/{target}";
        }

        private static string ReadCellText(XElement cell, IReadOnlyList<string> sharedStrings)
        {
            var type = (string?)cell.Attribute("t");
            if (type == "inlineStr")
                return string.Concat(cell.Descendants(MainNs + "t").Select(x => (string?)x ?? "")).Trim();

            var raw = (string?)cell.Element(MainNs + "v") ?? "";
            if (type == "s" && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex))
                return sharedIndex >= 0 && sharedIndex < sharedStrings.Count ? sharedStrings[sharedIndex].Trim() : "";

            return raw.Trim();
        }

        private static int ResolveColumnIndex(string reference)
        {
            var index = 0;
            foreach (var ch in reference)
            {
                if (!char.IsLetter(ch))
                    break;

                index = (index * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
            }

            return Math.Max(0, index - 1);
        }
    }
}
