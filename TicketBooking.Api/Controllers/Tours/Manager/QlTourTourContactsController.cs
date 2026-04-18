// FILE #253: TicketBooking.Api/Controllers/Tours/QlTourTourContactsController.cs
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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/contacts")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourContactsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourContactsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourContactPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourContactType? contactType = null,
        [FromQuery] bool? primary = null,
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

        IQueryable<TourContact> query = includeDeleted
            ? _db.TourContacts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourContacts.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (contactType.HasValue)
            query = query.Where(x => x.ContactType == contactType.Value);

        if (primary.HasValue)
            query = query.Where(x => x.IsPrimary == primary.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(qq) ||
                (x.Title != null && x.Title.Contains(qq)) ||
                (x.Department != null && x.Department.Contains(qq)) ||
                (x.Phone != null && x.Phone.Contains(qq)) ||
                (x.Email != null && x.Email.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.ContactType)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourContactListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                Name = x.Name,
                Title = x.Title,
                Department = x.Department,
                Phone = x.Phone,
                Email = x.Email,
                ContactType = x.ContactType,
                IsPrimary = x.IsPrimary,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourContactPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QlTourContactDetailDto>> GetById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourContact> query = includeDeleted
            ? _db.TourContacts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourContacts.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour contact not found in current tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<QlTourCreateContactResponse>> Create(
        Guid tourId,
        [FromBody] QlTourCreateContactRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        ValidatePayload(req.Name, req.Title, req.Department, req.Phone, req.Email, req.Notes);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (req.IsPrimary == true)
        {
            var currentPrimary = await _db.TourContacts.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.IsPrimary)
                .ToListAsync(ct);

            foreach (var old in currentPrimary)
            {
                old.IsPrimary = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        var entity = new TourContact
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Name = req.Name.Trim(),
            Title = NullIfWhiteSpace(req.Title),
            Department = NullIfWhiteSpace(req.Department),
            Phone = NullIfWhiteSpace(req.Phone),
            Email = NullIfWhiteSpace(req.Email),
            ContactType = req.ContactType,
            IsPrimary = req.IsPrimary ?? false,
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

        _db.TourContacts.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = entity.Id },
            new QlTourCreateContactResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid id,
        [FromBody] QlTourUpdateContactRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour contact not found in current tenant." });

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

        ValidatePayload(
            req.Name ?? entity.Name,
            req.Title,
            req.Department,
            req.Phone,
            req.Email,
            req.Notes);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();
        var nextIsPrimary = req.IsPrimary ?? entity.IsPrimary;

        if (nextIsPrimary)
        {
            var currentPrimary = await _db.TourContacts.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsPrimary)
                .ToListAsync(ct);

            foreach (var old in currentPrimary)
            {
                old.IsPrimary = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.Title is not null) entity.Title = NullIfWhiteSpace(req.Title);
        if (req.Department is not null) entity.Department = NullIfWhiteSpace(req.Department);
        if (req.Phone is not null) entity.Phone = NullIfWhiteSpace(req.Phone);
        if (req.Email is not null) entity.Email = NullIfWhiteSpace(req.Email);
        if (req.ContactType.HasValue) entity.ContactType = req.ContactType.Value;
        if (req.IsPrimary.HasValue) entity.IsPrimary = req.IsPrimary.Value;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour contact was changed by another user. Please reload and try again." });
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

    [HttpPost("{id:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePrimary(tourId, id, true, ct);

    [HttpPost("{id:guid}/unset-primary")]
    public async Task<IActionResult> UnsetPrimary(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePrimary(tourId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour contact not found in current tenant." });

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

        var entity = await _db.TourContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour contact not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> TogglePrimary(Guid tourId, Guid id, bool isPrimary, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour contact not found in current tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (isPrimary)
        {
            var currentPrimary = await _db.TourContacts.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsPrimary)
                .ToListAsync(ct);

            foreach (var old in currentPrimary)
            {
                old.IsPrimary = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }

            entity.IsActive = true;
        }

        entity.IsPrimary = isPrimary;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

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

    private static void ValidatePayload(
        string name,
        string? title,
        string? department,
        string? phone,
        string? email,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (name.Trim().Length > 200)
            throw new ArgumentException("Name max length is 200.");

        if (title is not null && title.Length > 200)
            throw new ArgumentException("Title max length is 200.");

        if (department is not null && department.Length > 200)
            throw new ArgumentException("Department max length is 200.");

        if (phone is not null && phone.Length > 50)
            throw new ArgumentException("Phone max length is 50.");

        if (email is not null && email.Length > 200)
            throw new ArgumentException("Email max length is 200.");

        if (notes is not null && notes.Length > 2000)
            throw new ArgumentException("Notes max length is 2000.");
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

    private static QlTourContactDetailDto MapDetail(TourContact x)
    {
        return new QlTourContactDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Name = x.Name,
            Title = x.Title,
            Department = x.Department,
            Phone = x.Phone,
            Email = x.Email,
            ContactType = x.ContactType,
            IsPrimary = x.IsPrimary,
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

public sealed class QlTourContactPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourContactListItemDto> Items { get; set; } = new();
}

public sealed class QlTourContactListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Name { get; set; } = "";
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TourContactType ContactType { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourContactDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Name { get; set; } = "";
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TourContactType ContactType { get; set; }
    public bool IsPrimary { get; set; }
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

public sealed class QlTourCreateContactRequest
{
    public string Name { get; set; } = "";
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TourContactType ContactType { get; set; } = TourContactType.General;
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdateContactRequest
{
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TourContactType? ContactType { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateContactResponse
{
    public Guid Id { get; set; }
}
