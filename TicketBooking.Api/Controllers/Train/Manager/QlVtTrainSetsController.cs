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
[Route("api/v{version:apiVersion}/qlvt/train/train-sets")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainSetsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainSetsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class CreateFromTripRequest
    {
        public Guid TripId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public sealed class ApplyToTripRequest
    {
        public Guid TripId { get; set; }
        public bool ReplaceExistingCars { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;

        var items = await _db.TrainSets.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Status,
                x.Description,
                x.IsActive,
                carCount = _db.TrainSetCarTemplates.IgnoreQueryFilters().Count(c => c.TrainSetId == x.Id && !c.IsDeleted)
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        var trainSet = await _db.TrainSets.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (trainSet is null)
            return NotFound(new { message = "TrainSet not found." });

        var cars = await _db.TrainSetCarTemplates.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TrainSetId == id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .ToListAsync(ct);

        var carTemplateIds = cars.Select(x => x.Id).ToList();
        var seats = await _db.TrainSetSeatTemplates.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && carTemplateIds.Contains(x.TrainSetCarTemplateId) && !x.IsDeleted)
            .OrderBy(x => x.TrainSetCarTemplateId)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .ToListAsync(ct);

        return Ok(new { trainSet, cars, seats });
    }

    [HttpPost("from-trip")]
    public async Task<IActionResult> CreateFromTrip([FromBody] CreateFromTripRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        if (req.TripId == Guid.Empty) return BadRequest(new { message = "TripId is required." });
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest(new { message = "Code is required." });
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });

        var tripExists = await _db.TrainTrips.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (!tripExists) return NotFound(new { message = "TrainTrip not found in this tenant." });

        var code = req.Code.Trim().ToUpperInvariant();
        var exists = await _db.TrainSets.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);
        if (exists) return Conflict(new { message = "TrainSet code already exists." });

        var cars = await _db.TrainCars.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == req.TripId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .ToListAsync(ct);
        var carIds = cars.Select(x => x.Id).ToList();
        var seats = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && carIds.Contains(x.CarId) && !x.IsDeleted)
            .ToListAsync(ct);

        var now = DateTimeOffset.Now;
        var trainSet = new TrainSet
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = req.Name.Trim(),
            Description = TrimOrNull(req.Description, 500),
            Status = TrainSetStatus.Active,
            IsActive = true,
            CreatedAt = now
        };
        _db.TrainSets.Add(trainSet);

        var templateByCarId = new Dictionary<Guid, Guid>();
        foreach (var car in cars)
        {
            var templateId = Guid.NewGuid();
            templateByCarId[car.Id] = templateId;
            _db.TrainSetCarTemplates.Add(new TrainSetCarTemplate
            {
                Id = templateId,
                TenantId = tenantId,
                TrainSetId = trainSet.Id,
                CarNumber = car.CarNumber,
                CarType = car.CarType,
                CabinClass = car.CabinClass,
                SortOrder = car.SortOrder,
                IsActive = car.IsActive,
                CreatedAt = now
            });
        }

        foreach (var seat in seats)
        {
            _db.TrainSetSeatTemplates.Add(new TrainSetSeatTemplate
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TrainSetCarTemplateId = templateByCarId[seat.CarId],
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
                IsActive = seat.IsActive,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { trainSet.Id, carCount = cars.Count, seatCount = seats.Count });
    }

    [HttpPost("{id:guid}/apply")]
    public async Task<IActionResult> ApplyToTrip(Guid id, [FromBody] ApplyToTripRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        if (req.TripId == Guid.Empty) return BadRequest(new { message = "TripId is required." });

        var trainSet = await _db.TrainSets.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (trainSet is null) return NotFound(new { message = "TrainSet not found." });

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (trip is null) return NotFound(new { message = "TrainTrip not found in this tenant." });

        if (await TrainSeatOccupancySupport.HasActiveSeatOccupancyAsync(_db, tenantId, trip.Id, DateTimeOffset.Now, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        var now = DateTimeOffset.Now;
        if (req.ReplaceExistingCars)
        {
            var oldCars = await _db.TrainCars.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == trip.Id && !x.IsDeleted)
                .ToListAsync(ct);
            foreach (var car in oldCars)
            {
                car.CarNumber = $"{car.CarNumber}-old-{now:HHmmss}";
                car.UpdatedAt = now;
                _db.TrainCars.Remove(car);
            }
        }

        var templateCars = await _db.TrainSetCarTemplates.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TrainSetId == trainSet.Id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);
        var templateCarIds = templateCars.Select(x => x.Id).ToList();
        var templateSeats = await _db.TrainSetSeatTemplates.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && templateCarIds.Contains(x.TrainSetCarTemplateId) && !x.IsDeleted)
            .ToListAsync(ct);

        var newCarIdByTemplateId = new Dictionary<Guid, Guid>();
        foreach (var templateCar in templateCars)
        {
            var carId = Guid.NewGuid();
            newCarIdByTemplateId[templateCar.Id] = carId;
            _db.TrainCars.Add(new TrainCar
            {
                Id = carId,
                TenantId = tenantId,
                TripId = trip.Id,
                CarNumber = templateCar.CarNumber,
                CarType = templateCar.CarType,
                CabinClass = templateCar.CabinClass,
                SortOrder = templateCar.SortOrder,
                IsActive = templateCar.IsActive,
                CreatedAt = now
            });
        }

        foreach (var templateSeat in templateSeats)
        {
            _db.TrainCarSeats.Add(new TrainCarSeat
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CarId = newCarIdByTemplateId[templateSeat.TrainSetCarTemplateId],
                SeatNumber = templateSeat.SeatNumber,
                SeatType = templateSeat.SeatType,
                CompartmentCode = templateSeat.CompartmentCode,
                CompartmentIndex = templateSeat.CompartmentIndex,
                RowIndex = templateSeat.RowIndex,
                ColumnIndex = templateSeat.ColumnIndex,
                IsWindow = templateSeat.IsWindow,
                IsAisle = templateSeat.IsAisle,
                SeatClass = templateSeat.SeatClass,
                PriceModifier = templateSeat.PriceModifier,
                IsActive = templateSeat.IsActive,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, carCount = templateCars.Count, seatCount = templateSeats.Count });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
