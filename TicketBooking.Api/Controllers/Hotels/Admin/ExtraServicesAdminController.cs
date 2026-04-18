// FILE #212: TicketBooking.Api/Controllers/Hotels/ExtraServicesAdminController.cs
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/extra-services")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class ExtraServicesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public ExtraServicesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminExtraServicePagedResponse<AdminExtraServiceListItemDto>>> List(
        [FromQuery] Guid? hotelId = null,
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<ExtraService> query = includeDeleted
            ? _db.ExtraServices.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.ExtraServices.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(x => x.HotelId == hotelId.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(q) ||
                x.Name.Contains(q) ||
                (x.Description != null && x.Description.Contains(q)));
        }

        var total = await query.CountAsync(ct);

        var items = await query.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminExtraServiceListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                HotelId = x.HotelId,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminExtraServicePagedResponse<AdminExtraServiceListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminExtraServiceDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<ExtraService> query = includeDeleted
            ? _db.ExtraServices.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.ExtraServices.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Extra service not found in current switched tenant." });

        var prices = await _db.ExtraServicePrices.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.ExtraServiceId == id && !x.IsDeleted)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .Select(x => new AdminExtraServicePriceDto
            {
                Id = x.Id,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CurrencyCode = x.CurrencyCode,
                Price = x.Price,
                IsDeleted = x.IsDeleted,
                UpdatedAt = x.UpdatedAt,
                RowVersionBase64 = x.RowVersion != null && x.RowVersion.Length > 0
                    ? Convert.ToBase64String(x.RowVersion)
                    : null
            })
            .ToListAsync(ct);

        return Ok(new AdminExtraServiceDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            HotelId = entity.HotelId,
            Code = entity.Code,
            Name = entity.Name,
            Type = entity.Type,
            Description = entity.Description,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } ? Convert.ToBase64String(entity.RowVersion) : null,
            Prices = prices
        });
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateExtraServiceResponse>> Create(
        [FromBody] AdminCreateExtraServiceRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return BadRequest(new { message = "Hotel not found in current switched tenant." });

        var code = req.Code!.Trim();

        var codeExists = await _db.ExtraServices.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = "Extra service code already exists in this hotel." });

        var entity = new ExtraService
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            Code = code,
            Name = req.Name!.Trim(),
            Type = req.Type ?? ExtraServiceType.Other,
            Description = NullIfWhiteSpace(req.Description),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.ExtraServices.Add(entity);
        await _db.SaveChangesAsync(ct);

        if (req.Prices is { Count: > 0 })
            await ReplacePricesAsync(tenantId, entity.Id, req.Prices, now, userId, ct);

        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateExtraServiceResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateExtraServiceRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.ExtraServices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Extra service not found in current switched tenant." });

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(req.RowVersionBase64);
                _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = bytes;
            }
            catch
            {
                return BadRequest(new { message = "RowVersionBase64 is invalid." });
            }
        }

        if (req.HotelId.HasValue && req.HotelId.Value != entity.HotelId)
        {
            var hotelExists = await _db.Hotels.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId.Value && !x.IsDeleted, ct);

            if (!hotelExists)
                return BadRequest(new { message = "Target hotel not found in current switched tenant." });

            var movedCode = req.Code?.Trim() ?? entity.Code;
            var codeExists = await _db.ExtraServices.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId.Value && x.Code == movedCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Extra service code already exists in target hotel." });

            entity.HotelId = req.HotelId.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var newCode = req.Code.Trim();
            var codeExists = await _db.ExtraServices.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == entity.HotelId && x.Code == newCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Extra service code already exists in this hotel." });

            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.Type.HasValue) entity.Type = req.Type.Value;
        if (req.Description is not null) entity.Description = NullIfWhiteSpace(req.Description);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        if (req.Prices is not null)
            await ReplacePricesAsync(tenantId, entity.Id, req.Prices, now, userId, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Data was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        return await ToggleDeleted(id, true, ct);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        return await ToggleDeleted(id, false, ct);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
    {
        return await ToggleActive(id, true, ct);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        return await ToggleActive(id, false, ct);
    }

    [HttpGet("{id:guid}/prices")]
    public async Task<ActionResult<List<AdminExtraServicePriceDto>>> GetPrices(
        Guid id,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        var serviceExists = await _db.ExtraServices.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (!serviceExists)
            return NotFound(new { message = "Extra service not found in current switched tenant." });

        IQueryable<ExtraServicePrice> query = includeDeleted
            ? _db.ExtraServicePrices.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.ExtraServicePrices.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        query = query.Where(x => x.ExtraServiceId == id);

        if (fromDate.HasValue)
            query = query.Where(x => x.EndDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.StartDate <= toDate.Value);

        var items = await query.AsNoTracking()
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .Select(x => new AdminExtraServicePriceDto
            {
                Id = x.Id,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CurrencyCode = x.CurrencyCode,
                Price = x.Price,
                IsDeleted = x.IsDeleted,
                UpdatedAt = x.UpdatedAt,
                RowVersionBase64 = x.RowVersion != null && x.RowVersion.Length > 0
                    ? Convert.ToBase64String(x.RowVersion)
                    : null
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPut("{id:guid}/prices")]
    public async Task<IActionResult> ReplacePrices(
        Guid id,
        [FromBody] AdminReplaceExtraServicePricesRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateReplacePrices(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var serviceExists = await _db.ExtraServices.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == id && !x.IsDeleted, ct);

        if (!serviceExists)
            return NotFound(new { message = "Extra service not found in current switched tenant." });

        await ReplacePricesAsync(tenantId, id, req.Prices, now, userId, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.ExtraServices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Extra service not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.ExtraServices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Extra service not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task ReplacePricesAsync(
        Guid tenantId,
        Guid extraServiceId,
        List<AdminExtraServicePriceUpsertDto> items,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        var existingRows = await _db.ExtraServicePrices.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.ExtraServiceId == extraServiceId)
            .ToListAsync(ct);

        _db.ExtraServicePrices.RemoveRange(existingRows);

        if (items.Count == 0) return;

        var rows = items
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .Select(x => new ExtraServicePrice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExtraServiceId = extraServiceId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CurrencyCode = string.IsNullOrWhiteSpace(x.CurrencyCode) ? "VND" : x.CurrencyCode!.Trim(),
                Price = x.Price,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            })
            .ToList();

        _db.ExtraServicePrices.AddRange(rows);
    }

    private void RequireAdminWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin write requires switched tenant context (X-TenantId).");
    }

    private static void ValidateCreate(AdminCreateExtraServiceRequest req)
    {
        if (req.HotelId == Guid.Empty)
            throw new ArgumentException("HotelId is required.");

        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        if (req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");
    }

    private static void ValidateUpdate(AdminUpdateExtraServiceRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");
    }

    private static void ValidateReplacePrices(AdminReplaceExtraServicePricesRequest req)
    {
        foreach (var item in req.Prices)
        {
            if (item.EndDate < item.StartDate)
                throw new ArgumentException("EndDate must be greater than or equal to StartDate.");

            if (item.Price < 0)
                throw new ArgumentException("Price must be >= 0.");

            if (!string.IsNullOrWhiteSpace(item.CurrencyCode) && item.CurrencyCode!.Trim().Length > 10)
                throw new ArgumentException("CurrencyCode max length is 10.");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AdminExtraServicePagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class AdminExtraServiceListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public ExtraServiceType Type { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminExtraServiceDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public ExtraServiceType Type { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
    public List<AdminExtraServicePriceDto> Prices { get; set; } = new();
}

public sealed class AdminExtraServicePriceDto
{
    public Guid Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal Price { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateExtraServiceRequest
{
    public Guid HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public ExtraServiceType? Type { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public List<AdminExtraServicePriceUpsertDto>? Prices { get; set; }
}

public sealed class AdminUpdateExtraServiceRequest
{
    public Guid? HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public ExtraServiceType? Type { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public List<AdminExtraServicePriceUpsertDto>? Prices { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminExtraServicePriceUpsertDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal Price { get; set; }
}

public sealed class AdminReplaceExtraServicePricesRequest
{
    public List<AdminExtraServicePriceUpsertDto> Prices { get; set; } = new();
}

public sealed class AdminCreateExtraServiceResponse
{
    public Guid Id { get; set; }
}

