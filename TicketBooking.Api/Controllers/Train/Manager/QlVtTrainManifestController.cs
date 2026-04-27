using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/trips")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainManifestController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainManifestController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("{tripId:guid}/manifest")]
    public async Task<IActionResult> GetManifest(
        Guid tripId,
        [FromQuery] Guid? carId = null,
        [FromQuery] string? format = null,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        var cars = await _db.TrainCars.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .Select(x => new { x.Id, x.CarNumber, x.CarType, x.CabinClass })
            .ToListAsync(ct);

        if (carId.HasValue && carId.Value != Guid.Empty && cars.All(x => x.Id != carId.Value))
            return BadRequest(new { message = "carId is invalid for this trip." });

        var carIds = carId.HasValue && carId.Value != Guid.Empty
            ? new List<Guid> { carId.Value }
            : cars.Select(x => x.Id).ToList();

        var seats = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && carIds.Contains(x.CarId) && !x.IsDeleted)
            .Select(x => new SeatLookup
            {
                Id = x.Id,
                CarId = x.CarId,
                SeatNumber = x.SeatNumber,
                SeatType = x.SeatType,
                SeatClass = x.SeatClass,
                CompartmentCode = x.CompartmentCode,
                CompartmentIndex = x.CompartmentIndex
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var seatIds = seats.Keys.ToList();
        var confirmedHolds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == tripId &&
                seatIds.Contains(x.TrainCarSeatId) &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Confirmed &&
                x.BookingId.HasValue)
            .OrderBy(x => x.FromStopIndex)
            .ThenBy(x => x.ToStopIndex)
            .ToListAsync(ct);

        var orderIds = confirmedHolds
            .Where(x => x.BookingId.HasValue)
            .Select(x => x.BookingId!.Value)
            .Distinct()
            .ToList();

        var orders = orderIds.Count == 0
            ? new Dictionary<Guid, OrderLookup>()
            : await _db.CustomerOrders.IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == tenantId &&
                    orderIds.Contains(x.Id) &&
                    x.ProductType == CustomerProductType.Train &&
                    !x.IsDeleted)
                .Select(x => new OrderLookup
                {
                    Id = x.Id,
                    OrderCode = x.OrderCode,
                    UserId = x.UserId,
                    ContactFullName = x.ContactFullName,
                    ContactPhone = x.ContactPhone,
                    ContactEmail = x.ContactEmail,
                    Status = x.Status,
                    PaymentStatus = x.PaymentStatus,
                    TicketStatus = x.TicketStatus,
                    MetadataJson = x.MetadataJson
                })
                .ToDictionaryAsync(x => x.Id, ct);

        var stopTimeIds = confirmedHolds
            .SelectMany(x => new[] { x.FromTripStopTimeId, x.ToTripStopTimeId })
            .Distinct()
            .ToList();

        var stopTimes = stopTimeIds.Count == 0
            ? new Dictionary<Guid, StopTimeLookup>()
            : await _db.TrainTripStopTimes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && stopTimeIds.Contains(x.Id) && !x.IsDeleted)
                .Join(
                    _db.TrainStopPoints.IgnoreQueryFilters().Where(sp => sp.TenantId == tenantId && !sp.IsDeleted),
                    st => st.StopPointId,
                    sp => sp.Id,
                    (st, sp) => new StopTimeLookup
                    {
                        Id = st.Id,
                        StopIndex = st.StopIndex,
                        StopName = sp.Name,
                        ArriveAt = st.ArriveAt,
                        DepartAt = st.DepartAt
                    })
                .ToDictionaryAsync(x => x.Id, ct);

        var carsById = cars.ToDictionary(x => x.Id);
        var rows = new List<ManifestRow>();

        foreach (var hold in confirmedHolds)
        {
            if (!seats.TryGetValue(hold.TrainCarSeatId, out var seat))
                continue;

            carsById.TryGetValue(seat.CarId, out var car);
            stopTimes.TryGetValue(hold.FromTripStopTimeId, out var fromStop);
            stopTimes.TryGetValue(hold.ToTripStopTimeId, out var toStop);
            orders.TryGetValue(hold.BookingId!.Value, out var order);

            var passenger = ResolvePassenger(order?.MetadataJson, hold.TrainCarSeatId, seat.SeatNumber);

            rows.Add(new ManifestRow
            {
                TripId = trip.Id,
                TripCode = trip.Code,
                TrainNumber = trip.TrainNumber,
                CarId = seat.CarId,
                CarNumber = car?.CarNumber ?? "",
                SeatId = seat.Id,
                SeatNumber = seat.SeatNumber,
                SeatType = seat.SeatType.ToString(),
                SeatClass = seat.SeatClass,
                CompartmentCode = seat.CompartmentCode,
                CompartmentIndex = seat.CompartmentIndex,
                FromStop = fromStop?.StopName ?? $"Stop {hold.FromStopIndex}",
                ToStop = toStop?.StopName ?? $"Stop {hold.ToStopIndex}",
                FromStopIndex = hold.FromStopIndex,
                ToStopIndex = hold.ToStopIndex,
                DepartureAt = fromStop?.DepartAt ?? fromStop?.ArriveAt,
                ArrivalAt = toStop?.ArriveAt ?? toStop?.DepartAt,
                OrderId = order?.Id,
                OrderCode = order?.OrderCode,
                OrderStatus = order?.Status.ToString(),
                PaymentStatus = order?.PaymentStatus.ToString(),
                TicketStatus = order?.TicketStatus.ToString(),
                PassengerName = passenger?.FullName ?? order?.ContactFullName ?? "",
                PassengerType = passenger?.PassengerType,
                IdNumber = passenger?.IdNumber,
                PassportNumber = passenger?.PassportNumber,
                PhoneNumber = passenger?.PhoneNumber ?? order?.ContactPhone,
                Email = passenger?.Email ?? order?.ContactEmail
            });
        }

        rows = rows
            .OrderBy(x => x.CarNumber)
            .ThenBy(x => x.CompartmentIndex ?? int.MaxValue)
            .ThenBy(x => x.SeatNumber)
            .ThenBy(x => x.FromStopIndex)
            .ToList();

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = BuildCsv(rows);
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            var fileName = $"train-manifest-{trip.Code}-{DateTimeOffset.Now:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        var summary = rows
            .GroupBy(x => new { x.CarId, x.CarNumber })
            .Select(g => new
            {
                g.Key.CarId,
                g.Key.CarNumber,
                passengerCount = g.Count(),
                fromStops = g.Select(x => x.FromStop).Distinct().OrderBy(x => x).ToList(),
                toStops = g.Select(x => x.ToStop).Distinct().OrderBy(x => x).ToList()
            })
            .ToList();

        return Ok(new
        {
            trip = new { trip.Id, trip.Code, trip.TrainNumber, trip.Name, trip.DepartureAt, trip.ArrivalAt, trip.Status },
            carId,
            passengerCount = rows.Count,
            cars = summary,
            items = rows
        });
    }

    private static CustomerPassengerInput? ResolvePassenger(string? metadataJson, Guid seatId, string seatNumber)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return null;

        try
        {
            var metadata = JsonSerializer.Deserialize<ManifestTrainOrderMetadata>(
                metadataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (metadata is null || metadata.Passengers.Count == 0)
                return null;

            var seatIndex = metadata.TrainCarSeatIds.FindIndex(x => x == seatId);
            if (seatIndex < 0 && metadata.SeatNumbers.Count > 0)
                seatIndex = metadata.SeatNumbers.FindIndex(x => string.Equals(x, seatNumber, StringComparison.OrdinalIgnoreCase));

            if (seatIndex >= 0 && seatIndex < metadata.Passengers.Count)
                return metadata.Passengers[seatIndex];

            return metadata.Passengers[0];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildCsv(IEnumerable<ManifestRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TripCode,TrainNumber,CarNumber,SeatNumber,SeatType,SeatClass,Compartment,FromStop,ToStop,DepartureAt,ArrivalAt,OrderCode,OrderStatus,PaymentStatus,TicketStatus,PassengerName,PassengerType,IdNumber,PassportNumber,PhoneNumber,Email");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(row.TripCode),
                Csv(row.TrainNumber),
                Csv(row.CarNumber),
                Csv(row.SeatNumber),
                Csv(row.SeatType),
                Csv(row.SeatClass),
                Csv(row.CompartmentCode),
                Csv(row.FromStop),
                Csv(row.ToStop),
                Csv(row.DepartureAt?.ToString("O")),
                Csv(row.ArrivalAt?.ToString("O")),
                Csv(row.OrderCode),
                Csv(row.OrderStatus),
                Csv(row.PaymentStatus),
                Csv(row.TicketStatus),
                Csv(row.PassengerName),
                Csv(row.PassengerType),
                Csv(row.IdNumber),
                Csv(row.PassportNumber),
                Csv(row.PhoneNumber),
                Csv(row.Email)
            }));
        }

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        var safe = value ?? "";
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private sealed class ManifestTrainOrderMetadata
    {
        public List<Guid> TrainCarSeatIds { get; set; } = new();
        public List<string> SeatNumbers { get; set; } = new();
        public List<CustomerPassengerInput> Passengers { get; set; } = new();
    }

    private sealed class SeatLookup
    {
        public Guid Id { get; init; }
        public Guid CarId { get; init; }
        public string SeatNumber { get; init; } = "";
        public TrainSeatType SeatType { get; init; }
        public string? SeatClass { get; init; }
        public string? CompartmentCode { get; init; }
        public int? CompartmentIndex { get; init; }
    }

    private sealed class StopTimeLookup
    {
        public Guid Id { get; init; }
        public int StopIndex { get; init; }
        public string StopName { get; init; } = "";
        public DateTimeOffset? ArriveAt { get; init; }
        public DateTimeOffset? DepartAt { get; init; }
    }

    private sealed class OrderLookup
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string OrderCode { get; init; } = "";
        public string ContactFullName { get; init; } = "";
        public string ContactPhone { get; init; } = "";
        public string ContactEmail { get; init; } = "";
        public CustomerOrderStatus Status { get; init; }
        public CustomerPaymentStatus PaymentStatus { get; init; }
        public CustomerTicketStatus TicketStatus { get; init; }
        public string? MetadataJson { get; init; }
    }

    private sealed class ManifestRow
    {
        public Guid TripId { get; init; }
        public string TripCode { get; init; } = "";
        public string TrainNumber { get; init; } = "";
        public Guid CarId { get; init; }
        public string CarNumber { get; init; } = "";
        public Guid SeatId { get; init; }
        public string SeatNumber { get; init; } = "";
        public string SeatType { get; init; } = "";
        public string? SeatClass { get; init; }
        public string? CompartmentCode { get; init; }
        public int? CompartmentIndex { get; init; }
        public string FromStop { get; init; } = "";
        public string ToStop { get; init; } = "";
        public int FromStopIndex { get; init; }
        public int ToStopIndex { get; init; }
        public DateTimeOffset? DepartureAt { get; init; }
        public DateTimeOffset? ArrivalAt { get; init; }
        public Guid? OrderId { get; init; }
        public string? OrderCode { get; init; }
        public string? OrderStatus { get; init; }
        public string? PaymentStatus { get; init; }
        public string? TicketStatus { get; init; }
        public string PassengerName { get; init; } = "";
        public string? PassengerType { get; init; }
        public string? IdNumber { get; init; }
        public string? PassportNumber { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Email { get; init; }
    }
}
