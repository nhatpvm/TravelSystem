// FILE #217: TicketBooking.Api/Controllers/Hotels/HotelContactsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/hotel-contacts")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class HotelContactsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public HotelContactsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminHotelContactPagedResponse>> List(
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

        IQueryable<HotelContact> query = includeDeleted
            ? _db.HotelContacts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.HotelContacts.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(x => x.HotelId == hotelId.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.ContactName.Contains(qq) ||
                (x.RoleTitle != null && x.RoleTitle.Contains(qq)) ||
                (x.Phone != null && x.Phone.Contains(qq)) ||
                (x.Email != null && x.Email.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.ContactName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminHotelContactListItemDto
            {
                Id = x.Id,
                HotelId = x.HotelId,
                Name = x.ContactName,
                Title = x.RoleTitle,
                Department = null,
                Phone = x.Phone,
                Email = x.Email,
                ContactType = null,
                IsPrimary = x.IsPrimary,
                SortOrder = null,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminHotelContactPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminHotelContactDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<HotelContact> query = includeDeleted
            ? _db.HotelContacts.IgnoreQueryFilters()
            : _db.HotelContacts;

        query = query.Where(x => x.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Hotel contact not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateHotelContactResponse>> Create(
        [FromBody] AdminCreateHotelContactRequest req,
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

        var entity = new HotelContact
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            ContactName = NullIfWhiteSpace(req.Name) ?? string.Empty,
            RoleTitle = NullIfWhiteSpace(req.Title),
            Phone = NullIfWhiteSpace(req.Phone),
            Email = NullIfWhiteSpace(req.Email),
            Notes = NullIfWhiteSpace(req.Notes),
            IsPrimary = req.IsPrimary ?? false,
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.HotelContacts.Add(entity);
        await _db.SaveChangesAsync(ct);

        await EnsurePrimaryContactAsync(tenantId, entity.HotelId, null, userId, ct);
        await tx.CommitAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateHotelContactResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateHotelContactRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();
        Guid previousHotelId;

        var entity = await _db.HotelContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel contact not found in current switched tenant." });

        previousHotelId = entity.HotelId;

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

            entity.HotelId = req.HotelId.Value;
        }

        if (req.Name is not null)
            entity.ContactName = NullIfWhiteSpace(req.Name) ?? string.Empty;

        if (req.Title is not null)
            entity.RoleTitle = NullIfWhiteSpace(req.Title);

        if (req.Phone is not null)
            entity.Phone = NullIfWhiteSpace(req.Phone);

        if (req.Email is not null)
            entity.Email = NullIfWhiteSpace(req.Email);

        if (req.IsPrimary.HasValue)
            entity.IsPrimary = req.IsPrimary.Value;

        if (req.Notes is not null)
            entity.Notes = NullIfWhiteSpace(req.Notes);

        if (req.IsActive.HasValue)
            entity.IsActive = req.IsActive.Value;

        if (req.IsDeleted.HasValue)
            entity.IsDeleted = req.IsDeleted.Value;

        if (req.IsPrimary == true && (!entity.IsActive || entity.IsDeleted))
            return BadRequest(new { message = "Inactive or deleted hotel contact cannot be set as primary." });

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _db.SaveChangesAsync(ct);
            if (previousHotelId != entity.HotelId)
                await EnsurePrimaryContactAsync(tenantId, previousHotelId, null, userId, ct);

            await EnsurePrimaryContactAsync(
                tenantId,
                entity.HotelId,
                entity.IsPrimary && entity.IsActive && !entity.IsDeleted ? entity.Id : null,
                userId,
                ct);
            await tx.CommitAsync(ct);

            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Data was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid id, CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        var entity = await _db.HotelContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel contact not found in current switched tenant." });

        if (!entity.IsActive)
            return BadRequest(new { message = "Inactive hotel contact cannot be set as primary." });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await EnsurePrimaryContactAsync(tenantId, entity.HotelId, entity.Id, GetCurrentUserId(), ct);
        await tx.CommitAsync(ct);
        return Ok(new { ok = true });
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

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.HotelContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel contact not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.SaveChangesAsync(ct);
        await EnsurePrimaryContactAsync(tenantId, entity.HotelId, null, userId, ct);
        await tx.CommitAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.HotelContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel contact not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.SaveChangesAsync(ct);
        await EnsurePrimaryContactAsync(tenantId, entity.HotelId, null, userId, ct);
        await tx.CommitAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task SetPrimaryInternalAsync(HotelContact entity, CancellationToken ct)
    {
        await EnsurePrimaryContactAsync(entity.TenantId, entity.HotelId, entity.Id, GetCurrentUserId(), ct);
    }

    private async Task EnsurePrimaryContactAsync(
        Guid tenantId,
        Guid hotelId,
        Guid? preferredContactId,
        Guid? userId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var contacts = await _db.HotelContacts.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.HotelId == hotelId)
            .OrderBy(x => x.ContactName)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var candidates = contacts
            .Where(x => !x.IsDeleted && x.IsActive)
            .ToList();

        HotelContact? selected = null;
        if (preferredContactId.HasValue)
            selected = candidates.FirstOrDefault(x => x.Id == preferredContactId.Value);

        selected ??= candidates.FirstOrDefault(x => x.IsPrimary);
        selected ??= candidates.FirstOrDefault();

        var hasChanges = false;

        foreach (var contact in contacts)
        {
            var shouldBePrimary = selected is not null && contact.Id == selected.Id;
            if (contact.IsPrimary == shouldBePrimary)
                continue;

            contact.IsPrimary = shouldBePrimary;
            contact.UpdatedAt = now;
            contact.UpdatedByUserId = userId;
            hasChanges = true;
        }

        if (hasChanges)
            await _db.SaveChangesAsync(ct);
    }

    private void RequireAdminWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin write requires switched tenant context (X-TenantId).");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static void ValidateCreate(AdminCreateHotelContactRequest req)
    {
        if (req.HotelId == Guid.Empty)
            throw new ArgumentException("HotelId is required.");

        if (string.IsNullOrWhiteSpace(req.Name) &&
            string.IsNullOrWhiteSpace(req.Phone) &&
            string.IsNullOrWhiteSpace(req.Email))
            throw new ArgumentException("At least one of Name, Phone, Email is required.");

        if (req.Name is not null && req.Name.Length > 200)
            throw new ArgumentException("Name max length is 200.");

        if (req.Title is not null && req.Title.Length > 200)
            throw new ArgumentException("Title max length is 200.");

        if (req.Department is not null && req.Department.Length > 200)
            throw new ArgumentException("Department max length is 200.");

        if (req.Phone is not null && req.Phone.Length > 50)
            throw new ArgumentException("Phone max length is 50.");

        if (req.Email is not null && req.Email.Length > 200)
            throw new ArgumentException("Email max length is 200.");

        if (req.ContactType is not null && req.ContactType.Length > 50)
            throw new ArgumentException("ContactType max length is 50.");

    }

    private static void ValidateUpdate(AdminUpdateHotelContactRequest req)
    {
        if (req.Name is not null && req.Name.Length > 200)
            throw new ArgumentException("Name max length is 200.");

        if (req.Title is not null && req.Title.Length > 200)
            throw new ArgumentException("Title max length is 200.");

        if (req.Department is not null && req.Department.Length > 200)
            throw new ArgumentException("Department max length is 200.");

        if (req.Phone is not null && req.Phone.Length > 50)
            throw new ArgumentException("Phone max length is 50.");

        if (req.Email is not null && req.Email.Length > 200)
            throw new ArgumentException("Email max length is 200.");

        if (req.ContactType is not null && req.ContactType.Length > 50)
            throw new ArgumentException("ContactType max length is 50.");

    }

    private static AdminHotelContactListItemDto MapListItem(HotelContact x)
    {
        return new AdminHotelContactListItemDto
        {
            Id = x.Id,
            HotelId = x.HotelId,
            Name = x.ContactName,
            Title = x.RoleTitle,
            Department = null,
            Phone = x.Phone,
            Email = x.Email,
            ContactType = null,
            IsPrimary = x.IsPrimary,
            SortOrder = null,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static AdminHotelContactDetailDto MapDetail(HotelContact x)
    {
        return new AdminHotelContactDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            HotelId = x.HotelId,
            Name = x.ContactName,
            Title = x.RoleTitle,
            Department = null,
            Phone = x.Phone,
            Email = x.Email,
            ContactType = null,
            IsPrimary = x.IsPrimary,
            SortOrder = null,
            Notes = x.Notes,
            MetadataJson = null,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AdminHotelContactPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminHotelContactListItemDto> Items { get; set; } = new();
}

public sealed class AdminHotelContactListItemDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactType { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminHotelContactDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactType { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
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

public sealed class AdminCreateHotelContactRequest
{
    public Guid HotelId { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactType { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdateHotelContactRequest
{
    public Guid? HotelId { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactType { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateHotelContactResponse
{
    public Guid Id { get; set; }
}
