//FILE: TicketBooking.Api/Controllers/Admin/FlightCabinSeatMapsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/flight/cabin-seat-maps")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightCabinSeatMapsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightCabinSeatMapsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class CabinSeatMapUpsertRequest
    {
        public Guid AircraftModelId { get; set; }
        public CabinClass CabinClass { get; set; } = CabinClass.Economy;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public int DeckCount { get; set; } = 1;
        public string? LayoutVersion { get; set; }
        public string? SeatLabelScheme { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class RegenerateSeatsRequest
    {
        public bool SoftDeleteMissing { get; set; } = true;
        public string? SeatLabelScheme { get; set; }
        public string? DefaultSeatType { get; set; } = "Standard";
        public string? DefaultSeatClass { get; set; } = "Standard";
        public decimal? DefaultPriceModifier { get; set; } = 0m;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] Guid? aircraftModelId,
        [FromQuery] CabinClass? cabinClass,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        IQueryable<CabinSeatMap> query = _db.FlightCabinSeatMaps.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (aircraftModelId.HasValue && aircraftModelId.Value != Guid.Empty)
            query = query.Where(x => x.AircraftModelId == aircraftModelId.Value);

        if (cabinClass.HasValue)
            query = query.Where(x => x.CabinClass == cabinClass.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                x.Name.ToUpper().Contains(uq) ||
                (x.LayoutVersion != null && x.LayoutVersion.ToUpper().Contains(uq)) ||
                (x.SeatLabelScheme != null && x.SeatLabelScheme.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.AircraftModelId)
            .ThenBy(x => x.CabinClass)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                CabinClass = x.CabinClass.ToString(),
                x.Code,
                x.Name,
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount,
                x.LayoutVersion,
                x.SeatLabelScheme,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
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

        IQueryable<CabinSeatMap> query = _db.FlightCabinSeatMaps.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                CabinClass = x.CabinClass.ToString(),
                x.Code,
                x.Name,
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount,
                x.LayoutVersion,
                x.SeatLabelScheme,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Cabin seat map not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CabinSeatMapUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        if (req.AircraftModelId == Guid.Empty)
            return BadRequest(new { message = "AircraftModelId is required." });

        var code = NormalizeRequired(req.Code, 80);
        var name = NormalizeRequired(req.Name, 200);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        if (req.TotalRows <= 0)
            return BadRequest(new { message = "TotalRows must be > 0." });

        if (req.TotalColumns <= 0)
            return BadRequest(new { message = "TotalColumns must be > 0." });

        if (req.DeckCount <= 0)
            return BadRequest(new { message = "DeckCount must be > 0." });

        var seatLabelScheme = NormalizeSeatLabelScheme(req.SeatLabelScheme, req.TotalColumns);
        if (seatLabelScheme is null)
            return BadRequest(new { message = "SeatLabelScheme is invalid or shorter than TotalColumns." });

        var modelExists = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AircraftModelId && !x.IsDeleted, ct);

        if (!modelExists)
            return BadRequest(new { message = "AircraftModelId not found." });

        var codeExists = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = $"Cabin seat map code '{code}' already exists." });

        var entity = new CabinSeatMap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AircraftModelId = req.AircraftModelId,
            CabinClass = req.CabinClass,
            Code = code,
            Name = name,
            TotalRows = req.TotalRows,
            TotalColumns = req.TotalColumns,
            DeckCount = req.DeckCount,
            LayoutVersion = TrimOrNull(req.LayoutVersion, 50),
            SeatLabelScheme = seatLabelScheme,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightCabinSeatMaps.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] CabinSeatMapUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Cabin seat map not found." });

        if (req.AircraftModelId == Guid.Empty)
            return BadRequest(new { message = "AircraftModelId is required." });

        var code = NormalizeRequired(req.Code, 80);
        var name = NormalizeRequired(req.Name, 200);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        if (req.TotalRows <= 0)
            return BadRequest(new { message = "TotalRows must be > 0." });

        if (req.TotalColumns <= 0)
            return BadRequest(new { message = "TotalColumns must be > 0." });

        if (req.DeckCount <= 0)
            return BadRequest(new { message = "DeckCount must be > 0." });

        var seatLabelScheme = NormalizeSeatLabelScheme(req.SeatLabelScheme, req.TotalColumns);
        if (seatLabelScheme is null)
            return BadRequest(new { message = "SeatLabelScheme is invalid or shorter than TotalColumns." });

        var modelExists = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AircraftModelId && !x.IsDeleted, ct);

        if (!modelExists)
            return BadRequest(new { message = "AircraftModelId not found." });

        var codeExists = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

        if (codeExists)
            return Conflict(new { message = $"Cabin seat map code '{code}' already exists." });

        entity.AircraftModelId = req.AircraftModelId;
        entity.CabinClass = req.CabinClass;
        entity.Code = code;
        entity.Name = name;
        entity.TotalRows = req.TotalRows;
        entity.TotalColumns = req.TotalColumns;
        entity.DeckCount = req.DeckCount;
        entity.LayoutVersion = TrimOrNull(req.LayoutVersion, 50);
        entity.SeatLabelScheme = seatLabelScheme;
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Cabin seat map not found." });

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

        var entity = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Cabin seat map not found." });

        if (entity.IsDeleted)
        {
            var codeExists = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == entity.Code && x.Id != id && !x.IsDeleted, ct);

            if (codeExists)
                return Conflict(new { message = $"Cannot restore: cabin seat map code '{entity.Code}' already exists." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    /// <summary>
    /// Idempotent seat regeneration by map dimensions + SeatLabelScheme + DeckCount.
    /// Creates missing seats, updates existing seats, optionally soft-deletes seats that no longer fit the layout.
    /// </summary>
    [HttpPost("{id:guid}/regenerate-seats")]
    public async Task<IActionResult> RegenerateSeats(
        [FromRoute] Guid id,
        [FromBody] RegenerateSeatsRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;

        var map = await _db.FlightCabinSeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (map is null)
            return NotFound(new { message = "Cabin seat map not found." });

        if (map.TotalRows <= 0 || map.TotalColumns <= 0 || map.DeckCount <= 0)
            return BadRequest(new { message = "Invalid cabin seat map dimensions." });

        var seatLabelScheme = NormalizeSeatLabelScheme(req.SeatLabelScheme ?? map.SeatLabelScheme, map.TotalColumns);
        if (seatLabelScheme is null)
            return BadRequest(new { message = "SeatLabelScheme is invalid or shorter than TotalColumns." });

        var defaultSeatType = TrimOrNull(req.DefaultSeatType, 50) ?? "Standard";
        var defaultSeatClass = TrimOrNull(req.DefaultSeatClass, 50) ?? "Standard";
        var defaultPriceModifier = req.DefaultPriceModifier ?? 0m;

        var existingSeats = await _db.FlightCabinSeats.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.CabinSeatMapId == map.Id)
            .ToListAsync(ct);

        var keepByCompositeKey = new Dictionary<string, CabinSeat>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in existingSeats.GroupBy(x => BuildSeatKey(x.DeckIndex, x.SeatNumber), StringComparer.OrdinalIgnoreCase))
        {
            var keep = group
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .First();

            keepByCompositeKey[group.Key] = keep;

            foreach (var duplicate in group.Where(x => x.Id != keep.Id))
            {
                if (!duplicate.IsDeleted)
                {
                    duplicate.IsDeleted = true;
                    duplicate.UpdatedAt = now;
                }
            }
        }

        var touchedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var deck = 1; deck <= map.DeckCount; deck++)
        {
            for (var row = 0; row < map.TotalRows; row++)
            {
                for (var col = 0; col < map.TotalColumns; col++)
                {
                    var letter = seatLabelScheme[col].ToString();
                    var seatNumber = $"{row + 1}{letter}";
                    var key = BuildSeatKey(deck, seatNumber);
                    touchedKeys.Add(key);

                    var isWindow = col == 0 || col == map.TotalColumns - 1;
                    var isAisle = IsAisleColumn(col, map.TotalColumns);

                    if (keepByCompositeKey.TryGetValue(key, out var existing))
                    {
                        var changed = false;

                        if (existing.RowIndex != row)
                        {
                            existing.RowIndex = row;
                            changed = true;
                        }

                        if (existing.ColumnIndex != col)
                        {
                            existing.ColumnIndex = col;
                            changed = true;
                        }

                        if (existing.DeckIndex != deck)
                        {
                            existing.DeckIndex = deck;
                            changed = true;
                        }

                        if (existing.IsWindow != isWindow)
                        {
                            existing.IsWindow = isWindow;
                            changed = true;
                        }

                        if (existing.IsAisle != isAisle)
                        {
                            existing.IsAisle = isAisle;
                            changed = true;
                        }

                        if (string.IsNullOrWhiteSpace(existing.SeatType))
                        {
                            existing.SeatType = defaultSeatType;
                            changed = true;
                        }

                        if (string.IsNullOrWhiteSpace(existing.SeatClass))
                        {
                            existing.SeatClass = defaultSeatClass;
                            changed = true;
                        }

                        if (existing.PriceModifier is null)
                        {
                            existing.PriceModifier = defaultPriceModifier;
                            changed = true;
                        }

                        if (!existing.IsActive)
                        {
                            existing.IsActive = true;
                            changed = true;
                        }

                        if (existing.IsDeleted)
                        {
                            existing.IsDeleted = false;
                            changed = true;
                        }

                        if (changed)
                            existing.UpdatedAt = now;
                    }
                    else
                    {
                        _db.FlightCabinSeats.Add(new CabinSeat
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            CabinSeatMapId = map.Id,
                            SeatNumber = seatNumber,
                            RowIndex = row,
                            ColumnIndex = col,
                            DeckIndex = deck,
                            IsWindow = isWindow,
                            IsAisle = isAisle,
                            SeatType = defaultSeatType,
                            SeatClass = defaultSeatClass,
                            PriceModifier = defaultPriceModifier,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedAt = now
                        });
                    }
                }
            }
        }

        if (req.SoftDeleteMissing)
        {
            foreach (var seat in keepByCompositeKey.Values)
            {
                var key = BuildSeatKey(seat.DeckIndex, seat.SeatNumber);
                if (!touchedKeys.Contains(key) && !seat.IsDeleted)
                {
                    seat.IsDeleted = true;
                    seat.UpdatedAt = now;
                }
            }
        }

        if (!string.Equals(map.SeatLabelScheme, seatLabelScheme, StringComparison.Ordinal))
        {
            map.SeatLabelScheme = seatLabelScheme;
            map.UpdatedAt = now;
        }

        if (map.IsDeleted)
        {
            map.IsDeleted = false;
            map.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            ok = true,
            mapId = map.Id,
            totalRows = map.TotalRows,
            totalColumns = map.TotalColumns,
            deckCount = map.DeckCount,
            seatLabelScheme = map.SeatLabelScheme
        });
    }

    private static string BuildSeatKey(int deckIndex, string seatNumber)
        => $"{deckIndex}:{seatNumber.Trim().ToUpperInvariant()}";

    private static bool IsAisleColumn(int columnIndex, int totalColumns)
    {
        if (totalColumns >= 6)
            return columnIndex == 2 || columnIndex == 3;

        if (totalColumns >= 4)
            return columnIndex == 1 || columnIndex == totalColumns - 2;

        return false;
    }

    private static string? NormalizeRequired(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? TrimOrNull(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? NormalizeSeatLabelScheme(string? input, int requiredColumns)
    {
        var value = string.IsNullOrWhiteSpace(input) ? "ABCDEF" : input.Trim().ToUpperInvariant();
        value = new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray());

        if (requiredColumns <= 0)
            return null;

        if (value.Length < requiredColumns)
            return null;

        if (value.Length > requiredColumns)
            value = value[..requiredColumns];

        foreach (var ch in value)
        {
            if (ch < 'A' || ch > 'Z')
                return null;
        }

        return value;
    }
}
