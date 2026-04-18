using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Train;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/car-seats")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainCarSeatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainCarSeatsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class UpsertTrainCarSeatRequest
    {
        public Guid CarId { get; set; }
        public string SeatNumber { get; set; } = "";
        public TrainSeatType SeatType { get; set; } = TrainSeatType.Seat;
        public string? CompartmentCode { get; set; }
        public int? CompartmentIndex { get; set; }
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public bool IsWindow { get; set; }
        public bool IsAisle { get; set; }
        public string? SeatClass { get; set; }
        public decimal? PriceModifier { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? carId,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : (pageSize > 200 ? 200 : pageSize);

        IQueryable<TrainCarSeat> query = _db.TrainCarSeats.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (carId.HasValue && carId.Value != Guid.Empty)
            query = query.Where(x => x.CarId == carId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.CarId)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.CarId,
                x.SeatNumber,
                x.SeatType,
                x.CompartmentCode,
                x.CompartmentIndex,
                x.RowIndex,
                x.ColumnIndex,
                x.IsWindow,
                x.IsAisle,
                x.SeatClass,
                x.PriceModifier,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<TrainCarSeat> query = _db.TrainCarSeats.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var seat = await query.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (seat is null)
            return NotFound(new { message = "TrainCarSeat not found in this tenant." });

        return Ok(new
        {
            seat.Id,
            seat.TenantId,
            seat.CarId,
            seat.SeatNumber,
            seat.SeatType,
            seat.CompartmentCode,
            seat.CompartmentIndex,
            seat.RowIndex,
            seat.ColumnIndex,
            seat.IsWindow,
            seat.IsAisle,
            seat.SeatClass,
            seat.PriceModifier,
            seat.IsActive,
            seat.IsDeleted,
            seat.CreatedAt,
            seat.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertTrainCarSeatRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateRequest(req);

        var tenantId = _tenantContext.TenantId!.Value;
        var seatNumber = req.SeatNumber.Trim();
        var car = await GetActiveCarAsync(req.CarId, tenantId, ct);
        if (car is null)
            return BadRequest(new { message = "CarId is invalid for this tenant." });

        if (await HasActiveSeatOccupancyAsync(car.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        var existing = await _db.TrainCarSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.CarId == req.CarId &&
                x.SeatNumber == seatNumber, ct);

        var now = DateTimeOffset.Now;

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                return Conflict(new { message = "SeatNumber already exists in this Car." });

            ApplySeat(existing, req, seatNumber, now);
            existing.IsDeleted = false;
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(Get), new { version = "1.0", id = existing.Id }, existing);
        }

        var entity = new TrainCarSeat
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CarId = req.CarId,
            SeatNumber = seatNumber,
            SeatType = req.SeatType,
            CompartmentCode = TrimOrNull(req.CompartmentCode, 20),
            CompartmentIndex = req.CompartmentIndex,
            RowIndex = req.RowIndex,
            ColumnIndex = req.ColumnIndex,
            IsWindow = req.IsWindow,
            IsAisle = req.IsAisle,
            SeatClass = TrimOrNull(req.SeatClass, 50),
            PriceModifier = req.PriceModifier,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = now
        };

        _db.TrainCarSeats.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { version = "1.0", id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertTrainCarSeatRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateRequest(req);

        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainCarSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TrainCarSeat not found in this tenant." });

        var targetCar = await GetActiveCarAsync(req.CarId, tenantId, ct);
        if (targetCar is null)
            return BadRequest(new { message = "CarId is invalid for this tenant." });

        var currentCar = await GetActiveCarAsync(entity.CarId, tenantId, ct);
        var affectedTripIds = new[] { currentCar?.TripId, targetCar.TripId }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        foreach (var tripId in affectedTripIds)
        {
            if (await HasActiveSeatOccupancyAsync(tripId, ct))
                return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });
        }

        var seatNumber = req.SeatNumber.Trim();
        var duplicate = await _db.TrainCarSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.CarId == req.CarId &&
                x.SeatNumber == seatNumber &&
                x.Id != id, ct);

        if (duplicate is not null)
        {
            return Conflict(new
            {
                message = duplicate.IsDeleted
                    ? "SeatNumber already exists in this Car as a deleted row. Restore or rename the existing seat first."
                    : "SeatNumber already exists in this Car."
            });
        }

        ApplySeat(entity, req, seatNumber, DateTimeOffset.Now);
        entity.IsDeleted = false;

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        Guid id,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainCarSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TrainCarSeat not found in this tenant." });

        var car = await GetActiveCarAsync(entity.CarId, tenantId, ct);
        if (car is not null && await HasActiveSeatOccupancyAsync(car.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

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
        Guid id,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainCarSeats.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TrainCarSeat not found in this tenant." });

        var car = await GetActiveCarAsync(entity.CarId, tenantId, ct);
        if (car is null)
            return BadRequest(new { message = "CarId is invalid for this tenant." });

        if (await HasActiveSeatOccupancyAsync(car.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        if (entity.IsDeleted)
        {
            var duplicate = await _db.TrainCarSeats.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.CarId == entity.CarId &&
                    x.SeatNumber == entity.SeatNumber &&
                    x.Id != id, ct);

            if (duplicate)
                return Conflict(new { message = "Cannot restore: another row already uses the same SeatNumber in this Car." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private static void ValidateRequest(UpsertTrainCarSeatRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.CarId == Guid.Empty) throw new InvalidOperationException("CarId is required.");
        if (string.IsNullOrWhiteSpace(req.SeatNumber)) throw new InvalidOperationException("SeatNumber is required.");
        if (req.SeatNumber.Trim().Length > 30) throw new InvalidOperationException("SeatNumber is too long (max 30).");
        if (req.CompartmentCode is not null && req.CompartmentCode.Trim().Length > 20) throw new InvalidOperationException("CompartmentCode max length is 20.");
        if (req.SeatClass is not null && req.SeatClass.Trim().Length > 50) throw new InvalidOperationException("SeatClass max length is 50.");
    }

    private static string? TrimOrNull(string? input, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();
        if (s.Length > maxLen) s = s[..maxLen];
        return s;
    }

    private static void ApplySeat(TrainCarSeat entity, UpsertTrainCarSeatRequest req, string seatNumber, DateTimeOffset now)
    {
        entity.CarId = req.CarId;
        entity.SeatNumber = seatNumber;
        entity.SeatType = req.SeatType;
        entity.CompartmentCode = TrimOrNull(req.CompartmentCode, 20);
        entity.CompartmentIndex = req.CompartmentIndex;
        entity.RowIndex = req.RowIndex;
        entity.ColumnIndex = req.ColumnIndex;
        entity.IsWindow = req.IsWindow;
        entity.IsAisle = req.IsAisle;
        entity.SeatClass = TrimOrNull(req.SeatClass, 50);
        entity.PriceModifier = req.PriceModifier;
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = now;
    }

    private Task<TrainCar?> GetActiveCarAsync(Guid carId, Guid tenantId, CancellationToken ct)
        => _db.TrainCars.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == carId && x.TenantId == tenantId && !x.IsDeleted, ct);

    private Task<bool> HasActiveSeatOccupancyAsync(Guid tripId, CancellationToken ct)
        => TrainSeatOccupancySupport.HasActiveSeatOccupancyAsync(
            _db,
            _tenantContext.TenantId!.Value,
            tripId,
            DateTimeOffset.Now,
            ct);
}
