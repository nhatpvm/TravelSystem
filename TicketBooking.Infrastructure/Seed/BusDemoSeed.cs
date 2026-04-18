// FILE #075 (FIX v3): TicketBooking.Infrastructure/Seed/BusDemoSeed.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Fleet;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// BUS demo seed (Level 3) - align with TenantsSeed:
    /// - Uses ONLY tenant NX001 (TenantType.Bus).
    /// - Seeds "2 routes + 2 trips + 2 vehicles + 2 seatmaps" under NX001 (gọn mà đủ demo).
    ///
    /// IMPORTANT FIXES:
    /// - RouteStops: UPSERT by StopIndex (no Remove()+Insert()) to avoid TenantId-change interceptor issues.
    /// - TripStopTimes: UPSERT by StopIndex (no Remove()+Insert()) to avoid TenantId-change interceptor issues.
    /// - TripSegmentPrices: UPSERT by (FromStopIndex,ToStopIndex) to avoid duplicate key on unique index.
    /// </summary>
    public static class BusDemoSeed
    {
        private const string TenantCode = "NX001";

        public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;

            // 0) Ensure tenant NX001 exists (Bus)
            var tenant = await EnsureTenantAsync(db, TenantCode, "Nhà xe NX001", TenantType.Bus, holdMinutes: 5, now, logger, ct);

            // 1) Locations (tenant-owned in current design)
            var hcm = await EnsureLocationAsync(db, tenant.Id, LocationType.City, "TP. Hồ Chí Minh", "HCM", "Asia/Ho_Chi_Minh", ct);
            var dalat = await EnsureLocationAsync(db, tenant.Id, LocationType.City, "Đà Lạt", "DLA", "Asia/Ho_Chi_Minh", ct);
            var nhatrang = await EnsureLocationAsync(db, tenant.Id, LocationType.City, "Nha Trang", "NTR", "Asia/Ho_Chi_Minh", ct);
            var restLoc = await EnsureLocationAsync(db, tenant.Id, LocationType.Other, "Trạm dừng chân Demo", "NX001-REST", "Asia/Ho_Chi_Minh", ct);

            // 2) Provider
            var provider = await EnsureProviderAsync(
                db,
                tenant.Id,
                ProviderType.Bus,
                code: "NX001-BUS",
                name: "Nhà Xe Minh Nhật",
                slug: "nha-xe-minh-nhat",
                locationId: hcm.Id,
                ct);

            // 3) SeatMaps (2) + Seats
            var sm1 = await EnsureSeatMapAsync(db, tenant.Id, VehicleType.Bus, "NX001-SM-01", "Bus 5x4 (Demo)", rows: 5, cols: 4, decks: 1, ct);
            var sm2 = await EnsureSeatMapAsync(db, tenant.Id, VehicleType.Bus, "NX001-SM-02", "Bus 6x4 (Demo)", rows: 6, cols: 4, decks: 1, ct);

            await EnsureSeatsForSeatMapAsync(db, tenant.Id, sm1, ct);
            await EnsureSeatsForSeatMapAsync(db, tenant.Id, sm2, ct);

            // 4) Vehicles (2)
            var v1 = await EnsureVehicleAsync(db, tenant.Id, VehicleType.Bus, provider.Id, sm1.Id, "NX001-V-01", "Xe demo 01", sm1.TotalRows * sm1.TotalColumns, "60A-999.99", ct);
            var v2 = await EnsureVehicleAsync(db, tenant.Id, VehicleType.Bus, provider.Id, sm2.Id, "NX001-V-02", "Xe demo 02", sm2.TotalRows * sm2.TotalColumns, "60A-888.88", ct);

            await EnsureBusVehicleDetailAsync(db, tenant.Id, v1.Id, ct);
            await EnsureBusVehicleDetailAsync(db, tenant.Id, v2.Id, ct);

            // 5) StopPoints linked to Locations
            var spHcm = await EnsureStopPointAsync(db, tenant.Id, hcm.Id, StopPointType.Terminal, "Bến xe TP.HCM", "TP.HCM", ct);
            var spDalat = await EnsureStopPointAsync(db, tenant.Id, dalat.Id, StopPointType.Terminal, "Bến xe Đà Lạt", "Đà Lạt", ct);
            var spNtr = await EnsureStopPointAsync(db, tenant.Id, nhatrang.Id, StopPointType.Terminal, "Bến xe Nha Trang", "Nha Trang", ct);
            var spRest = await EnsureStopPointAsync(db, tenant.Id, restLoc.Id, StopPointType.RestStop, "Trạm dừng chân Demo", "QL1A", ct);

            // 6) Routes (2) + RouteStops (3 stops each)
            var r1 = await EnsureBusRouteAsync(db, tenant.Id, provider.Id, "SGN-DLA", "TP.HCM → Đà Lạt", spHcm.Id, spDalat.Id, ct);
            await UpsertRouteStopsAsync(db, tenant.Id, r1.Id, new[]
            {
                (spHcm.Id, 0, 0),
                (spRest.Id, 1, 180),
                (spDalat.Id, 2, 360),
            }, ct);

            var r2 = await EnsureBusRouteAsync(db, tenant.Id, provider.Id, "SGN-NTR", "TP.HCM → Nha Trang", spHcm.Id, spNtr.Id, ct);
            await UpsertRouteStopsAsync(db, tenant.Id, r2.Id, new[]
            {
                (spHcm.Id, 0, 0),
                (spRest.Id, 1, 150),
                (spNtr.Id, 2, 300),
            }, ct);

            // 7) Trips (2) (published) - next 2 days
            var d1 = now.Date.AddDays(1);
            var d2 = now.Date.AddDays(2);

            var t1 = await EnsureTripAsync(db, tenant.Id, provider.Id, r1.Id, v1.Id,
                code: "NX001-TRIP-1",
                name: "Chuyến sáng (Demo)",
                departAt: new DateTimeOffset(d1.Year, d1.Month, d1.Day, 7, 0, 0, TimeSpan.FromHours(7)),
                arriveAt: new DateTimeOffset(d1.Year, d1.Month, d1.Day, 13, 0, 0, TimeSpan.FromHours(7)),
                ct);

            await GenerateTripStopTimesFromRouteAsync(db, tenant.Id, t1.Id, t1.RouteId, t1.DepartureAt, ct);
            await EnsureAllSegmentPricesAsync(db, tenant.Id, t1.Id, currency: "VND", baseFareStart: 220_000m, ct);

            var t2 = await EnsureTripAsync(db, tenant.Id, provider.Id, r2.Id, v2.Id,
                code: "NX001-TRIP-2",
                name: "Chuyến chiều (Demo)",
                departAt: new DateTimeOffset(d2.Year, d2.Month, d2.Day, 14, 0, 0, TimeSpan.FromHours(7)),
                arriveAt: new DateTimeOffset(d2.Year, d2.Month, d2.Day, 19, 0, 0, TimeSpan.FromHours(7)),
                ct);

            await GenerateTripStopTimesFromRouteAsync(db, tenant.Id, t2.Id, t2.RouteId, t2.DepartureAt, ct);
            await EnsureAllSegmentPricesAsync(db, tenant.Id, t2.Id, currency: "VND", baseFareStart: 180_000m, ct);

            logger.LogInformation("BUS demo seed completed for tenant {TenantCode}.", TenantCode);
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
                logger.LogInformation("Created BUS tenant {Code}.", code);
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
                logger.LogInformation("Updated BUS tenant {Code}.", code);
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
        // Fleet helpers
        // --------------------------

        private static async Task<SeatMap> EnsureSeatMapAsync(
            AppDbContext db,
            Guid tenantId,
            VehicleType vehicleType,
            string code,
            string name,
            int rows,
            int cols,
            int decks,
            CancellationToken ct)
        {
            var existing = await db.SeatMaps.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.VehicleType == vehicleType && x.Code == code, ct);

            if (existing is null)
            {
                var sm = new SeatMap
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VehicleType = vehicleType,
                    Code = code,
                    Name = name,
                    TotalRows = rows,
                    TotalColumns = cols,
                    DeckCount = decks,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.SeatMaps.Add(sm);
                await db.SaveChangesAsync(ct);
                return sm;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.TotalRows != rows) { existing.TotalRows = rows; changed = true; }
            if (existing.TotalColumns != cols) { existing.TotalColumns = cols; changed = true; }
            if (existing.DeckCount != decks) { existing.DeckCount = decks; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task EnsureSeatsForSeatMapAsync(AppDbContext db, Guid tenantId, SeatMap seatMap, CancellationToken ct)
        {
            var expected = seatMap.TotalRows * seatMap.TotalColumns * Math.Max(1, seatMap.DeckCount);

            var existingCount = await db.Seats.IgnoreQueryFilters()
                .CountAsync(x => x.TenantId == tenantId && x.SeatMapId == seatMap.Id && !x.IsDeleted, ct);

            if (existingCount == expected) return;

            var old = await db.Seats.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.SeatMapId == seatMap.Id)
                .ToListAsync(ct);

            foreach (var o in old) db.Seats.Remove(o);
            await db.SaveChangesAsync(ct);

            var now = DateTimeOffset.Now;
            var seats = new List<Seat>();

            for (var deck = 1; deck <= Math.Max(1, seatMap.DeckCount); deck++)
            {
                for (var r = 0; r < seatMap.TotalRows; r++)
                {
                    for (var c = 0; c < seatMap.TotalColumns; c++)
                    {
                        var seatNumber = $"{(r + 1):00}{(c + 1):00}";
                        if (seatMap.DeckCount > 1) seatNumber = $"{seatNumber}-D{deck}";

                        seats.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            SeatMapId = seatMap.Id,
                            SeatNumber = seatNumber,
                            RowIndex = r,
                            ColumnIndex = c,
                            DeckIndex = deck,
                            SeatType = SeatType.Standard,
                            SeatClass = SeatClass.Any,
                            IsWindow = (c == 0 || c == seatMap.TotalColumns - 1),
                            IsAisle = (seatMap.TotalColumns >= 3 && (c == 1 || c == seatMap.TotalColumns - 2)),
                            IsActive = true,
                            IsDeleted = false,
                            CreatedAt = now
                        });
                    }
                }
            }

            db.Seats.AddRange(seats);
            await db.SaveChangesAsync(ct);
        }

        private static async Task<Vehicle> EnsureVehicleAsync(
            AppDbContext db,
            Guid tenantId,
            VehicleType vehicleType,
            Guid providerId,
            Guid seatMapId,
            string code,
            string name,
            int seatCapacity,
            string? plate,
            CancellationToken ct)
        {
            var existing = await db.Vehicles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.VehicleType == vehicleType && x.Code == code, ct);

            if (existing is null)
            {
                var v = new Vehicle
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VehicleType = vehicleType,
                    ProviderId = providerId,
                    SeatMapId = seatMapId,
                    Code = code,
                    Name = name,
                    SeatCapacity = seatCapacity,
                    PlateNumber = plate,
                    Status = "Active",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.Vehicles.Add(v);
                await db.SaveChangesAsync(ct);
                return v;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.ProviderId != providerId) { existing.ProviderId = providerId; changed = true; }
            if (existing.SeatMapId != seatMapId) { existing.SeatMapId = seatMapId; changed = true; }
            if (existing.SeatCapacity != seatCapacity) { existing.SeatCapacity = seatCapacity; changed = true; }
            if (existing.PlateNumber != plate) { existing.PlateNumber = plate; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task EnsureBusVehicleDetailAsync(AppDbContext db, Guid tenantId, Guid vehicleId, CancellationToken ct)
        {
            var existing = await db.BusVehicleDetails.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.VehicleId == vehicleId, ct);

            if (existing is null)
            {
                db.BusVehicleDetails.Add(new BusVehicleDetail
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VehicleId = vehicleId,
                    BusType = "Giường nằm",
                    AmenitiesJson = "{\"wifi\":true,\"water\":true,\"ac\":true}",
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                });

                await db.SaveChangesAsync(ct);
                return;
            }

            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }
        }

        // --------------------------
        // Bus helpers
        // --------------------------

        private static async Task<StopPoint> EnsureStopPointAsync(
            AppDbContext db,
            Guid tenantId,
            Guid locationId,
            StopPointType type,
            string name,
            string? address,
            CancellationToken ct)
        {
            var existing = await db.BusStopPoints.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.LocationId == locationId && x.Type == type, ct);

            if (existing is null)
            {
                var sp = new StopPoint
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LocationId = locationId,
                    Type = type,
                    Name = name,
                    AddressLine = address,
                    SortOrder = 0,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.BusStopPoints.Add(sp);
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

        private static async Task<BusRoute> EnsureBusRouteAsync(
            AppDbContext db,
            Guid tenantId,
            Guid providerId,
            string code,
            string name,
            Guid fromStopPointId,
            Guid toStopPointId,
            CancellationToken ct)
        {
            var existing = await db.BusRoutes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var r = new BusRoute
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

                db.BusRoutes.Add(r);
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

        private static async Task UpsertRouteStopsAsync(
            AppDbContext db,
            Guid tenantId,
            Guid routeId,
            (Guid stopPointId, int stopIndex, int minutesFromStart)[] items,
            CancellationToken ct)
        {
            if (items is null || items.Length < 2)
                throw new InvalidOperationException("RouteStops must contain at least 2 items.");

            var now = DateTimeOffset.Now;

            var existing = await db.BusRouteStops.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.RouteId == routeId)
                .ToListAsync(ct);

            var byIndex = new Dictionary<int, RouteStop>();
            foreach (var g in existing.GroupBy(x => x.StopIndex))
            {
                var keep = g.OrderByDescending(x => x.CreatedAt).First();
                byIndex[g.Key] = keep;

                foreach (var extra in g.Where(x => x.Id != keep.Id))
                {
                    if (!extra.IsDeleted)
                    {
                        extra.IsDeleted = true;
                        extra.UpdatedAt = now;
                    }
                }
            }

            var ordered = items.OrderBy(x => x.stopIndex).ToArray();
            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].stopIndex != i)
                    throw new InvalidOperationException("StopIndex must be continuous from 0..n-1.");
                if (ordered[i].stopPointId == Guid.Empty)
                    throw new InvalidOperationException("StopPointId is required.");
            }

            var touched = new HashSet<int>();

            foreach (var it in ordered)
            {
                touched.Add(it.stopIndex);

                if (byIndex.TryGetValue(it.stopIndex, out var row))
                {
                    var changed = false;
                    if (row.StopPointId != it.stopPointId) { row.StopPointId = it.stopPointId; changed = true; }
                    if (row.MinutesFromStart != it.minutesFromStart) { row.MinutesFromStart = it.minutesFromStart; changed = true; }
                    if (!row.IsActive) { row.IsActive = true; changed = true; }
                    if (row.IsDeleted) { row.IsDeleted = false; changed = true; }
                    if (changed) row.UpdatedAt = now;
                }
                else
                {
                    db.BusRouteStops.Add(new RouteStop
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        RouteId = routeId,
                        StopPointId = it.stopPointId,
                        StopIndex = it.stopIndex,
                        MinutesFromStart = it.minutesFromStart,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now
                    });
                }
            }

            foreach (var row in byIndex.Values)
            {
                if (!touched.Contains(row.StopIndex) && !row.IsDeleted)
                {
                    row.IsDeleted = true;
                    row.UpdatedAt = now;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task<Trip> EnsureTripAsync(
            AppDbContext db,
            Guid tenantId,
            Guid providerId,
            Guid routeId,
            Guid vehicleId,
            string code,
            string name,
            DateTimeOffset departAt,
            DateTimeOffset arriveAt,
            CancellationToken ct)
        {
            var existing = await db.BusTrips.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var t = new Trip
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProviderId = providerId,
                    RouteId = routeId,
                    VehicleId = vehicleId,
                    Code = code,
                    Name = name,
                    Status = TripStatus.Published,
                    DepartureAt = departAt,
                    ArrivalAt = arriveAt,
                    FareRulesJson = "{\"change\":false,\"refund\":false}",
                    BaggagePolicyJson = "{\"carryOn\":\"7kg\"}",
                    BoardingPolicyJson = "{\"arriveBeforeMinutes\":30}",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };

                db.BusTrips.Add(t);
                await db.SaveChangesAsync(ct);
                return t;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.ProviderId != providerId) { existing.ProviderId = providerId; changed = true; }
            if (existing.RouteId != routeId) { existing.RouteId = routeId; changed = true; }
            if (existing.VehicleId != vehicleId) { existing.VehicleId = vehicleId; changed = true; }
            if (existing.DepartureAt != departAt) { existing.DepartureAt = departAt; changed = true; }
            if (existing.ArrivalAt != arriveAt) { existing.ArrivalAt = arriveAt; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }
            if (existing.Status != TripStatus.Published) { existing.Status = TripStatus.Published; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task GenerateTripStopTimesFromRouteAsync(
            AppDbContext db,
            Guid tenantId,
            Guid tripId,
            Guid routeId,
            DateTimeOffset departureAt,
            CancellationToken ct)
        {
            var routeStops = await db.BusRouteStops.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.RouteId == routeId && !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.StopIndex)
                .ToListAsync(ct);

            if (routeStops.Count < 2) return;

            var desired = routeStops.Select(rs =>
            {
                var t = rs.MinutesFromStart.HasValue ? departureAt.AddMinutes(rs.MinutesFromStart.Value) : (DateTimeOffset?)null;

                return new
                {
                    rs.StopIndex,
                    rs.StopPointId,
                    rs.MinutesFromStart,
                    ArriveAt = t,
                    DepartAt = t,
                    IsActive = rs.IsActive
                };
            }).ToList();

            // Last stop: no DepartAt
            desired[^1] = new
            {
                desired[^1].StopIndex,
                desired[^1].StopPointId,
                desired[^1].MinutesFromStart,
                desired[^1].ArriveAt,
                DepartAt = (DateTimeOffset?)null,
                desired[^1].IsActive
            };

            var now = DateTimeOffset.Now;

            var existing = await db.BusTripStopTimes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId)
                .ToListAsync(ct);

            var byIndex = new Dictionary<int, TripStopTime>();
            foreach (var g in existing.GroupBy(x => x.StopIndex))
            {
                var keep = g.OrderByDescending(x => x.CreatedAt).First();
                byIndex[g.Key] = keep;

                foreach (var extra in g.Where(x => x.Id != keep.Id))
                {
                    if (!extra.IsDeleted)
                    {
                        extra.IsDeleted = true;
                        extra.UpdatedAt = now;
                    }
                }
            }

            var touched = new HashSet<int>();

            foreach (var d in desired)
            {
                touched.Add(d.StopIndex);

                if (byIndex.TryGetValue(d.StopIndex, out var row))
                {
                    var changed = false;

                    if (row.StopPointId != d.StopPointId) { row.StopPointId = d.StopPointId; changed = true; }
                    if (row.MinutesFromStart != d.MinutesFromStart) { row.MinutesFromStart = d.MinutesFromStart; changed = true; }
                    if (row.ArriveAt != d.ArriveAt) { row.ArriveAt = d.ArriveAt; changed = true; }
                    if (row.DepartAt != d.DepartAt) { row.DepartAt = d.DepartAt; changed = true; }
                    if (row.IsActive != d.IsActive) { row.IsActive = d.IsActive; changed = true; }
                    if (row.IsDeleted) { row.IsDeleted = false; changed = true; }

                    if (changed) row.UpdatedAt = now;
                }
                else
                {
                    db.BusTripStopTimes.Add(new TripStopTime
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        TripId = tripId,
                        StopPointId = d.StopPointId,
                        StopIndex = d.StopIndex,
                        MinutesFromStart = d.MinutesFromStart,
                        ArriveAt = d.ArriveAt,
                        DepartAt = d.DepartAt,
                        IsActive = d.IsActive,
                        IsDeleted = false,
                        CreatedAt = now
                    });
                }
            }

            foreach (var row in byIndex.Values)
            {
                if (!touched.Contains(row.StopIndex) && !row.IsDeleted)
                {
                    row.IsDeleted = true;
                    row.UpdatedAt = now;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// FIX: UPSERT TripSegmentPrices by (FromStopIndex, ToStopIndex)
        /// => không còn lỗi duplicate key với unique index IX_TripSegmentPrices_TripId_FromStopIndex_ToStopIndex
        /// </summary>
        private static async Task EnsureAllSegmentPricesAsync(
            AppDbContext db,
            Guid tenantId,
            Guid tripId,
            string currency,
            decimal baseFareStart,
            CancellationToken ct)
        {
            var stopTimes = await db.BusTripStopTimes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.StopIndex)
                .ToListAsync(ct);

            if (stopTimes.Count < 2) return;

            var now = DateTimeOffset.Now;
            currency = currency.ToUpperInvariant();

            // Existing prices for this trip
            var existing = await db.BusTripSegmentPrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId)
                .ToListAsync(ct);

            // Dedup existing by (FromStopIndex, ToStopIndex) just in case
            var byKey = new Dictionary<(int from, int to), TripSegmentPrice>();
            foreach (var g in existing.GroupBy(x => (x.FromStopIndex, x.ToStopIndex)))
            {
                var keep = g.OrderByDescending(x => x.CreatedAt).First();
                byKey[(keep.FromStopIndex, keep.ToStopIndex)] = keep;

                foreach (var extra in g.Where(x => x.Id != keep.Id))
                {
                    if (!extra.IsDeleted)
                    {
                        extra.IsDeleted = true;
                        extra.UpdatedAt = now;
                    }
                }
            }

            var touched = new HashSet<(int from, int to)>();

            for (int i = 0; i < stopTimes.Count - 1; i++)
            {
                for (int j = i + 1; j < stopTimes.Count; j++)
                {
                    var from = stopTimes[i];
                    var to = stopTimes[j];

                    var key = (from.StopIndex, to.StopIndex);
                    touched.Add(key);

                    var baseFare = baseFareStart + ((j - i) * 50_000m);
                    var taxes = 0m;
                    var total = baseFare + taxes;

                    if (byKey.TryGetValue(key, out var row))
                    {
                        var changed = false;

                        // NEVER touch TenantId / TripId
                        if (row.FromTripStopTimeId != from.Id) { row.FromTripStopTimeId = from.Id; changed = true; }
                        if (row.ToTripStopTimeId != to.Id) { row.ToTripStopTimeId = to.Id; changed = true; }

                        if (!string.Equals(row.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) { row.CurrencyCode = currency; changed = true; }
                        if (row.BaseFare != baseFare) { row.BaseFare = baseFare; changed = true; }
                        if ((row.TaxesFees ?? 0m) != taxes) { row.TaxesFees = taxes; changed = true; }
                        if (row.TotalPrice != total) { row.TotalPrice = total; changed = true; }

                        if (!row.IsActive) { row.IsActive = true; changed = true; }
                        if (row.IsDeleted) { row.IsDeleted = false; changed = true; }

                        if (changed) row.UpdatedAt = now;
                    }
                    else
                    {
                        db.BusTripSegmentPrices.Add(new TripSegmentPrice
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            TripId = tripId,
                            FromTripStopTimeId = from.Id,
                            ToTripStopTimeId = to.Id,
                            FromStopIndex = from.StopIndex,
                            ToStopIndex = to.StopIndex,
                            CurrencyCode = currency,
                            BaseFare = baseFare,
                            TaxesFees = taxes,
                            TotalPrice = total,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedAt = now
                        });
                    }
                }
            }

            // Soft-delete prices not touched anymore (future-proof)
            foreach (var row in byKey.Values)
            {
                var key = (row.FromStopIndex, row.ToStopIndex);
                if (!touched.Contains(key) && !row.IsDeleted)
                {
                    row.IsDeleted = true;
                    row.UpdatedAt = now;
                }
            }

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