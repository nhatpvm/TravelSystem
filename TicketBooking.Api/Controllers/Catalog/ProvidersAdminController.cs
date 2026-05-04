// FILE #055: TicketBooking.Api/Controllers/ProvidersAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Catalog;
using TicketBooking.Infrastructure.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/catalog/providers")]
[Route("api/v{version:apiVersion}/tenant/catalog/providers")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX},{RoleNames.QLVT},{RoleNames.QLVMM},{RoleNames.QLKS},{RoleNames.QLTour}")]
public sealed class ProvidersAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProvidersAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] ProviderType? type = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        var allowedTypes = GetAllowedProviderTypes();
        if (!User.IsInRole(RoleNames.Admin) && !_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác quản lý đối tác." });

        if (!User.IsInRole(RoleNames.Admin) && type.HasValue && !allowedTypes.Contains(type.Value))
            return BadRequest(new { message = "Loại đối tác không thuộc phạm vi tenant này." });

        IQueryable<Provider> query = _db.Providers;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (!User.IsInRole(RoleNames.Admin))
            query = query.Where(x => allowedTypes.Contains(x.Type));
        else if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword) || x.Slug.Contains(keyword));
        }

        // If admin did not switch tenant, show all (tenant filter not applied when HasTenant=false)
        // If admin switched tenant, tenant filter applies automatically.

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Type,
                x.Code,
                x.Name,
                x.Slug,
                x.LocationId,
                LocationName = _db.Locations.IgnoreQueryFilters()
                    .Where(l => l.Id == x.LocationId)
                    .Select(l => l.Name)
                    .FirstOrDefault(),
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
        IQueryable<Provider> query = _db.Providers;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);
        if (!User.IsInRole(RoleNames.Admin))
            query = query.Where(x => GetAllowedProviderTypes().Contains(x.Type));

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "Không tìm thấy đối tác." });

        return Ok(item);
    }

    public sealed class UpsertProviderRequest
    {
        public ProviderType Type { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";

        public string? LegalName { get; set; }

        public string? LogoUrl { get; set; }
        public string? CoverUrl { get; set; }

        public string? SupportPhone { get; set; }
        public string? SupportEmail { get; set; }
        public string? WebsiteUrl { get; set; }

        public string? AddressLine { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? ProvinceId { get; set; }
        public Guid? DistrictId { get; set; }
        public Guid? WardId { get; set; }

        public decimal? RatingAverage { get; set; }
        public int RatingCount { get; set; }

        public string? Description { get; set; }
        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertProviderRequest req, CancellationToken ct = default)
    {
        // Admin write requires switched tenant => enforced by interceptor/middleware
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        if (!IsProviderTypeAllowed(req.Type))
            return BadRequest(new { message = "Loại đối tác không thuộc phạm vi tenant này." });
        await ValidateProviderReferencesAsync(req, ct);

        var code = req.Code.Trim();
        var slug = req.Slug.Trim();

        // Uniqueness per tenant
        var existsCode = await _db.Providers.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == _tenantContext.TenantId && x.Code == code, ct);
        if (existsCode) return Conflict(new { message = "Mã đối tác đã tồn tại trong tenant này." });

        var existsSlug = await _db.Providers.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == _tenantContext.TenantId && x.Slug == slug, ct);
        if (existsSlug) return Conflict(new { message = "Slug đối tác đã tồn tại trong tenant này." });

        var entity = new Provider
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            Type = req.Type,
            Code = code,
            Name = req.Name.Trim(),
            Slug = slug,
            LegalName = req.LegalName?.Trim(),
            LogoUrl = req.LogoUrl?.Trim(),
            CoverUrl = req.CoverUrl?.Trim(),
            SupportPhone = req.SupportPhone?.Trim(),
            SupportEmail = req.SupportEmail?.Trim(),
            WebsiteUrl = req.WebsiteUrl?.Trim(),
            AddressLine = req.AddressLine?.Trim(),
            LocationId = req.LocationId,
            ProvinceId = req.ProvinceId,
            DistrictId = req.DistrictId,
            WardId = req.WardId,
            RatingAverage = req.RatingAverage,
            RatingCount = req.RatingCount,
            Description = req.Description,
            MetadataJson = req.MetadataJson,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.Providers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertProviderRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        if (!IsProviderTypeAllowed(req.Type))
            return BadRequest(new { message = "Loại đối tác không thuộc phạm vi tenant này." });
        await ValidateProviderReferencesAsync(req, ct);

        var code = req.Code.Trim();
        var slug = req.Slug.Trim();

        var entity = await _db.Providers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy đối tác trong tenant này." });

        // uniqueness checks (excluding self)
        var existsCode = await _db.Providers.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == _tenantContext.TenantId && x.Code == code && x.Id != id, ct);
        if (existsCode) return Conflict(new { message = "Mã đối tác đã tồn tại trong tenant này." });

        var existsSlug = await _db.Providers.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == _tenantContext.TenantId && x.Slug == slug && x.Id != id, ct);
        if (existsSlug) return Conflict(new { message = "Slug đối tác đã tồn tại trong tenant này." });

        entity.Type = req.Type;
        entity.Code = code;
        entity.Name = req.Name.Trim();
        entity.Slug = slug;
        entity.LegalName = req.LegalName?.Trim();
        entity.LogoUrl = req.LogoUrl?.Trim();
        entity.CoverUrl = req.CoverUrl?.Trim();
        entity.SupportPhone = req.SupportPhone?.Trim();
        entity.SupportEmail = req.SupportEmail?.Trim();
        entity.WebsiteUrl = req.WebsiteUrl?.Trim();
        entity.AddressLine = req.AddressLine?.Trim();
        entity.LocationId = req.LocationId;
        entity.ProvinceId = req.ProvinceId;
        entity.DistrictId = req.DistrictId;
        entity.WardId = req.WardId;
        entity.RatingAverage = req.RatingAverage;
        entity.RatingCount = req.RatingCount;
        entity.Description = req.Description;
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

        var entity = await _db.Providers
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (entity is null) return NotFound(new { message = "Không tìm thấy đối tác trong tenant này." });
        if (!IsProviderTypeAllowed(entity.Type))
            return NotFound(new { message = "Không tìm thấy đối tác trong phạm vi tenant này." });

        _db.Providers.Remove(entity); // interceptor converts to soft delete
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var entity = await _db.Providers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy đối tác." });
        if (!IsProviderTypeAllowed(entity.Type))
            return NotFound(new { message = "Không tìm thấy đối tác trong phạm vi tenant này." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private async Task ValidateProviderReferencesAsync(UpsertProviderRequest req, CancellationToken ct)
    {
        if (req.LocationId.HasValue && req.LocationId.Value != Guid.Empty)
        {
            var location = await _db.Locations.IgnoreQueryFilters()
                .Where(x => x.Id == req.LocationId.Value && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
                .Select(x => new { x.ProvinceId, x.DistrictId, x.WardId })
                .FirstOrDefaultAsync(ct);

            if (location is null)
                throw new InvalidOperationException("LocationId không hợp lệ trong tenant này.");

            if (req.ProvinceId.HasValue && location.ProvinceId.HasValue && req.ProvinceId.Value != location.ProvinceId.Value)
                throw new InvalidOperationException("LocationId không thuộc ProvinceId đã chọn.");

            if (req.DistrictId.HasValue && location.DistrictId.HasValue && req.DistrictId.Value != location.DistrictId.Value)
                throw new InvalidOperationException("LocationId không thuộc DistrictId đã chọn.");

            if (req.WardId.HasValue && location.WardId.HasValue && req.WardId.Value != location.WardId.Value)
                throw new InvalidOperationException("LocationId không thuộc WardId đã chọn.");
        }

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
            var wardDistrictId = await _db.Wards
                .Where(x => x.Id == req.WardId.Value)
                .Select(x => (Guid?)x.DistrictId)
                .FirstOrDefaultAsync(ct);

            if (!wardDistrictId.HasValue)
                throw new InvalidOperationException("WardId không hợp lệ.");

            if (req.DistrictId.HasValue && req.DistrictId.Value != wardDistrictId.Value)
                throw new InvalidOperationException("WardId không thuộc DistrictId đã chọn.");
        }
    }

    private static void Validate(UpsertProviderRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Code)) throw new InvalidOperationException("Mã là bắt buộc.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Tên là bắt buộc.");
        if (string.IsNullOrWhiteSpace(req.Slug)) throw new InvalidOperationException("Slug là bắt buộc.");

        if (req.Code.Length > 50) throw new InvalidOperationException("Mã tối đa 50 ký tự.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Tên tối đa 200 ký tự.");
        if (req.Slug.Length > 200) throw new InvalidOperationException("Slug tối đa 200 ký tự.");
        if (req.RatingAverage.HasValue && (req.RatingAverage.Value < 0 || req.RatingAverage.Value > 5))
            throw new InvalidOperationException("Điểm đánh giá phải nằm trong khoảng 0 đến 5.");
        if (req.RatingCount < 0) throw new InvalidOperationException("Số lượt đánh giá không được âm.");
    }

    private bool IsProviderTypeAllowed(ProviderType providerType)
        => User.IsInRole(RoleNames.Admin) || GetAllowedProviderTypes().Contains(providerType);

    private ProviderType[] GetAllowedProviderTypes()
    {
        if (User.IsInRole(RoleNames.Admin))
            return Enum.GetValues<ProviderType>();

        var values = new List<ProviderType>();
        if (User.IsInRole(RoleNames.QLNX))
            values.Add(ProviderType.Bus);
        if (User.IsInRole(RoleNames.QLVT))
            values.Add(ProviderType.Train);
        if (User.IsInRole(RoleNames.QLVMM))
            values.Add(ProviderType.Flight);
        if (User.IsInRole(RoleNames.QLKS))
            values.Add(ProviderType.Hotel);
        if (User.IsInRole(RoleNames.QLTour))
            values.Add(ProviderType.Tour);

        return values.Distinct().ToArray();
    }
}
