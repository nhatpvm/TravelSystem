// FILE #083: TicketBooking.Api/Controllers/TrainCarsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/train/cars")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class TrainCarsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TrainCarsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] Guid? tripId = null,
        CancellationToken ct = default)
    {
        IQueryable<TrainCar> query = _db.TrainCars;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (tripId.HasValue && tripId.Value != Guid.Empty)
            query = query.Where(x => x.TripId == tripId.Value);

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.TripId,
                x.CarNumber,
                x.CarType,
                x.CabinClass,
                x.SortOrder,
                x.IsActive,
                x.IsDeleted
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        IQueryable<TrainCar> query = _db.TrainCars;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var car = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (car is null) return NotFound(new { message = "TrainCar not found." });

        var seats = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => x.CarId == id && !x.IsDeleted)
            .OrderBy(x => x.CompartmentIndex)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .Select(x => new
            {
                x.Id,
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
                x.IsActive
            })
            .ToListAsync(ct);

        return Ok(new { car, seats });
    }

    public sealed class UpsertTrainCarRequest
    {
        public Guid TripId { get; set; }
        public string CarNumber { get; set; } = "";
        public TrainCarType CarType { get; set; } = TrainCarType.SeatCoach;
        public string? CabinClass { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertTrainCarRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        ValidateCar(req);

        var tripOk = await _db.TrainTrips.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.TripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!tripOk) return BadRequest(new { message = "TripId is invalid for this tenant." });

        if (await HasActiveSeatOccupancyAsync(req.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        var exists = await _db.TrainCars.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == _tenantContext.TenantId && x.TripId == req.TripId && x.CarNumber == req.CarNumber.Trim(), ct);

        if (exists) return Conflict(new { message = "CarNumber already exists for this trip." });

        var entity = new TrainCar
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            TripId = req.TripId,
            CarNumber = req.CarNumber.Trim(),
            CarType = req.CarType,
            CabinClass = req.CabinClass?.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.TrainCars.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertTrainCarRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        ValidateCar(req);

        var entity = await _db.TrainCars.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "TrainCar not found in this tenant." });

        var tripOk = await _db.TrainTrips.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.TripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!tripOk) return BadRequest(new { message = "TripId is invalid for this tenant." });

        var affectedTripIds = new[] { entity.TripId, req.TripId }.Distinct().ToList();
        foreach (var tripId in affectedTripIds)
        {
            if (await HasActiveSeatOccupancyAsync(tripId, ct))
                return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });
        }

        var exists = await _db.TrainCars.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.TripId == req.TripId &&
                x.CarNumber == req.CarNumber.Trim() &&
                x.Id != id, ct);

        if (exists) return Conflict(new { message = "CarNumber already exists for this trip." });

        entity.TripId = req.TripId;
        entity.CarNumber = req.CarNumber.Trim();
        entity.CarType = req.CarType;
        entity.CabinClass = req.CabinClass?.Trim();
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    public sealed class GenerateSeatsRequest
    {
        public int Rows { get; set; } = 10;
        public int Columns { get; set; } = 4;
        public bool UseCompartments { get; set; } = false;
        public int CompartmentSize { get; set; } = 4; // K4
        public bool SleeperUpperLower { get; set; } = false; // for sleeper create Upper/Lower
        public string? SeatClass { get; set; } = null;
        public decimal? PriceModifier { get; set; } = null;
    }

    /// <summary>
    /// Replace all seats in a car with a generated layout.
    /// This keeps it simple for demo and later can be replaced by a real schema/seat map logic.
    /// </summary>
    [HttpPost("{carId:guid}/seats/generate")]
    public async Task<IActionResult> GenerateSeats(Guid carId, [FromBody] GenerateSeatsRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var car = await _db.TrainCars.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == carId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (car is null) return NotFound(new { message = "TrainCar not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(car.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        if (req.Rows <= 0 || req.Rows > 50) return BadRequest(new { message = "Rows must be 1..50" });
        if (req.Columns <= 0 || req.Columns > 10) return BadRequest(new { message = "Columns must be 1..10" });
        if (req.UseCompartments && (req.CompartmentSize < 2 || req.CompartmentSize > 8))
            return BadRequest(new { message = "CompartmentSize must be 2..8" });

        var now = DateTimeOffset.Now;
        var generated = TrainCarSeatGenerationSupport.BuildLayout(
            req.Rows,
            req.Columns,
            req.UseCompartments,
            req.CompartmentSize,
            req.SleeperUpperLower,
            req.SeatClass,
            req.PriceModifier);

        var existing = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => x.TenantId == _tenantContext.TenantId && x.CarId == carId)
            .ToListAsync(ct);

        var existingBySeatNumber = new Dictionary<string, TrainCarSeat>(StringComparer.OrdinalIgnoreCase);

        foreach (var g in existing.GroupBy(x => x.SeatNumber))
        {
            var keep = g.OrderByDescending(x => x.CreatedAt).First();
            existingBySeatNumber[keep.SeatNumber] = keep;

            foreach (var extra in g.Where(x => x.Id != keep.Id))
            {
                if (!extra.IsDeleted)
                {
                    extra.IsDeleted = true;
                    extra.UpdatedAt = now;
                }
            }
        }

        var touchedSeatNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var seat in generated)
        {
            touchedSeatNumbers.Add(seat.SeatNumber);

            if (existingBySeatNumber.TryGetValue(seat.SeatNumber, out var row))
            {
                row.SeatType = seat.SeatType;
                row.CompartmentCode = seat.CompartmentCode;
                row.CompartmentIndex = seat.CompartmentIndex;
                row.RowIndex = seat.RowIndex;
                row.ColumnIndex = seat.ColumnIndex;
                row.IsWindow = seat.IsWindow;
                row.IsAisle = seat.IsAisle;
                row.SeatClass = seat.SeatClass;
                row.PriceModifier = seat.PriceModifier;
                row.IsActive = true;
                row.IsDeleted = false;
                row.UpdatedAt = now;
            }
            else
            {
                _db.TrainCarSeats.Add(new TrainCarSeat
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId!.Value,
                    CarId = carId,
                    SeatNumber = seat.SeatNumber,
                    SeatType = seat.SeatType,
                    CompartmentCode = seat.CompartmentCode,
                    CompartmentIndex = seat.CompartmentIndex,
                    RowIndex = seat.RowIndex,
                    ColumnIndex = seat.ColumnIndex,
                    IsWindow = seat.IsWindow,
                    IsAisle = seat.IsAisle,
                    SeatClass = seat.SeatClass,
                    PriceModifier = seat.PriceModifier,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now
                });
            }
        }

        foreach (var row in existingBySeatNumber.Values)
        {
            if (!touchedSeatNumbers.Contains(row.SeatNumber) && !row.IsDeleted)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, count = generated.Count });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var entity = await _db.TrainCars.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (entity is null) return NotFound(new { message = "TrainCar not found." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        _db.TrainCars.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var entity = await _db.TrainCars.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "TrainCar not found." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private void RequireTenantWrite()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required for admin write requests.");
    }

    private static void ValidateCar(UpsertTrainCarRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.TripId == Guid.Empty) throw new InvalidOperationException("TripId is required.");
        if (string.IsNullOrWhiteSpace(req.CarNumber)) throw new InvalidOperationException("CarNumber is required.");
        if (req.CarNumber.Length > 20) throw new InvalidOperationException("CarNumber max length is 20.");
        if (req.CabinClass is not null && req.CabinClass.Length > 50) throw new InvalidOperationException("CabinClass max length is 50.");
    }

    private Task<bool> HasActiveSeatOccupancyAsync(Guid tripId, CancellationToken ct)
        => TrainSeatOccupancySupport.HasActiveSeatOccupancyAsync(
            _db,
            _tenantContext.TenantId!.Value,
            tripId,
            DateTimeOffset.Now,
            ct);
}
