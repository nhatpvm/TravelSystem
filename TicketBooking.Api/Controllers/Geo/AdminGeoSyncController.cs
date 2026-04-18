// FILE #051: TicketBooking.Api/Controllers/AdminGeoSyncController.cs  (UPDATE - log sync)
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketBooking.Domain.Geo;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/geo")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminGeoSyncController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdminGeoSyncController> _logger;

    public AdminGeoSyncController(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<AdminGeoSyncController> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromQuery] int depth = 3, CancellationToken ct = default)
    {
        if (depth < 1 || depth > 3)
            return BadRequest(new { message = "depth phải nằm trong khoảng 1..3." });

        var url = $"https://provinces.open-api.vn/api/?depth={depth}";
        var now = DateTimeOffset.Now;

        var log = new GeoSyncLog
        {
            Id = Guid.NewGuid(),
            Source = "provinces.open-api.vn",
            Url = url,
            Depth = depth,
            CreatedAt = now
        };

        _logger.LogInformation("Geo sync started. url={Url}", url);

        try
        {
            var http = _httpClientFactory.CreateClient();
            using var resp = await http.GetAsync(url, ct);

            log.HttpStatus = (int)resp.StatusCode;

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                log.IsSuccess = false;
                log.ErrorMessage = "Gọi Geo API thất bại.";
                log.ErrorDetail = body;

                _db.GeoSyncLogs.Add(log);
                await _db.SaveChangesAsync(ct);

                _logger.LogWarning("Geo sync failed. status={Status}", (int)resp.StatusCode);
                return StatusCode((int)resp.StatusCode, new { message = "Gọi Geo API thất bại.", status = (int)resp.StatusCode });
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var provinces = JsonSerializer.Deserialize<List<ProvinceDto>>(json, options) ?? new List<ProvinceDto>();

            var provinceByCode = (await _db.Provinces.AsNoTracking().ToListAsync(ct)).ToDictionary(x => x.Code);
            var districtByCode = (await _db.Districts.AsNoTracking().ToListAsync(ct)).ToDictionary(x => x.Code);
            var wardByCode = (await _db.Wards.AsNoTracking().ToListAsync(ct)).ToDictionary(x => x.Code);

            var insertedProvinces = 0;
            var updatedProvinces = 0;
            var insertedDistricts = 0;
            var updatedDistricts = 0;
            var insertedWards = 0;
            var updatedWards = 0;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            foreach (var p in provinces)
            {
                var (province, insP, updP) = await UpsertProvinceAsync(p, provinceByCode, now, ct);
                insertedProvinces += insP;
                updatedProvinces += updP;

                if (province is null) continue;

                if (depth >= 2 && p.Districts is not null)
                {
                    foreach (var d in p.Districts)
                    {
                        var (district, insD, updD) = await UpsertDistrictAsync(d, province.Id, districtByCode, now, ct);
                        insertedDistricts += insD;
                        updatedDistricts += updD;

                        if (district is null) continue;

                        if (depth >= 3 && d.Wards is not null)
                        {
                            foreach (var w in d.Wards)
                            {
                                var (_, insW, updW) = await UpsertWardAsync(w, district.Id, wardByCode, now, ct);
                                insertedWards += insW;
                                updatedWards += updW;
                            }
                        }
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            log.IsSuccess = true;
            log.ProvincesInserted = insertedProvinces;
            log.ProvincesUpdated = updatedProvinces;
            log.DistrictsInserted = insertedDistricts;
            log.DistrictsUpdated = updatedDistricts;
            log.WardsInserted = insertedWards;
            log.WardsUpdated = updatedWards;

            _db.GeoSyncLogs.Add(log);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Geo sync completed.");

            return Ok(new
            {
                ok = true,
                depth,
                provinces = new { inserted = insertedProvinces, updated = updatedProvinces, totalFetched = provinces.Count },
                districts = new { inserted = insertedDistricts, updated = updatedDistricts },
                wards = new { inserted = insertedWards, updated = updatedWards }
            });
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.HttpStatus = log.HttpStatus == 0 ? 500 : log.HttpStatus;
            log.ErrorMessage = ex.Message;
            log.ErrorDetail = ex.ToString();

            _db.GeoSyncLogs.Add(log);
            await _db.SaveChangesAsync(ct);

            _logger.LogError(ex, "Đồng bộ địa giới bị lỗi.");
            return StatusCode(500, new { message = "Đồng bộ địa giới bị lỗi.", detail = ex.Message });
        }
    }

    private async Task<(Province? entity, int inserted, int updated)> UpsertProvinceAsync(
        ProvinceDto dto,
        Dictionary<int, Province> provinceByCode,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (dto.Code <= 0 || string.IsNullOrWhiteSpace(dto.Name))
            return (null, 0, 0);

        if (!provinceByCode.TryGetValue(dto.Code, out _))
        {
            var entity = new Province
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Name = NormalizeName(dto.Name),
                Slug = dto.Codename ?? dto.Slug,
                Type = dto.DivisionType,
                IsActive = true,
                CreatedAt = now
            };

            _db.Provinces.Add(entity);
            provinceByCode[dto.Code] = entity;
            return (entity, 1, 0);
        }
        else
        {
            var entity = await _db.Provinces.FirstAsync(x => x.Code == dto.Code, ct);

            var changed = false;
            var name = NormalizeName(dto.Name);
            var slug = dto.Codename ?? dto.Slug;

            if (entity.Name != name) { entity.Name = name; changed = true; }
            if (entity.Slug != slug) { entity.Slug = slug; changed = true; }
            if (entity.Type != dto.DivisionType) { entity.Type = dto.DivisionType; changed = true; }
            if (!entity.IsActive) { entity.IsActive = true; changed = true; }

            if (changed) entity.UpdatedAt = now;

            return (entity, 0, changed ? 1 : 0);
        }
    }

    private async Task<(District? entity, int inserted, int updated)> UpsertDistrictAsync(
        DistrictDto dto,
        Guid provinceId,
        Dictionary<int, District> districtByCode,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (dto.Code <= 0 || string.IsNullOrWhiteSpace(dto.Name))
            return (null, 0, 0);

        if (!districtByCode.TryGetValue(dto.Code, out _))
        {
            var entity = new District
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Name = NormalizeName(dto.Name),
                Slug = dto.Codename ?? dto.Slug,
                Type = dto.DivisionType,
                ProvinceId = provinceId,
                IsActive = true,
                CreatedAt = now
            };

            _db.Districts.Add(entity);
            districtByCode[dto.Code] = entity;
            return (entity, 1, 0);
        }
        else
        {
            var entity = await _db.Districts.FirstAsync(x => x.Code == dto.Code, ct);

            var changed = false;
            var name = NormalizeName(dto.Name);
            var slug = dto.Codename ?? dto.Slug;

            if (entity.Name != name) { entity.Name = name; changed = true; }
            if (entity.Slug != slug) { entity.Slug = slug; changed = true; }
            if (entity.Type != dto.DivisionType) { entity.Type = dto.DivisionType; changed = true; }
            if (entity.ProvinceId != provinceId) { entity.ProvinceId = provinceId; changed = true; }
            if (!entity.IsActive) { entity.IsActive = true; changed = true; }

            if (changed) entity.UpdatedAt = now;

            return (entity, 0, changed ? 1 : 0);
        }
    }

    private async Task<(Ward? entity, int inserted, int updated)> UpsertWardAsync(
        WardDto dto,
        Guid districtId,
        Dictionary<int, Ward> wardByCode,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (dto.Code <= 0 || string.IsNullOrWhiteSpace(dto.Name))
            return (null, 0, 0);

        if (!wardByCode.TryGetValue(dto.Code, out _))
        {
            var entity = new Ward
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Name = NormalizeName(dto.Name),
                Slug = dto.Codename ?? dto.Slug,
                Type = dto.DivisionType,
                DistrictId = districtId,
                IsActive = true,
                CreatedAt = now
            };

            _db.Wards.Add(entity);
            wardByCode[dto.Code] = entity;
            return (entity, 1, 0);
        }
        else
        {
            var entity = await _db.Wards.FirstAsync(x => x.Code == dto.Code, ct);

            var changed = false;
            var name = NormalizeName(dto.Name);
            var slug = dto.Codename ?? dto.Slug;

            if (entity.Name != name) { entity.Name = name; changed = true; }
            if (entity.Slug != slug) { entity.Slug = slug; changed = true; }
            if (entity.Type != dto.DivisionType) { entity.Type = dto.DivisionType; changed = true; }
            if (entity.DistrictId != districtId) { entity.DistrictId = districtId; changed = true; }
            if (!entity.IsActive) { entity.IsActive = true; changed = true; }

            if (changed) entity.UpdatedAt = now;

            return (entity, 0, changed ? 1 : 0);
        }
    }

    private static string NormalizeName(string name)
        => string.Join(' ', name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private sealed class ProvinceDto
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("division_type")] public string? DivisionType { get; set; }
        [JsonPropertyName("codename")] public string? Codename { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("districts")] public List<DistrictDto>? Districts { get; set; }
    }

    private sealed class DistrictDto
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("division_type")] public string? DivisionType { get; set; }
        [JsonPropertyName("codename")] public string? Codename { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("wards")] public List<WardDto>? Wards { get; set; }
    }

    private sealed class WardDto
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("division_type")] public string? DivisionType { get; set; }
        [JsonPropertyName("codename")] public string? Codename { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
    }
}
