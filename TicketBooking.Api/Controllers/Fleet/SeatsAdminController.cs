// FILE #062: TicketBooking.Api/Controllers/SeatsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/fleet/seats")]
[Route("api/v{version:apiVersion}/qlnx/fleet/seats")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class SeatsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public SeatsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid seatMapId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (IsQlnxOnly() && !_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác quản lý ghế của QLNX." });

        if (seatMapId == Guid.Empty)
            return BadRequest(new { message = "seatMapId là bắt buộc." });

        if (IsQlnxOnly() && !await IsBusSeatMapAsync(seatMapId, ct))
            return NotFound(new { message = "Không tìm thấy sơ đồ ghế xe khách trong tenant này." });

        IQueryable<Seat> query = _db.Seats;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        query = query.Where(x => x.SeatMapId == seatMapId);

        var items = await query
            .OrderBy(x => x.DeckIndex)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.SeatMapId,
                x.SeatNumber,
                x.RowIndex,
                x.ColumnIndex,
                x.DeckIndex,
                x.SeatType,
                x.SeatClass,
                x.IsAisle,
                x.IsWindow,
                x.PriceModifier,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        if (IsQlnxOnly() && !_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác quản lý ghế của QLNX." });

        IQueryable<Seat> query = _db.Seats;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "Không tìm thấy ghế." });
        if (IsQlnxOnly() && !await IsBusSeatMapAsync(item.SeatMapId, ct))
            return NotFound(new { message = "Không tìm thấy ghế xe khách trong tenant này." });

        return Ok(item);
    }

    public sealed class UpdateSeatRequest
    {
        public string SeatNumber { get; set; } = "";
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public int DeckIndex { get; set; } = 1;

        public SeatType SeatType { get; set; } = SeatType.Standard;
        public SeatClass SeatClass { get; set; } = SeatClass.Any;

        public bool IsAisle { get; set; }
        public bool IsWindow { get; set; }
        public decimal? PriceModifier { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSeatRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);

        var seat = await _db.Seats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (seat is null) return NotFound(new { message = "Không tìm thấy ghế trong tenant này." });

        if (IsQlnxOnly() && !await IsBusSeatMapAsync(seat.SeatMapId, ct))
            return NotFound(new { message = "Không tìm thấy ghế xe khách trong tenant này." });

        var exists = await _db.Seats.IgnoreQueryFilters().AnyAsync(x =>
            x.SeatMapId == seat.SeatMapId &&
            x.SeatNumber == req.SeatNumber.Trim() &&
            x.Id != id &&
            !x.IsDeleted, ct);

        if (exists) return Conflict(new { message = "Số ghế đã tồn tại trong sơ đồ ghế này." });

        seat.SeatNumber = req.SeatNumber.Trim();
        seat.RowIndex = req.RowIndex;
        seat.ColumnIndex = req.ColumnIndex;
        seat.DeckIndex = req.DeckIndex <= 0 ? 1 : req.DeckIndex;
        seat.SeatType = req.SeatType;
        seat.SeatClass = req.SeatClass;
        seat.IsAisle = req.IsAisle;
        seat.IsWindow = req.IsWindow;
        seat.PriceModifier = req.PriceModifier;
        seat.IsActive = req.IsActive;
        seat.IsDeleted = false;
        seat.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(seat);
    }

    public sealed class BulkUpdateSeatsRequest
    {
        public Guid SeatMapId { get; set; }
        public List<Guid> SeatIds { get; set; } = new();

        public SeatType? SeatType { get; set; }
        public SeatClass? SeatClass { get; set; }
        public bool? IsActive { get; set; }

        public decimal? PriceModifier { get; set; }
        public bool SetPriceModifier { get; set; } = false;
    }

    [HttpPost("bulk-update")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateSeatsRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        if (req.SeatMapId == Guid.Empty)
            return BadRequest(new { message = "SeatMapId là bắt buộc." });

        if (req.SeatIds is null || req.SeatIds.Count == 0)
            return BadRequest(new { message = "SeatIds là bắt buộc." });

        if (IsQlnxOnly() && !await IsBusSeatMapAsync(req.SeatMapId, ct))
            return NotFound(new { message = "Không tìm thấy sơ đồ ghế xe khách trong tenant này." });

        var seats = await _db.Seats.IgnoreQueryFilters()
            .Where(x => x.TenantId == _tenantContext.TenantId && x.SeatMapId == req.SeatMapId && req.SeatIds.Contains(x.Id))
            .ToListAsync(ct);

        if (seats.Count == 0) return NotFound(new { message = "Không tìm thấy ghế nào." });

        var now = DateTimeOffset.Now;

        foreach (var s in seats)
        {
            if (req.SeatType.HasValue) s.SeatType = req.SeatType.Value;
            if (req.SeatClass.HasValue) s.SeatClass = req.SeatClass.Value;
            if (req.IsActive.HasValue) s.IsActive = req.IsActive.Value;

            if (req.SetPriceModifier)
                s.PriceModifier = req.PriceModifier;

            s.IsDeleted = false;
            s.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, updated = seats.Count });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var seat = await _db.Seats
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (seat is null) return NotFound(new { message = "Không tìm thấy ghế trong tenant này." });

        if (IsQlnxOnly() && !await IsBusSeatMapAsync(seat.SeatMapId, ct))
            return NotFound(new { message = "Không tìm thấy ghế xe khách trong tenant này." });

        _db.Seats.Remove(seat);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var seat = await _db.Seats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (seat is null)
            return NotFound(new { message = "Không tìm thấy ghế." });

        if (IsQlnxOnly() && !await IsBusSeatMapAsync(seat.SeatMapId, ct))
            return NotFound(new { message = "Không tìm thấy ghế xe khách trong tenant này." });

        seat.IsDeleted = false;
        seat.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private static void Validate(UpdateSeatRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.SeatNumber)) throw new InvalidOperationException("Số ghế là bắt buộc.");
        if (req.SeatNumber.Length > 20) throw new InvalidOperationException("Số ghế tối đa 20 ký tự.");
        if (req.RowIndex < 0 || req.RowIndex > 500) throw new InvalidOperationException("Chỉ số hàng không hợp lệ.");
        if (req.ColumnIndex < 0 || req.ColumnIndex > 200) throw new InvalidOperationException("Chỉ số cột không hợp lệ.");
        if (req.DeckIndex <= 0 || req.DeckIndex > 5) throw new InvalidOperationException("Chỉ số tầng không hợp lệ.");
    }

    private bool IsQlnxOnly()
        => User.IsInRole(RoleNames.QLNX) && !User.IsInRole(RoleNames.Admin);

    private Task<bool> IsBusSeatMapAsync(Guid seatMapId, CancellationToken ct)
        => _db.SeatMaps.IgnoreQueryFilters().AnyAsync(x =>
            x.Id == seatMapId &&
            x.TenantId == _tenantContext.TenantId &&
            !x.IsDeleted &&
            (x.VehicleType == VehicleType.Bus || x.VehicleType == VehicleType.TourBus), ct);
}
