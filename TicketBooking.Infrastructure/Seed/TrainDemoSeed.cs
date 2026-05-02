// FILE #087: TicketBooking.Infrastructure/Seed/TrainDemoSeed.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Tenants;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// TRAIN demo seed (Level 3) - align with TenantsSeed:
    /// - Uses tenant VT001 (TenantType.Train).
    /// - Seeds: Locations, Providers(Train), train.StopPoints/Routes/RouteStops/Trips/TripStopTimes/TripSegmentPrices/TrainCars/TrainCarSeats.
    ///
    /// IMPORTANT:
    /// - Your interceptor enforces "tenant-owned insert must have TenantId in context".
    /// - So Program.cs must set TenantContext = VT001 before calling this seed (same as BusDemoSeed).
    /// </summary>
    public static class TrainDemoSeed
    {
        private const string TenantCode = "VT001";

        public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;

            // 0) Ensure tenant VT001 exists (Train)
            var tenant = await EnsureTenantAsync(db, TenantCode, "Vé tàu VT001", TenantType.Train, holdMinutes: 5, now, logger, ct);

            // 1) Locations (tenant-owned in current design)
            var hcm = await EnsureLocationAsync(db, tenant.Id, LocationType.City, "TP. Hồ Chí Minh", "HCM", "Asia/Ho_Chi_Minh", ct);
            var danang = await EnsureLocationAsync(db, tenant.Id, LocationType.City, "Đà Nẵng", "DAD", "Asia/Ho_Chi_Minh", ct);
            var hanoi = await EnsureLocationAsync(db, tenant.Id, LocationType.City, "Hà Nội", "HAN", "Asia/Ho_Chi_Minh", ct);

            // 2) Provider (Train)
            var provider = await EnsureProviderAsync(
                db,
                tenant.Id,
                ProviderType.Train,
                code: "VT001-TRAIN",
                name: "Đường sắt Việt Nam",
                slug: "duong-sat-viet-nam",
                locationId: hanoi.Id,
                ct);

            // 3) Train stop points linked to locations (ga)
            var spHcm = await EnsureTrainStopPointAsync(db, tenant.Id, hcm.Id, "Ga Sài Gòn", "TP.HCM", ct);
            var spDaNang = await EnsureTrainStopPointAsync(db, tenant.Id, danang.Id, "Ga Đà Nẵng", "Đà Nẵng", ct);
            var spHaNoi = await EnsureTrainStopPointAsync(db, tenant.Id, hanoi.Id, "Ga Hà Nội", "Hà Nội", ct);

            // 4) Routes (2) + RouteStops
            // Route 1: HCM -> Đà Nẵng -> Hà Nội
            var r1 = await EnsureTrainRouteAsync(db, tenant.Id, provider.Id, "SGN-DAD-HAN", "TP.HCM → Đà Nẵng → Hà Nội", spHcm.Id, spHaNoi.Id, ct);
            await ReplaceTrainRouteStopsAsync(db, tenant.Id, r1.Id, new[]
            {
                (spHcm.Id,   0, 0),
                (spDaNang.Id,1, 900),  // 15h
                (spHaNoi.Id, 2, 1800), // 30h
            }, ct);

            // Route 2: Hà Nội -> Đà Nẵng -> HCM
            var r2 = await EnsureTrainRouteAsync(db, tenant.Id, provider.Id, "HAN-DAD-SGN", "Hà Nội → Đà Nẵng → TP.HCM", spHaNoi.Id, spHcm.Id, ct);
            await ReplaceTrainRouteStopsAsync(db, tenant.Id, r2.Id, new[]
            {
                (spHaNoi.Id, 0, 0),
                (spDaNang.Id,1, 900),
                (spHcm.Id,   2, 1800),
            }, ct);

            // 5) Trips (2) + StopTimes + SegmentPrices + Cars + Seats
            var d1 = now.Date.AddDays(1);
            var d2 = now.Date.AddDays(2);

            var trip1 = await EnsureTrainTripAsync(db, tenant.Id, provider.Id, r1.Id,
                trainNumber: "SE1",
                code: "VT001-TRIP-1",
                name: "Chuyến SE1",
                departAt: new DateTimeOffset(d1.Year, d1.Month, d1.Day, 6, 0, 0, TimeSpan.FromHours(7)),
                arriveAt: new DateTimeOffset(d2.Year, d2.Month, d2.Day, 12, 0, 0, TimeSpan.FromHours(7)),
                ct);

            await GenerateTrainTripStopTimesFromRouteAsync(db, tenant.Id, trip1.Id, trip1.RouteId, trip1.DepartureAt, ct);
            await EnsureAllTrainSegmentPricesAsync(db, tenant.Id, trip1.Id, currency: "VND", baseFareStart: 450_000m, ct);

            // Cars + Seats (Trip1)
            var t1Car1 = await EnsureTrainCarAsync(db, tenant.Id, trip1.Id, "01", TrainCarType.SeatCoach, "Economy", sortOrder: 1, ct);
            var t1Car2 = await EnsureTrainCarAsync(db, tenant.Id, trip1.Id, "02", TrainCarType.Sleeper, "Sleeper", sortOrder: 2, ct);

            await EnsureSeatsForSeatCoachAsync(db, tenant.Id, t1Car1.Id, rows: 12, cols: 4, seatClass: "SoftSeat", ct);
            await EnsureSeatsForSleeperAsync(db, tenant.Id, t1Car2.Id, compartments: 6, berthPerCompartment: 4, seatClass: "K4", ct);

            var trip2 = await EnsureTrainTripAsync(db, tenant.Id, provider.Id, r2.Id,
                trainNumber: "SE2",
                code: "VT001-TRIP-2",
                name: "Chuyến SE2",
                departAt: new DateTimeOffset(d2.Year, d2.Month, d2.Day, 18, 0, 0, TimeSpan.FromHours(7)),
                arriveAt: new DateTimeOffset(d2.Year, d2.Month, d2.Day, 23, 30, 0, TimeSpan.FromHours(7)),
                ct);

            await GenerateTrainTripStopTimesFromRouteAsync(db, tenant.Id, trip2.Id, trip2.RouteId, trip2.DepartureAt, ct);
            await EnsureAllTrainSegmentPricesAsync(db, tenant.Id, trip2.Id, currency: "VND", baseFareStart: 420_000m, ct);

            // Cars + Seats (Trip2)
            var t2Car1 = await EnsureTrainCarAsync(db, tenant.Id, trip2.Id, "01", TrainCarType.SeatCoach, "Economy", sortOrder: 1, ct);
            var t2Car2 = await EnsureTrainCarAsync(db, tenant.Id, trip2.Id, "02", TrainCarType.Sleeper, "Sleeper", sortOrder: 2, ct);

            await EnsureSeatsForSeatCoachAsync(db, tenant.Id, t2Car1.Id, rows: 10, cols: 4, seatClass: "SoftSeat", ct);
            await EnsureSeatsForSleeperAsync(db, tenant.Id, t2Car2.Id, compartments: 5, berthPerCompartment: 4, seatClass: "K4", ct);

            logger.LogInformation("TRAIN demo seed completed for tenant {TenantCode}.", TenantCode);
        }

        // --------------------------
        // Tenants helper
        // --------------------------

        private static async Task<Tenant> EnsureTenantAsync(
            AppDbContext db,
            string code,
            string name,
            TenantType type,
            int holdMinutes,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Code == code, ct);

            if (tenant is null)
            {
                tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Type = type,
                    Status = TenantStatus.Active,
                    HoldMinutes = holdMinutes,
                    IsDeleted = false,
                    CreatedAt = now
                };

                db.Tenants.Add(tenant);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Created TRAIN tenant {Code}.", code);
                return tenant;
            }

            var changed = false;
            if (tenant.Name != name) { tenant.Name = name; changed = true; }
            if (tenant.Type != type) { tenant.Type = type; changed = true; }
            if (tenant.Status != TenantStatus.Active) { tenant.Status = TenantStatus.Active; changed = true; }
            if (tenant.HoldMinutes != holdMinutes) { tenant.HoldMinutes = holdMinutes; changed = true; }
            if (tenant.IsDeleted) { tenant.IsDeleted = false; changed = true; }

            if (changed)
            {
                tenant.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Updated TRAIN tenant {Code}.", code);
            }

            return tenant;
        }

        // --------------------------
        // Catalog helpers
        // --------------------------

        private static async Task<Location> EnsureLocationAsync(
            AppDbContext db,
            Guid tenantId,
            LocationType type,
            string name,
            string code,
            string? timeZone,
            CancellationToken ct)
        {
            var normalized = NormalizeSearch(name);

            var existing = await db.Locations.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var loc = new Location
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Type = type,
                    Name = name,
                    NormalizedName = normalized,
                    ShortName = name,
                    Code = code,
                    TimeZone = timeZone,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.Locations.Add(loc);
                await db.SaveChangesAsync(ct);
                return loc;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.NormalizedName != normalized) { existing.NormalizedName = normalized; changed = true; }
            if (existing.Type != type) { existing.Type = type; changed = true; }
            if (existing.TimeZone != timeZone) { existing.TimeZone = timeZone; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task<Provider> EnsureProviderAsync(
            AppDbContext db,
            Guid tenantId,
            ProviderType type,
            string code,
            string name,
            string slug,
            Guid? locationId,
            CancellationToken ct)
        {
            var existing = await db.Providers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var p = new Provider
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Type = type,
                    Code = code,
                    Name = name,
                    Slug = slug,
                    LocationId = locationId,
                    SupportPhone = "0900 000 000",
                    SupportEmail = "support@ticketbooking.local",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.Providers.Add(p);
                await db.SaveChangesAsync(ct);
                return p;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.Slug != slug) { existing.Slug = slug; changed = true; }
            if (existing.Type != type) { existing.Type = type; changed = true; }
            if (existing.LocationId != locationId) { existing.LocationId = locationId; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        // --------------------------
        // Train helpers
        // --------------------------

        private static async Task<TrainStopPoint> EnsureTrainStopPointAsync(
            AppDbContext db,
            Guid tenantId,
            Guid locationId,
            string name,
            string? address,
            CancellationToken ct)
        {
            var existing = await db.TrainStopPoints.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.LocationId == locationId && x.Type == TrainStopPointType.Station, ct);

            if (existing is null)
            {
                var sp = new TrainStopPoint
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LocationId = locationId,
                    Type = TrainStopPointType.Station,
                    Name = name,
                    AddressLine = address,
                    SortOrder = 0,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.TrainStopPoints.Add(sp);
                await db.SaveChangesAsync(ct);
                return sp;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.AddressLine != address) { existing.AddressLine = address; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task<TrainRoute> EnsureTrainRouteAsync(
            AppDbContext db,
            Guid tenantId,
            Guid providerId,
            string code,
            string name,
            Guid fromStopPointId,
            Guid toStopPointId,
            CancellationToken ct)
        {
            var existing = await db.TrainRoutes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var r = new TrainRoute
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProviderId = providerId,
                    Code = code,
                    Name = name,
                    FromStopPointId = fromStopPointId,
                    ToStopPointId = toStopPointId,
                    EstimatedMinutes = 0,
                    DistanceKm = 0,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.TrainRoutes.Add(r);
                await db.SaveChangesAsync(ct);
                return r;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.ProviderId != providerId) { existing.ProviderId = providerId; changed = true; }
            if (existing.FromStopPointId != fromStopPointId) { existing.FromStopPointId = fromStopPointId; changed = true; }
            if (existing.ToStopPointId != toStopPointId) { existing.ToStopPointId = toStopPointId; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }
        private static Guid StableGuid(Guid tenantId, string key)
        {
            // Stable GUID per (tenantId + key) for idempotent seeds
            // Using MD5 -> 16 bytes -> Guid
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{tenantId:D}|{key}");
            var hash = md5.ComputeHash(bytes); // 16 bytes
            return new Guid(hash);
        }

        private static async Task ReplaceTrainRouteStopsAsync(
            AppDbContext db,
            Guid tenantId,
            Guid routeId,
            (Guid stopPointId, int stopIndex, int minutesFromStart)[] items,
            CancellationToken ct)
        {
            // IMPORTANT: clear tracked entities that could belong to other tenants
            db.ChangeTracker.Clear();

            // Fast + safe delete without tracking
            await db.TrainRouteStops.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.RouteId == routeId)
                .ExecuteDeleteAsync(ct);

            var now = DateTimeOffset.Now;

            // IMPORTANT: make IDs stable per tenant+route to avoid duplicates across reruns
            var rows = items.Select(x => new TrainRouteStop
            {
                Id = StableGuid(tenantId, $"train-route-stop:{routeId}:{x.stopIndex}:{x.stopPointId}"),
                TenantId = tenantId,
                RouteId = routeId,
                StopPointId = x.stopPointId,
                StopIndex = x.stopIndex,
                MinutesFromStart = x.minutesFromStart,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now
            }).ToList();

            db.TrainRouteStops.AddRange(rows);
            await db.SaveChangesAsync(ct);

            db.ChangeTracker.Clear();
        }



        private static async Task<TrainTrip> EnsureTrainTripAsync(
            AppDbContext db,
            Guid tenantId,
            Guid providerId,
            Guid routeId,
            string trainNumber,
            string code,
            string name,
            DateTimeOffset departAt,
            DateTimeOffset arriveAt,
            CancellationToken ct)
        {
            var existing = await db.TrainTrips.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var t = new TrainTrip
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProviderId = providerId,
                    RouteId = routeId,
                    TrainNumber = trainNumber,
                    Code = code,
                    Name = name,
                    Status = TrainTripStatus.Published,
                    DepartureAt = departAt,
                    ArrivalAt = arriveAt,
                    FareRulesJson = "{\"change\":true,\"refund\":true}",
                    BaggagePolicyJson = "{\"carryOn\":\"10kg\"}",
                    BoardingPolicyJson = "{\"arriveBeforeMinutes\":30}",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.TrainTrips.Add(t);
                await db.SaveChangesAsync(ct);
                return t;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.TrainNumber != trainNumber) { existing.TrainNumber = trainNumber; changed = true; }
            if (existing.ProviderId != providerId) { existing.ProviderId = providerId; changed = true; }
            if (existing.RouteId != routeId) { existing.RouteId = routeId; changed = true; }
            if (existing.DepartureAt != departAt) { existing.DepartureAt = departAt; changed = true; }
            if (existing.ArrivalAt != arriveAt) { existing.ArrivalAt = arriveAt; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }
            if (existing.Status != TrainTripStatus.Published) { existing.Status = TrainTripStatus.Published; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task GenerateTrainTripStopTimesFromRouteAsync(
            AppDbContext db,
            Guid tenantId,
            Guid tripId,
            Guid routeId,
            DateTimeOffset departureAt,
            CancellationToken ct)
        {
            var routeStops = await db.TrainRouteStops.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.RouteId == routeId && !x.IsDeleted)
                .OrderBy(x => x.StopIndex)
                .ToListAsync(ct);

            if (routeStops.Count < 2) return;

            // ✅ 0) Delete active/cancelled/expired holds first because they FK to TripStopTimes.
            await db.TrainTripSeatHolds.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId)
                .ExecuteDeleteAsync(ct);

            // ✅ 1) Delete segment prices (they FK to TripStopTimes)
            await db.TrainTripSegmentPrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId)
                .ExecuteDeleteAsync(ct);

            // ✅ 2) Then delete stop-times
            await db.TrainTripStopTimes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId)
                .ExecuteDeleteAsync(ct);

            var now = DateTimeOffset.Now;

            var rows = routeStops.Select(rs =>
            {
                var t = rs.MinutesFromStart.HasValue
                    ? departureAt.AddMinutes(rs.MinutesFromStart.Value)
                    : (DateTimeOffset?)null;

                return new TrainTripStopTime
                {
                    Id = Guid.NewGuid(),

                    // ✅ IMPORTANT: do NOT set TenantId here (let interceptor set by TenantContext)
                    // TenantId = tenantId,

                    TripId = tripId,
                    StopPointId = rs.StopPointId,
                    StopIndex = rs.StopIndex,
                    MinutesFromStart = rs.MinutesFromStart,
                    ArriveAt = t,
                    DepartAt = t,
                    IsActive = rs.IsActive,
                    IsDeleted = false,
                    CreatedAt = now
                };
            }).ToList();

            rows[0].DepartAt = departureAt;
            if (rows[0].ArriveAt is null) rows[0].ArriveAt = departureAt;

            rows[^1].DepartAt = null;

            db.TrainTripStopTimes.AddRange(rows);
            await db.SaveChangesAsync(ct);

            var trip = await db.TrainTrips.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == tripId, ct);

            if (trip is not null)
            {
                trip.DepartureAt = departureAt;
                trip.ArrivalAt = rows[^1].ArriveAt ?? rows[^1].DepartAt ?? departureAt;
                trip.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task EnsureAllTrainSegmentPricesAsync(
            AppDbContext db,
            Guid tenantId,
            Guid tripId,
            string currency,
            decimal baseFareStart,
            CancellationToken ct)
        {
            var stopTimes = await db.TrainTripStopTimes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted)
                .OrderBy(x => x.StopIndex)
                .ToListAsync(ct);

            if (stopTimes.Count < 2) return;

            await db.TrainTripSegmentPrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId)
                .ExecuteDeleteAsync(ct);

            var now = DateTimeOffset.Now;
            var rows = new List<TrainTripSegmentPrice>();

            for (int i = 0; i < stopTimes.Count - 1; i++)
            {
                for (int j = i + 1; j < stopTimes.Count; j++)
                {
                    var from = stopTimes[i];
                    var to = stopTimes[j];

                    // simple scaling by segment distance (j-i)
                    var baseFare = baseFareStart + ((j - i) * 120_000m);
                    var taxes = 0m;
                    var total = baseFare + taxes;

                    rows.Add(new TrainTripSegmentPrice
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        TripId = tripId,
                        FromTripStopTimeId = from.Id,
                        ToTripStopTimeId = to.Id,
                        FromStopIndex = from.StopIndex,
                        ToStopIndex = to.StopIndex,
                        CurrencyCode = currency.ToUpperInvariant(),
                        BaseFare = baseFare,
                        TaxesFees = taxes,
                        TotalPrice = total,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now
                    });
                }
            }

            db.TrainTripSegmentPrices.AddRange(rows);
            await db.SaveChangesAsync(ct);
        }

        private static async Task<TrainCar> EnsureTrainCarAsync(
            AppDbContext db,
            Guid tenantId,
            Guid tripId,
            string carNumber,
            TrainCarType carType,
            string? cabinClass,
            int sortOrder,
            CancellationToken ct)
        {
            var existing = await db.TrainCars.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TripId == tripId && x.CarNumber == carNumber, ct);

            if (existing is null)
            {
                var car = new TrainCar
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TripId = tripId,
                    CarNumber = carNumber,
                    CarType = carType,
                    CabinClass = cabinClass,
                    SortOrder = sortOrder,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.TrainCars.Add(car);
                await db.SaveChangesAsync(ct);
                return car;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.CarType != carType) { existing.CarType = carType; changed = true; }
            if (existing.CabinClass != cabinClass) { existing.CabinClass = cabinClass; changed = true; }
            if (existing.SortOrder != sortOrder) { existing.SortOrder = sortOrder; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task EnsureSeatsForSeatCoachAsync(
            AppDbContext db,
            Guid tenantId,
            Guid carId,
            int rows,
            int cols,
            string? seatClass,
            CancellationToken ct)
        {
            var expected = rows * cols;

            var existingCount = await db.TrainCarSeats.IgnoreQueryFilters()
                .CountAsync(x => x.TenantId == tenantId && x.CarId == carId && !x.IsDeleted, ct);

            if (existingCount == expected) return;

            await db.TrainCarSeats.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.CarId == carId)
                .ExecuteDeleteAsync(ct);

            var now = DateTimeOffset.Now;
            var seats = new List<TrainCarSeat>();
            int seatCounter = 1;

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var isWindow = (c == 0 || c == cols - 1);
                    var isAisle = (cols >= 3 && (c == 1 || c == cols - 2));

                    seats.Add(new TrainCarSeat
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        CarId = carId,
                        SeatNumber = $"{seatCounter:000}",
                        SeatType = TrainSeatType.Seat,
                        CompartmentCode = null,
                        CompartmentIndex = null,
                        RowIndex = r,
                        ColumnIndex = c,
                        IsWindow = isWindow,
                        IsAisle = isAisle,
                        SeatClass = seatClass,
                        PriceModifier = null,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now
                    });

                    seatCounter++;
                }
            }

            db.TrainCarSeats.AddRange(seats);
            await db.SaveChangesAsync(ct);
        }

        private static async Task EnsureSeatsForSleeperAsync(
            AppDbContext db,
            Guid tenantId,
            Guid carId,
            int compartments,
            int berthPerCompartment,
            string? seatClass,
            CancellationToken ct)
        {
            // Each "position" has 2 berths (L/U) if berthPerCompartment is even we still store explicitly.
            // Here: create berthPerCompartment berths per compartment (e.g. 4) => 4 items like 01-L, 01-U, 02-L, 02-U ...
            var expected = compartments * berthPerCompartment;

            var existingCount = await db.TrainCarSeats.IgnoreQueryFilters()
                .CountAsync(x => x.TenantId == tenantId && x.CarId == carId && !x.IsDeleted, ct);

            if (existingCount == expected) return;

            await db.TrainCarSeats.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.CarId == carId)
                .ExecuteDeleteAsync(ct);

            var now = DateTimeOffset.Now;
            var seats = new List<TrainCarSeat>();

            int row = 0;
            int col = 0;

            for (int k = 1; k <= compartments; k++)
            {
                var compCode = $"K{berthPerCompartment}-{k:00}";

                // create pairs: 01-L,01-U,02-L,02-U ... until berthPerCompartment
                for (int i = 1; i <= berthPerCompartment; i++)
                {
                    var isUpper = (i % 2 == 0);
                    var seatNo = $"{k:00}-{(isUpper ? "U" : "L")}{i:00}";

                    seats.Add(new TrainCarSeat
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        CarId = carId,
                        SeatNumber = seatNo,
                        SeatType = isUpper ? TrainSeatType.UpperBerth : TrainSeatType.LowerBerth,
                        CompartmentCode = compCode,
                        CompartmentIndex = k,
                        RowIndex = row,
                        ColumnIndex = col,
                        IsWindow = (col == 0),
                        IsAisle = (col == 1),
                        SeatClass = seatClass,
                        PriceModifier = null,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now
                    });

                    col++;
                    if (col >= 2) { col = 0; row++; }
                }

                // gap row per compartment for readability
                row++;
            }

            db.TrainCarSeats.AddRange(seats);
            await db.SaveChangesAsync(ct);
        }

        // --------------------------
        // Utils
        // --------------------------

        private static string NormalizeSearch(string input)
        {
            input = input.Trim();
            if (input.Length == 0) return "";

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
            noDiacritics = noDiacritics.Replace('đ', 'd').Replace('Đ', 'D');

            var upper = noDiacritics.ToUpperInvariant();
            return string.Join(' ', upper.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
