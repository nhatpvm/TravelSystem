//FILE: TicketBooking.Api/Controllers/Admin/FlightCabinSeatsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/flight/cabin-seats")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightCabinSeatsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightCabinSeatsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class CabinSeatUpsertRequest
    {
        public Guid CabinSeatMapId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public int DeckIndex { get; set; } = 1;
        public bool IsWindow { get; set; }
        public bool IsAisle { get; set; }
        public string? SeatType { get; set; }
        public string? SeatClass { get; set; }
        public decimal? PriceModifier { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class CabinSeatPatchRequest
    {
        public string? SeatType { get; set; }
        public string? SeatClass { get; set; }
        public decimal? PriceModifier { get; set; }
        public bool? IsActive { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid cabinSeatMapId,
        [FromQuery] string? q,
        [FromQuery] int? deckIndex,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        if (cabinSeatMapId == Guid.Empty)
            return BadRequest(new { message = "cabinSeatMapId is required." });

        if (deckIndex.HasValue && deckIndex.Value <= 0)
            return BadRequest(new { message = "deckIndex must be > 0." });

        var tenantId = _tenant.TenantId.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : (pageSize > 200 ? 200 : pageSize);

        IQueryable<CabinSeat> query = _db.FlightCabinSeats.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId && x.CabinSeatMapId == cabinSeatMapId);

        if (deckIndex.HasValue)
            query = query.Where(x => x.DeckIndex == deckIndex.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.SeatNumber.ToUpper().Contains(uq) ||
                (x.SeatType != null && x.SeatType.ToUpper().Contains(uq)) ||
                (x.SeatClass != null && x.SeatClass.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.DeckIndex)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .ThenBy(x => x.SeatNumber)
            .Select(x => new
            {
                x.Id,
                x.CabinSeatMapId,
                x.SeatNumber,
                x.RowIndex,
                x.ColumnIndex,
                x.DeckIndex,
                x.IsWindow,
                x.IsAisle,
                x.SeatType,
                x.SeatClass,
                x.PriceModifier,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            page,
            pageSize,
            total,
            items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<CabinSeat> query = _db.FlightCabinSeats.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.CabinSeatMapId,
                x.SeatNumber,
                x.RowIndex,
                x.ColumnIndex,
                x.DeckIndex,
                x.IsWindow,
                x.IsAisle,
                x.SeatType,
                x.SeatClass,
                x.PriceModifier,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Cabin seat not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CabinSeatUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var validationError = await ValidateUpsertAsync(tenantId, req, idToExclude: null, ct);
        if (validationError is not null)
            return validationError;

        var entity = new CabinSeat
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CabinSeatMapId = req.CabinSeatMapId,
            SeatNumber = NormalizeRequired(req.SeatNumber, 20)!,
            RowIndex = req.RowIndex,
            ColumnIndex = req.ColumnIndex,
            DeckIndex = req.DeckIndex,
            IsWindow = req.IsWindow,
            IsAisle = req.IsAisle,
            SeatType = TrimOrNull(req.SeatType, 50),
            SeatClass = TrimOrNull(req.SeatClass, 50),
            PriceModifier = req.PriceModifier,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightCabinSeats.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] CabinSeatUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightCabinSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Cabin seat not found." });

        var validationError = await ValidateUpsertAsync(tenantId, req, id, ct);
        if (validationError is not null)
            return validationError;

        entity.CabinSeatMapId = req.CabinSeatMapId;
        entity.SeatNumber = NormalizeRequired(req.SeatNumber, 20)!;
        entity.RowIndex = req.RowIndex;
        entity.ColumnIndex = req.ColumnIndex;
        entity.DeckIndex = req.DeckIndex;
        entity.IsWindow = req.IsWindow;
        entity.IsAisle = req.IsAisle;
        entity.SeatType = TrimOrNull(req.SeatType, 50);
        entity.SeatClass = TrimOrNull(req.SeatClass, 50);
        entity.PriceModifier = req.PriceModifier;
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(
        [FromRoute] Guid id,
        [FromBody] CabinSeatPatchRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightCabinSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Cabin seat not found." });

        var changed = false;

        if (req.SeatType is not null)
        {
            var value = TrimOrNull(req.SeatType, 50);
            if (entity.SeatType != value)
            {
                entity.SeatType = value;
                changed = true;
            }
        }

        if (req.SeatClass is not null)
        {
            var value = TrimOrNull(req.SeatClass, 50);
            if (entity.SeatClass != value)
            {
                entity.SeatClass = value;
                changed = true;
            }
        }

        if (req.PriceModifier.HasValue && entity.PriceModifier != req.PriceModifier.Value)
        {
            entity.PriceModifier = req.PriceModifier.Value;
            changed = true;
        }

        if (req.IsActive.HasValue && entity.IsActive != req.IsActive.Value)
        {
            entity.IsActive = req.IsActive.Value;
            changed = true;
        }

        if (entity.IsDeleted)
        {
            entity.IsDeleted = false;
            changed = true;
        }

        if (!changed)
            return Ok(new { ok = true, changed = false });

        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, changed = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightCabinSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Cabin seat not found." });

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightCabinSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Cabin seat not found." });

        if (entity.IsDeleted)
        {
            var seatNumberExists = await _db.FlightCabinSeats.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.CabinSeatMapId == entity.CabinSeatMapId &&
                    x.SeatNumber == entity.SeatNumber &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (seatNumberExists)
                return Conflict(new { message = $"Cannot restore: seat number '{entity.SeatNumber}' already exists in this cabin seat map." });

            var coordinateExists = await _db.FlightCabinSeats.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.CabinSeatMapId == entity.CabinSeatMapId &&
                    x.DeckIndex == entity.DeckIndex &&
                    x.RowIndex == entity.RowIndex &&
                    x.ColumnIndex == entity.ColumnIndex &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (coordinateExists)
                return Conflict(new { message = "Cannot restore: another active seat already uses the same deck/row/column position." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private async Task<IActionResult?> ValidateUpsertAsync(
        Guid tenantId,
        CabinSeatUpsertRequest req,
        Guid? idToExclude,
        CancellationToken ct)
    {
        if (req.CabinSeatMapId == Guid.Empty)
            return BadRequest(new { message = "CabinSeatMapId is required." });

        var seatNumber = NormalizeRequired(req.SeatNumber, 20);
        if (seatNumber is null)
            return BadRequest(new { message = "SeatNumber is required." });

        if (req.RowIndex < 0)
            return BadRequest(new { message = "RowIndex must be >= 0." });

        if (req.ColumnIndex < 0)
            return BadRequest(new { message = "ColumnIndex must be >= 0." });

        if (req.DeckIndex <= 0)
            return BadRequest(new { message = "DeckIndex must be > 0." });

        var seatMap = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Id == req.CabinSeatMapId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount
            })
            .FirstOrDefaultAsync(ct);

        if (seatMap is null)
            return BadRequest(new { message = "CabinSeatMapId not found." });

        if (req.RowIndex >= seatMap.TotalRows)
            return BadRequest(new { message = $"RowIndex must be between 0 and {seatMap.TotalRows - 1}." });

        if (req.ColumnIndex >= seatMap.TotalColumns)
            return BadRequest(new { message = $"ColumnIndex must be between 0 and {seatMap.TotalColumns - 1}." });

        if (req.DeckIndex > seatMap.DeckCount)
            return BadRequest(new { message = $"DeckIndex must be between 1 and {seatMap.DeckCount}." });

        var seatNumberExists = await _db.FlightCabinSeats.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.CabinSeatMapId == req.CabinSeatMapId &&
                x.SeatNumber == seatNumber &&
                (!idToExclude.HasValue || x.Id != idToExclude.Value) &&
                !x.IsDeleted, ct);

        if (seatNumberExists)
            return Conflict(new { message = $"SeatNumber '{seatNumber}' already exists in this cabin seat map." });

        var coordinateExists = await _db.FlightCabinSeats.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.CabinSeatMapId == req.CabinSeatMapId &&
                x.DeckIndex == req.DeckIndex &&
                x.RowIndex == req.RowIndex &&
                x.ColumnIndex == req.ColumnIndex &&
                (!idToExclude.HasValue || x.Id != idToExclude.Value) &&
                !x.IsDeleted, ct);

        if (coordinateExists)
            return Conflict(new { message = "Another active seat already uses the same deck/row/column position." });

        return null;
    }

    private static string? NormalizeRequired(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim().ToUpperInvariant();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? TrimOrNull(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
