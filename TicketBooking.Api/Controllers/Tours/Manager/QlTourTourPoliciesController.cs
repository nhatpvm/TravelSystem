// FILE #250: TicketBooking.Api/Controllers/Tours/QlTourTourPoliciesController.cs
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/policies")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourPoliciesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourPoliciesController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourPolicyPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourPolicyType? type = null,
        [FromQuery] bool? highlighted = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourPolicy> query = includeDeleted
            ? _db.TourPolicies.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourPolicies.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (highlighted.HasValue)
            query = query.Where(x => x.IsHighlighted == highlighted.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(qq)) ||
                (x.DescriptionMarkdown != null && x.DescriptionMarkdown.Contains(qq)) ||
                (x.DescriptionHtml != null && x.DescriptionHtml.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsHighlighted)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourPolicyListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                ShortDescription = x.ShortDescription,
                IsHighlighted = x.IsHighlighted,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourPolicyPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QlTourPolicyDetailDto>> GetById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourPolicy> query = includeDeleted
            ? _db.TourPolicies.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourPolicies.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour policy not found in current tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<QlTourCreatePolicyResponse>> Create(
        Guid tourId,
        [FromBody] QlTourCreatePolicyRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await ValidateCreateAsync(tenantId, tourId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Type = req.Type,
            ShortDescription = NullIfWhiteSpace(req.ShortDescription),
            DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown),
            DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml),
            PolicyJson = NullIfWhiteSpace(req.PolicyJson),
            IsHighlighted = req.IsHighlighted ?? false,
            SortOrder = req.SortOrder ?? 0,
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourPolicies.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = entity.Id },
            new QlTourCreatePolicyResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid id,
        [FromBody] QlTourUpdatePolicyRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour policy not found in current tenant." });

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

        await ValidateUpdateAsync(tenantId, tourId, id, req, ct);

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.Type.HasValue) entity.Type = req.Type.Value;
        if (req.ShortDescription is not null) entity.ShortDescription = NullIfWhiteSpace(req.ShortDescription);
        if (req.DescriptionMarkdown is not null) entity.DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown);
        if (req.DescriptionHtml is not null) entity.DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml);
        if (req.PolicyJson is not null) entity.PolicyJson = NullIfWhiteSpace(req.PolicyJson);
        if (req.IsHighlighted.HasValue) entity.IsHighlighted = req.IsHighlighted.Value;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour policy was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, false, ct);

    [HttpPost("{id:guid}/highlight")]
    public async Task<IActionResult> Highlight(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleHighlighted(tourId, id, true, ct);

    [HttpPost("{id:guid}/unhighlight")]
    public async Task<IActionResult> Unhighlight(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleHighlighted(tourId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour policy not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid tourId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour policy not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleHighlighted(Guid tourId, Guid id, bool isHighlighted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour policy not found in current tenant." });

        entity.IsHighlighted = isHighlighted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task EnsureTourExistsAsync(Guid tenantId, Guid tourId, CancellationToken ct)
    {
        var exists = await _db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == tourId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour not found in current tenant.");
    }

    private async Task ValidateCreateAsync(
        Guid tenantId,
        Guid tourId,
        QlTourCreatePolicyRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        if (req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.ShortDescription is not null && req.ShortDescription.Length > 2000)
            throw new ArgumentException("ShortDescription max length is 2000.");

        if (req.Notes is not null && req.Notes.Length > 2000)
            throw new ArgumentException("Notes max length is 2000.");

        var code = req.Code.Trim();

        var duplicatedCode = await _db.TourPolicies.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == code, ct);

        if (duplicatedCode)
            throw new ArgumentException("Code already exists in current tour.");
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid tourId,
        Guid currentId,
        QlTourUpdatePolicyRequest req,
        CancellationToken ct)
    {
        if (req.Code is not null && string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code cannot be empty.");

        if (req.Name is not null && string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name cannot be empty.");

        if (req.Code is not null && req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.ShortDescription is not null && req.ShortDescription.Length > 2000)
            throw new ArgumentException("ShortDescription max length is 2000.");

        if (req.Notes is not null && req.Notes.Length > 2000)
            throw new ArgumentException("Notes max length is 2000.");

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var code = req.Code.Trim();

            var duplicatedCode = await _db.TourPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == code && x.Id != currentId, ct);

            if (duplicatedCode)
                throw new ArgumentException("Code already exists in current tour.");
        }
    }

    private Guid RequireTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("QLTour operations require tenant context.");

        return _tenant.TenantId.Value;
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static QlTourPolicyDetailDto MapDetail(TourPolicy x)
    {
        return new QlTourPolicyDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Code = x.Code,
            Name = x.Name,
            Type = x.Type,
            ShortDescription = x.ShortDescription,
            DescriptionMarkdown = x.DescriptionMarkdown,
            DescriptionHtml = x.DescriptionHtml,
            PolicyJson = x.PolicyJson,
            IsHighlighted = x.IsHighlighted,
            SortOrder = x.SortOrder,
            Notes = x.Notes,
            MetadataJson = x.MetadataJson,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }
}

public sealed class QlTourPolicyPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourPolicyListItemDto> Items { get; set; } = new();
}

public sealed class QlTourPolicyListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPolicyType Type { get; set; }
    public string? ShortDescription { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourPolicyDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPolicyType Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? PolicyJson { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreatePolicyRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPolicyType Type { get; set; } = TourPolicyType.General;
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? PolicyJson { get; set; }
    public bool? IsHighlighted { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdatePolicyRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TourPolicyType? Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? PolicyJson { get; set; }
    public bool? IsHighlighted { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreatePolicyResponse
{
    public Guid Id { get; set; }
}
