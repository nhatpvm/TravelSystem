// FILE #249: TicketBooking.Api/Controllers/Tours/QlTourTourCapacityController.cs
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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/schedules/{scheduleId:guid}/capacity")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourCapacityController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourCapacityController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourCapacityDetailDto>> Get(
        Guid tourId,
        Guid scheduleId,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        IQueryable<TourScheduleCapacity> query = includeDeleted
            ? _db.TourScheduleCapacities.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId)
            : _db.TourScheduleCapacities
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule capacity not found in current tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<QlTourCreateCapacityResponse>> Create(
        Guid tourId,
        Guid scheduleId,
        [FromBody] QlTourCreateCapacityRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);
        ValidateBusinessRules(
            req.TotalSlots,
            req.SoldSlots ?? 0,
            req.HeldSlots  ?? 0,
            req.BlockedSlots ?? 0,
            req.MinGuestsToOperate,
            req.MaxGuestsPerBooking,
            req.WarningThreshold);

        var existed = await _db.TourScheduleCapacities.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId, ct);

        if (existed)
            return Conflict(new { message = "Capacity already exists for current schedule." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourScheduleCapacity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourScheduleId = scheduleId,
            TotalSlots = req.TotalSlots,
            SoldSlots = req.SoldSlots ?? 0,
            HeldSlots = req.HeldSlots ?? 0,
            BlockedSlots = req.BlockedSlots ?? 0,
            MinGuestsToOperate = req.MinGuestsToOperate,
            MaxGuestsPerBooking = req.MaxGuestsPerBooking,
            WarningThreshold = req.WarningThreshold,
            Status = req.Status,
            AllowWaitlist = req.AllowWaitlist ?? true,
            AutoCloseWhenFull = req.AutoCloseWhenFull ?? true,
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourScheduleCapacities.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(Get),
            new { version = "1.0", tourId, scheduleId },
            new QlTourCreateCapacityResponse { Id = entity.Id });
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid scheduleId,
        [FromBody] QlTourUpdateCapacityRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourScheduleCapacities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule capacity not found in current tenant." });

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

        var nextTotalSlots = req.TotalSlots ?? entity.TotalSlots;
        var nextSoldSlots = req.SoldSlots ?? entity.SoldSlots;
        var nextHeldSlots = req.HeldSlots ?? entity.HeldSlots;
        var nextBlockedSlots = req.BlockedSlots ?? entity.BlockedSlots;
        var nextMinGuests = req.MinGuestsToOperate ?? entity.MinGuestsToOperate;
        var nextMaxGuestsPerBooking = req.MaxGuestsPerBooking ?? entity.MaxGuestsPerBooking;
        var nextWarningThreshold = req.WarningThreshold ?? entity.WarningThreshold;

        ValidateBusinessRules(
            nextTotalSlots,
            nextSoldSlots,
            nextHeldSlots,
            nextBlockedSlots,
            nextMinGuests,
            nextMaxGuestsPerBooking,
            nextWarningThreshold);

        if (req.TotalSlots.HasValue) entity.TotalSlots = req.TotalSlots.Value;
        if (req.SoldSlots.HasValue) entity.SoldSlots = req.SoldSlots.Value;
        if (req.HeldSlots.HasValue) entity.HeldSlots = req.HeldSlots.Value;
        if (req.BlockedSlots.HasValue) entity.BlockedSlots = req.BlockedSlots.Value;
        if (req.MinGuestsToOperate.HasValue) entity.MinGuestsToOperate = req.MinGuestsToOperate;
        if (req.MaxGuestsPerBooking.HasValue) entity.MaxGuestsPerBooking = req.MaxGuestsPerBooking;
        if (req.WarningThreshold.HasValue) entity.WarningThreshold = req.WarningThreshold;
        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.AllowWaitlist.HasValue) entity.AllowWaitlist = req.AllowWaitlist.Value;
        if (req.AutoCloseWhenFull.HasValue) entity.AutoCloseWhenFull = req.AutoCloseWhenFull.Value;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        if (entity.AutoCloseWhenFull && entity.AvailableSlots <= 0 && entity.Status == TourCapacityStatus.Open)
            entity.Status = TourCapacityStatus.Full;

        if (entity.Status == TourCapacityStatus.Full && entity.AvailableSlots > 0)
            entity.Status = TourCapacityStatus.Open;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour schedule capacity was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ToggleDeleted(tourId, scheduleId, true, ct);

    [HttpPost("restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ToggleDeleted(tourId, scheduleId, false, ct);

    [HttpPost("activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ToggleActive(tourId, scheduleId, true, ct);

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ToggleActive(tourId, scheduleId, false, ct);

    [HttpPost("open")]
    public async Task<IActionResult> Open(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ChangeStatus(tourId, scheduleId, TourCapacityStatus.Open, ct);

    [HttpPost("limited")]
    public async Task<IActionResult> Limited(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ChangeStatus(tourId, scheduleId, TourCapacityStatus.Limited, ct);

    [HttpPost("full")]
    public async Task<IActionResult> Full(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ChangeStatus(tourId, scheduleId, TourCapacityStatus.Full, ct);

    [HttpPost("close")]
    public async Task<IActionResult> Close(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ChangeStatus(tourId, scheduleId, TourCapacityStatus.Closed, ct);

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => await ChangeStatus(tourId, scheduleId, TourCapacityStatus.Cancelled, ct);

    private async Task<IActionResult> ToggleDeleted(
        Guid tourId,
        Guid scheduleId,
        bool isDeleted,
        CancellationToken ct)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourScheduleCapacities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule capacity not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(
        Guid tourId,
        Guid scheduleId,
        bool isActive,
        CancellationToken ct)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourScheduleCapacities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule capacity not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ChangeStatus(
        Guid tourId,
        Guid scheduleId,
        TourCapacityStatus status,
        CancellationToken ct)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourScheduleCapacities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule capacity not found in current tenant." });

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        if (status == TourCapacityStatus.Open)
            entity.IsActive = true;

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

    private async Task EnsureScheduleExistsAsync(Guid tenantId, Guid tourId, Guid scheduleId, CancellationToken ct)
    {
        var exists = await _db.TourSchedules.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == scheduleId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour schedule not found in current tenant.");
    }

    private static void ValidateBusinessRules(
        int totalSlots,
        int soldSlots,
        int heldSlots,
        int blockedSlots,
        int? minGuestsToOperate,
        int? maxGuestsPerBooking,
        int? warningThreshold)
    {
        if (totalSlots <= 0)
            throw new ArgumentException("TotalSlots must be greater than 0.");

        if (soldSlots < 0)
            throw new ArgumentException("SoldSlots cannot be negative.");

        if (heldSlots < 0)
            throw new ArgumentException("HeldSlots cannot be negative.");

        if (blockedSlots < 0)
            throw new ArgumentException("BlockedSlots cannot be negative.");

        if (soldSlots + heldSlots + blockedSlots > totalSlots)
            throw new ArgumentException("SoldSlots + HeldSlots + BlockedSlots cannot exceed TotalSlots.");

        if (minGuestsToOperate.HasValue && minGuestsToOperate <= 0)
            throw new ArgumentException("MinGuestsToOperate must be greater than 0.");

        if (maxGuestsPerBooking.HasValue && maxGuestsPerBooking <= 0)
            throw new ArgumentException("MaxGuestsPerBooking must be greater than 0.");

        if (maxGuestsPerBooking.HasValue && maxGuestsPerBooking > totalSlots)
            throw new ArgumentException("MaxGuestsPerBooking cannot exceed TotalSlots.");

        if (warningThreshold.HasValue && warningThreshold < 0)
            throw new ArgumentException("WarningThreshold cannot be negative.");
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

    private static QlTourCapacityDetailDto MapDetail(TourScheduleCapacity x)
    {
        return new QlTourCapacityDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourScheduleId = x.TourScheduleId,
            TotalSlots = x.TotalSlots,
            SoldSlots = x.SoldSlots,
            HeldSlots = x.HeldSlots,
            BlockedSlots = x.BlockedSlots,
            AvailableSlots = x.AvailableSlots,
            MinGuestsToOperate = x.MinGuestsToOperate,
            MaxGuestsPerBooking = x.MaxGuestsPerBooking,
            WarningThreshold = x.WarningThreshold,
            Status = x.Status,
            AllowWaitlist = x.AllowWaitlist,
            AutoCloseWhenFull = x.AutoCloseWhenFull,
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

public sealed class QlTourCapacityDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourScheduleId { get; set; }
    public int TotalSlots { get; set; }
    public int SoldSlots { get; set; }
    public int HeldSlots { get; set; }
    public int BlockedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuestsPerBooking { get; set; }
    public int? WarningThreshold { get; set; }
    public TourCapacityStatus Status { get; set; }
    public bool AllowWaitlist { get; set; }
    public bool AutoCloseWhenFull { get; set; }
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

public sealed class QlTourCreateCapacityRequest
{
    public int TotalSlots { get; set; }
    public int? SoldSlots { get; set; }
    public int? HeldSlots { get; set; }
    public int? BlockedSlots { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuestsPerBooking { get; set; }
    public int? WarningThreshold { get; set; }
    public TourCapacityStatus Status { get; set; } = TourCapacityStatus.Open;
    public bool? AllowWaitlist { get; set; }
    public bool? AutoCloseWhenFull { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdateCapacityRequest
{
    public int? TotalSlots { get; set; }
    public int? SoldSlots { get; set; }
    public int? HeldSlots { get; set; }
    public int? BlockedSlots { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuestsPerBooking { get; set; }
    public int? WarningThreshold { get; set; }
    public TourCapacityStatus? Status { get; set; }
    public bool? AllowWaitlist { get; set; }
    public bool? AutoCloseWhenFull { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateCapacityResponse
{
    public Guid Id { get; set; }
}
