// FILE #210: TicketBooking.Api/Controllers/Hotels/AriAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/ari")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AriAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public AriAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // INVENTORY (RoomTypeInventory)
    // =========================================================

    [HttpGet("room-types/{roomTypeId:guid}/inventory")]
    public async Task<ActionResult<List<AdminInventoryCalendarItemDto>>> GetInventoryCalendar(
        Guid roomTypeId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        RequireAdminTenantContext();
        ValidateDateRange(fromDate, toDate);

        var tenantId = _tenant.TenantId!.Value;

        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == roomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return NotFound(new { message = "Room type not found in current switched tenant." });

        IQueryable<RoomTypeInventory> query = includeDeleted
            ? _db.RoomTypeInventories.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.RoomTypeInventories.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var items = await query.AsNoTracking()
            .Where(x => x.RoomTypeId == roomTypeId && x.Date >= fromDate && x.Date <= toDate)
            .OrderBy(x => x.Date)
            .Select(x => new AdminInventoryCalendarItemDto
            {
                Id = x.Id,
                RoomTypeId = x.RoomTypeId,
                Date = x.Date,
                TotalUnits = x.TotalUnits,
                SoldUnits = x.SoldUnits,
                HeldUnits = x.HeldUnits,
                AvailableUnits = x.TotalUnits - x.SoldUnits - x.HeldUnits,
                Status = x.Status,
                MinNights = x.MinNights,
                MaxNights = x.MaxNights,
                Notes = x.Notes,
                IsDeleted = x.IsDeleted,
                UpdatedAt = x.UpdatedAt,
                RowVersionBase64 = x.RowVersion != null && x.RowVersion.Length > 0
                    ? Convert.ToBase64String(x.RowVersion)
                    : null
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPut("room-types/{roomTypeId:guid}/inventory/bulk")]
    public async Task<IActionResult> BulkUpsertInventory(
        Guid roomTypeId,
        [FromBody] AdminInventoryBulkUpsertRequest req,
        CancellationToken ct = default)
    {
        RequireAdminTenantContext();
        ValidateInventoryBulk(req);

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64) && req.FromDate != req.ToDate)
            return BadRequest(new { message = "RowVersionBase64 can only be used for a single date bulk inventory update." });

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == roomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return NotFound(new { message = "Room type not found in current switched tenant." });

        var rows = await _db.RoomTypeInventories.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.RoomTypeId == roomTypeId &&
                x.Date >= req.FromDate &&
                x.Date <= req.ToDate)
            .ToListAsync(ct);

        var map = rows.ToDictionary(x => x.Date);

        foreach (var date in EachDate(req.FromDate, req.ToDate))
        {
            if (!map.TryGetValue(date, out var row))
            {
                row = new RoomTypeInventory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    RoomTypeId = roomTypeId,
                    Date = date,
                    TotalUnits = req.TotalUnits,
                    SoldUnits = req.SoldUnits ?? 0,
                    HeldUnits = req.HeldUnits ?? 0,
                    Status = req.Status ?? InventoryStatus.Open,
                    MinNights = req.MinNights,
                    MaxNights = req.MaxNights,
                    Notes = NullIfWhiteSpace(req.Notes),
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId,
                    UpdatedAt = now,
                    UpdatedByUserId = userId
                };

                _db.RoomTypeInventories.Add(row);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(req.RowVersionBase64);
                    _db.Entry(row).Property(x => x.RowVersion).OriginalValue = bytes;
                }
                catch
                {
                    return BadRequest(new { message = "RowVersionBase64 is invalid." });
                }
            }

            row.TotalUnits = req.TotalUnits;
            if (req.SoldUnits.HasValue) row.SoldUnits = req.SoldUnits.Value;
            if (req.HeldUnits.HasValue) row.HeldUnits = req.HeldUnits.Value;
            if (req.Status.HasValue) row.Status = req.Status.Value;
            row.MinNights = req.MinNights;
            row.MaxNights = req.MaxNights;
            row.Notes = NullIfWhiteSpace(req.Notes);
            row.IsDeleted = false;
            row.UpdatedAt = now;
            row.UpdatedByUserId = userId;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Inventory was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("room-types/{roomTypeId:guid}/inventory/delete-range")]
    public async Task<IActionResult> DeleteInventoryRange(
        Guid roomTypeId,
        [FromBody] AdminInventoryDeleteRangeRequest req,
        CancellationToken ct = default)
    {
        RequireAdminTenantContext();
        ValidateDateRange(req.FromDate, req.ToDate);

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var rows = await _db.RoomTypeInventories.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.RoomTypeId == roomTypeId &&
                x.Date >= req.FromDate &&
                x.Date <= req.ToDate)
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.UpdatedAt = DateTimeOffset.Now;
            row.UpdatedByUserId = userId;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, affected = rows.Count });
    }

    // =========================================================
    // DAILY RATES (DailyRate)
    // =========================================================

    [HttpGet("rate-plan-room-types/{ratePlanRoomTypeId:guid}/daily-rates")]
    public async Task<ActionResult<List<AdminDailyRateCalendarItemDto>>> GetDailyRates(
        Guid ratePlanRoomTypeId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        RequireAdminTenantContext();
        ValidateDateRange(fromDate, toDate);

        var tenantId = _tenant.TenantId!.Value;

        var mappingExists = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == ratePlanRoomTypeId && !x.IsDeleted, ct);

        if (!mappingExists)
            return NotFound(new { message = "RatePlanRoomType not found in current switched tenant." });

        IQueryable<DailyRate> query = includeDeleted
            ? _db.DailyRates.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.DailyRates.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var items = await query.AsNoTracking()
            .Where(x => x.RatePlanRoomTypeId == ratePlanRoomTypeId && x.Date >= fromDate && x.Date <= toDate)
            .OrderBy(x => x.Date)
            .Select(x => new AdminDailyRateCalendarItemDto
            {
                Id = x.Id,
                RatePlanRoomTypeId = x.RatePlanRoomTypeId,
                Date = x.Date,
                Price = x.Price,
                CurrencyCode = x.CurrencyCode,
                BasePrice = x.BasePrice,
                Taxes = x.Taxes,
                Fees = x.Fees,
                IsActive = x.IsActive,
                MetadataJson = x.MetadataJson,
                IsDeleted = x.IsDeleted,
                UpdatedAt = x.UpdatedAt,
                RowVersionBase64 = x.RowVersion != null && x.RowVersion.Length > 0
                    ? Convert.ToBase64String(x.RowVersion)
                    : null
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPut("rate-plan-room-types/{ratePlanRoomTypeId:guid}/daily-rates/bulk")]
    public async Task<IActionResult> BulkUpsertDailyRates(
        Guid ratePlanRoomTypeId,
        [FromBody] AdminDailyRateBulkUpsertRequest req,
        CancellationToken ct = default)
    {
        RequireAdminTenantContext();
        ValidateDailyRateBulk(req);

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64) && req.FromDate != req.ToDate)
            return BadRequest(new { message = "RowVersionBase64 can only be used for a single date bulk daily rate update." });

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var mappingExists = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == ratePlanRoomTypeId && !x.IsDeleted, ct);

        if (!mappingExists)
            return NotFound(new { message = "RatePlanRoomType not found in current switched tenant." });

        var rows = await _db.DailyRates.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.RatePlanRoomTypeId == ratePlanRoomTypeId &&
                x.Date >= req.FromDate &&
                x.Date <= req.ToDate)
            .ToListAsync(ct);

        var map = rows.ToDictionary(x => x.Date);

        foreach (var date in EachDate(req.FromDate, req.ToDate))
        {
            if (!map.TryGetValue(date, out var row))
            {
                row = new DailyRate
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    RatePlanRoomTypeId = ratePlanRoomTypeId,
                    Date = date,
                    Price = req.Price,
                    CurrencyCode = string.IsNullOrWhiteSpace(req.CurrencyCode) ? "VND" : req.CurrencyCode!.Trim(),
                    BasePrice = req.BasePrice,
                    Taxes = req.Taxes,
                    Fees = req.Fees,
                    IsActive = req.IsActive ?? true,
                    MetadataJson = NullIfWhiteSpace(req.MetadataJson),
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId,
                    UpdatedAt = now,
                    UpdatedByUserId = userId
                };

                _db.DailyRates.Add(row);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(req.RowVersionBase64);
                    _db.Entry(row).Property(x => x.RowVersion).OriginalValue = bytes;
                }
                catch
                {
                    return BadRequest(new { message = "RowVersionBase64 is invalid." });
                }
            }

            row.Price = req.Price;
            row.CurrencyCode = string.IsNullOrWhiteSpace(req.CurrencyCode) ? row.CurrencyCode : req.CurrencyCode!.Trim();
            row.BasePrice = req.BasePrice;
            row.Taxes = req.Taxes;
            row.Fees = req.Fees;
            if (req.IsActive.HasValue) row.IsActive = req.IsActive.Value;
            row.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
            row.IsDeleted = false;
            row.UpdatedAt = now;
            row.UpdatedByUserId = userId;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Daily rates were changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("rate-plan-room-types/{ratePlanRoomTypeId:guid}/daily-rates/delete-range")]
    public async Task<IActionResult> DeleteDailyRatesRange(
        Guid ratePlanRoomTypeId,
        [FromBody] AdminDailyRateDeleteRangeRequest req,
        CancellationToken ct = default)
    {
        RequireAdminTenantContext();
        ValidateDateRange(req.FromDate, req.ToDate);

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var rows = await _db.DailyRates.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.RatePlanRoomTypeId == ratePlanRoomTypeId &&
                x.Date >= req.FromDate &&
                x.Date <= req.ToDate)
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.UpdatedAt = DateTimeOffset.Now;
            row.UpdatedByUserId = userId;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, affected = rows.Count });
    }

    private void RequireAdminTenantContext()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin ARI operations require switched tenant context (X-TenantId).");
    }

    private static void ValidateDateRange(DateOnly fromDate, DateOnly toDate)
    {
        if (toDate < fromDate)
            throw new ArgumentException("ToDate must be greater than or equal to FromDate.");

        if (toDate.DayNumber - fromDate.DayNumber > 366)
            throw new ArgumentException("Date range cannot exceed 366 days.");
    }

    private static void ValidateInventoryBulk(AdminInventoryBulkUpsertRequest req)
    {
        ValidateDateRange(req.FromDate, req.ToDate);

        if (req.TotalUnits < 0)
            throw new ArgumentException("TotalUnits must be >= 0.");

        if (req.SoldUnits.HasValue && req.SoldUnits.Value < 0)
            throw new ArgumentException("SoldUnits must be >= 0.");

        if (req.HeldUnits.HasValue && req.HeldUnits.Value < 0)
            throw new ArgumentException("HeldUnits must be >= 0.");

        if (req.MinNights.HasValue && req.MinNights.Value < 0)
            throw new ArgumentException("MinNights must be >= 0.");

        if (req.MaxNights.HasValue && req.MaxNights.Value < 0)
            throw new ArgumentException("MaxNights must be >= 0.");
    }

    private static void ValidateDailyRateBulk(AdminDailyRateBulkUpsertRequest req)
    {
        ValidateDateRange(req.FromDate, req.ToDate);

        if (req.Price < 0)
            throw new ArgumentException("Price must be >= 0.");

        if (req.BasePrice.HasValue && req.BasePrice.Value < 0)
            throw new ArgumentException("BasePrice must be >= 0.");

        if (req.Taxes.HasValue && req.Taxes.Value < 0)
            throw new ArgumentException("Taxes must be >= 0.");

        if (req.Fees.HasValue && req.Fees.Value < 0)
            throw new ArgumentException("Fees must be >= 0.");

        if (!string.IsNullOrWhiteSpace(req.CurrencyCode) && req.CurrencyCode!.Trim().Length > 10)
            throw new ArgumentException("CurrencyCode max length is 10.");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static IEnumerable<DateOnly> EachDate(DateOnly fromDate, DateOnly toDate)
    {
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            yield return d;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AdminInventoryCalendarItemDto
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public DateOnly Date { get; set; }
    public int TotalUnits { get; set; }
    public int SoldUnits { get; set; }
    public int HeldUnits { get; set; }
    public int AvailableUnits { get; set; }
    public InventoryStatus Status { get; set; }
    public int? MinNights { get; set; }
    public int? MaxNights { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminInventoryBulkUpsertRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalUnits { get; set; }
    public int? SoldUnits { get; set; }
    public int? HeldUnits { get; set; }
    public InventoryStatus? Status { get; set; }
    public int? MinNights { get; set; }
    public int? MaxNights { get; set; }
    public string? Notes { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminInventoryDeleteRangeRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public sealed class AdminDailyRateCalendarItemDto
{
    public Guid Id { get; set; }
    public Guid RatePlanRoomTypeId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal? BasePrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public bool IsActive { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminDailyRateBulkUpsertRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal Price { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public bool? IsActive { get; set; }
    public string? MetadataJson { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminDailyRateDeleteRangeRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

