// FILE #061: TicketBooking.Api/Controllers/SeatMapsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Fleet;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/fleet/seat-maps")]
[Route("api/v{version:apiVersion}/qlnx/fleet/seat-maps")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class SeatMapsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public SeatMapsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] VehicleType? vehicleType = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        if (IsQlnxOnly() && !_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác quản lý sơ đồ ghế của QLNX." });

        if (IsQlnxOnly() && vehicleType.HasValue && !IsBusVehicleType(vehicleType.Value))
            return BadRequest(new { message = "QLNX chỉ được quản lý sơ đồ ghế xe khách hoặc xe tour." });

        IQueryable<SeatMap> query = _db.SeatMaps;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (IsQlnxOnly())
            query = query.Where(x => x.VehicleType == VehicleType.Bus || x.VehicleType == VehicleType.TourBus);
        else if (vehicleType.HasValue)
            query = query.Where(x => x.VehicleType == vehicleType.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.VehicleType,
                x.Code,
                x.Name,
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount,
                SeatCount = _db.Seats.IgnoreQueryFilters().Count(s => s.SeatMapId == x.Id && !s.IsDeleted),
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
        if (IsQlnxOnly() && !_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác quản lý sơ đồ ghế của QLNX." });

        IQueryable<SeatMap> query = _db.SeatMaps;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);
        if (IsQlnxOnly())
            query = query.Where(x => x.VehicleType == VehicleType.Bus || x.VehicleType == VehicleType.TourBus);

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "Không tìm thấy sơ đồ ghế." });

        var seatCount = await _db.Seats.IgnoreQueryFilters().CountAsync(x => x.SeatMapId == id && !x.IsDeleted, ct);

        return Ok(new { seatMap = item, seatCount });
    }

    public sealed class UpsertSeatMapRequest
    {
        public VehicleType VehicleType { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";

        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }

        public int DeckCount { get; set; } = 1;
        public string? LayoutVersion { get; set; }
        public string? SeatLabelScheme { get; set; }

        public bool IsActive { get; set; } = true;

        public string? MetadataJson { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertSeatMapRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        if (IsQlnxOnly() && !IsBusVehicleType(req.VehicleType))
            return BadRequest(new { message = "QLNX chỉ được tạo sơ đồ ghế xe khách hoặc xe tour." });

        Validate(req);

        var exists = await _db.SeatMaps.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.VehicleType == req.VehicleType &&
            x.Code == req.Code.Trim(), ct);

        if (exists) return Conflict(new { message = "Mã sơ đồ ghế đã tồn tại trong tenant này cho loại phương tiện đã chọn." });

        var entity = new SeatMap
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            VehicleType = req.VehicleType,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            TotalRows = req.TotalRows,
            TotalColumns = req.TotalColumns,
            DeckCount = req.DeckCount <= 0 ? 1 : req.DeckCount,
            LayoutVersion = req.LayoutVersion?.Trim(),
            SeatLabelScheme = req.SeatLabelScheme?.Trim(),
            IsActive = req.IsActive,
            MetadataJson = req.MetadataJson,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.SeatMaps.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertSeatMapRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        if (IsQlnxOnly() && !IsBusVehicleType(req.VehicleType))
            return BadRequest(new { message = "QLNX chỉ được cập nhật sơ đồ ghế xe khách hoặc xe tour." });

        Validate(req);

        var entity = await _db.SeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy sơ đồ ghế trong tenant này." });
        if (IsQlnxOnly() && !IsBusVehicleType(entity.VehicleType))
            return NotFound(new { message = "Không tìm thấy sơ đồ ghế xe khách trong tenant này." });

        var exists = await _db.SeatMaps.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.VehicleType == req.VehicleType &&
            x.Code == req.Code.Trim() &&
            x.Id != id, ct);

        if (exists) return Conflict(new { message = "Mã sơ đồ ghế đã tồn tại trong tenant này cho loại phương tiện đã chọn." });

        entity.VehicleType = req.VehicleType;
        entity.Code = req.Code.Trim();
        entity.Name = req.Name.Trim();
        entity.TotalRows = req.TotalRows;
        entity.TotalColumns = req.TotalColumns;
        entity.DeckCount = req.DeckCount <= 0 ? 1 : req.DeckCount;
        entity.LayoutVersion = req.LayoutVersion?.Trim();
        entity.SeatLabelScheme = req.SeatLabelScheme?.Trim();
        entity.IsActive = req.IsActive;
        entity.MetadataJson = req.MetadataJson;

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

        var entity = await _db.SeatMaps
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (entity is null) return NotFound(new { message = "Không tìm thấy sơ đồ ghế trong tenant này." });
        if (IsQlnxOnly() && !IsBusVehicleType(entity.VehicleType))
            return NotFound(new { message = "Không tìm thấy sơ đồ ghế xe khách trong tenant này." });

        _db.SeatMaps.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var entity = await _db.SeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy sơ đồ ghế." });
        if (IsQlnxOnly() && !IsBusVehicleType(entity.VehicleType))
            return NotFound(new { message = "Không tìm thấy sơ đồ ghế xe khách trong tenant này." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    public sealed class GenerateSeatsRequest
    {
        public string Prefix { get; set; } = "";      // e.g. "A" => A01, A02...
        public SeatType SeatType { get; set; } = SeatType.Standard;
        public SeatClass SeatClass { get; set; } = SeatClass.Any;

        public bool MarkWindow { get; set; } = true;
        public bool MarkAisle { get; set; } = true;

        public bool OverwriteExisting { get; set; } = false; // if true, soft delete old seats and regenerate
    }

    /// <summary>
    /// Generate a simple grid of seats for a seat map:
    /// - SeatNumber: {Prefix}{RowIndex+1}{ColumnIndex+1} (e.g. A11, A12...)
    /// - For bus sleeper you can create per deck.
    /// This is a starter tool; later we can implement custom templates.
    /// </summary>
    [HttpPost("{id:guid}/generate-seats")]
    public async Task<IActionResult> GenerateSeats(Guid id, [FromBody] GenerateSeatsRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var map = await _db.SeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (map is null) return NotFound(new { message = "Không tìm thấy sơ đồ ghế trong tenant này." });
        if (IsQlnxOnly() && !IsBusVehicleType(map.VehicleType))
            return BadRequest(new { message = "QLNX chỉ được sinh ghế cho sơ đồ xe khách hoặc xe tour." });

        // existing seats
        var existing = await _db.Seats.IgnoreQueryFilters()
            .Where(x => x.SeatMapId == id)
            .ToListAsync(ct);

        if (existing.Count > 0 && !req.OverwriteExisting)
            return Conflict(new { message = "Sơ đồ ghế đã có ghế. Hãy bật overwriteExisting=true để sinh lại." });

        if (existing.Count > 0 && req.OverwriteExisting)
        {
            foreach (var s in existing)
                _db.Seats.Remove(s); // interceptor => soft delete
            await _db.SaveChangesAsync(ct);
        }

        var prefix = (req.Prefix ?? "").Trim();
        if (prefix.Length > 5) prefix = prefix.Substring(0, 5);

        var seatsToAdd = new List<Seat>();
        var now = DateTimeOffset.Now;

        var decks = map.DeckCount <= 0 ? 1 : map.DeckCount;

        for (var deck = 1; deck <= decks; deck++)
        {
            for (var r = 0; r < map.TotalRows; r++)
            {
                for (var c = 0; c < map.TotalColumns; c++)
                {
                    // Simple seat number
                    var seatNumber = $"{prefix}{(r + 1):00}{(c + 1):00}";
                    if (decks > 1) seatNumber = $"{seatNumber}-D{deck}";

                    var isWindow = req.MarkWindow && (c == 0 || c == map.TotalColumns - 1);
                    var isAisle = req.MarkAisle && (map.TotalColumns >= 3 && (c == 1 || c == map.TotalColumns - 2));

                    seatsToAdd.Add(new Seat
                    {
                        Id = Guid.NewGuid(),
                        TenantId = _tenantContext.TenantId!.Value,
                        SeatMapId = map.Id,
                        SeatNumber = seatNumber,
                        RowIndex = r,
                        ColumnIndex = c,
                        DeckIndex = deck,
                        SeatType = req.SeatType,
                        SeatClass = req.SeatClass,
                        IsWindow = isWindow,
                        IsAisle = isAisle,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now
                    });
                }
            }
        }

        _db.Seats.AddRange(seatsToAdd);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, created = seatsToAdd.Count });
    }

    private static void Validate(UpsertSeatMapRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Code)) throw new InvalidOperationException("Mã là bắt buộc.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Tên là bắt buộc.");

        if (req.Code.Length > 50) throw new InvalidOperationException("Mã tối đa 50 ký tự.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Tên tối đa 200 ký tự.");
        if (req.TotalRows <= 0 || req.TotalRows > 200) throw new InvalidOperationException("Tổng số hàng phải trong khoảng 1 đến 200.");
        if (req.TotalColumns <= 0 || req.TotalColumns > 50) throw new InvalidOperationException("Tổng số cột phải trong khoảng 1 đến 50.");
        if (req.DeckCount <= 0 || req.DeckCount > 5) throw new InvalidOperationException("Số tầng phải trong khoảng 1 đến 5.");
    }

    private bool IsQlnxOnly()
        => User.IsInRole(RoleNames.QLNX) && !User.IsInRole(RoleNames.Admin);

    private static bool IsBusVehicleType(VehicleType vehicleType)
        => vehicleType is VehicleType.Bus or VehicleType.TourBus;
}
