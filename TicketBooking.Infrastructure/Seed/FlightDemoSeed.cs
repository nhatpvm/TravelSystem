// FILE #093: TicketBooking.Infrastructure/Seed/FlightDemoSeed.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// Flight demo seed (Phase 10) - V3:
    /// - Seeds ONLY tenant VMM001 (TenantType.Flight).
    /// - Idempotent, safe with:
    ///   + Soft delete
    ///   + Tenant enforcement interceptor (never changes TenantId)
    ///   + Unique indexes (UPsert by natural keys)
    /// - Links flight.Airports -> catalog.Locations (LocationId) as required.
    ///
    /// Demo data:
    /// - Airports: SGN, HAN (with Locations)
    /// - Airline: Vietnam Airlines
    /// - AircraftModel: A320 + Aircraft
    /// - CabinSeatMap: A320 Economy 20x6 + CabinSeats (UPsert)
    /// - FareClasses: Y, C + FareRules
    /// - Flights: 2 flights (SGN->HAN, HAN->SGN) for next day
    /// - Offers: 2 offers (Y and C) for SGN->HAN + breakdown lines + segments
    /// - Ancillaries: baggage/meal demo
    /// </summary>
    public static class FlightDemoSeed
    {
        private const string TenantCode = "VMM001";

        // Avoid namespace conflict with "Flight"
        private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

        public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;

            // 0) Ensure tenant VMM001 exists
            var tenant = await EnsureTenantAsync(db, TenantCode, "Vé máy bay VMM001", TenantType.Flight, holdMinutes: 5, now, logger, ct);

            // 1) Locations (tenant-owned in your current design)
            var locSgn = await EnsureLocationAsync(
                db,
                tenant.Id,
                LocationType.Airport,
                "Sân bay Tân Sơn Nhất",
                "SGN",
                "Asia/Ho_Chi_Minh",
                airportIataCode: "SGN",
                airportIcaoCode: "VVTS",
                ct);
            var locHan = await EnsureLocationAsync(
                db,
                tenant.Id,
                LocationType.Airport,
                "Sân bay Nội Bài",
                "HAN",
                "Asia/Ho_Chi_Minh",
                airportIataCode: "HAN",
                airportIcaoCode: "VVNB",
                ct);

            // 2) Airports (link to Locations)
            var sgn = await EnsureAirportAsync(db, tenant.Id, locSgn.Id, code: "SGN", name: "Tân Sơn Nhất", iata: "SGN", icao: "VVTS", timeZone: "Asia/Ho_Chi_Minh", ct);
            var han = await EnsureAirportAsync(db, tenant.Id, locHan.Id, code: "HAN", name: "Nội Bài", iata: "HAN", icao: "VVNB", timeZone: "Asia/Ho_Chi_Minh", ct);

            // 3) Airline
            var airline = await EnsureAirlineAsync(db, tenant.Id, code: "VNA", name: "Vietnam Airlines", iata: "VN", icao: "HVN", ct);

            // 4) AircraftModel + Aircraft
            var a320 = await EnsureAircraftModelAsync(db, tenant.Id, code: "A320", manufacturer: "Airbus", model: "A320-200", typicalCapacity: 180, ct);
            var aircraft = await EnsureAircraftAsync(db, tenant.Id, aircraftModelId: a320.Id, airlineId: airline.Id, code: "VNA-A320-01", registration: "VN-A123", name: "Airbus A320 Demo", ct);

            // 5) CabinSeatMap + CabinSeats (UPsert to satisfy unique index CabinSeatMapId+SeatNumber)
            var ecoMap = await EnsureCabinSeatMapAsync(db, tenant.Id, a320.Id, CabinClass.Economy, code: "A320-ECO", name: "A320 Economy 20x6", rows: 20, cols: 6, seatLabelScheme: "ABCDEF", ct);
            await EnsureCabinSeatsForSeatMapAsync(db, tenant.Id, ecoMap.Id, rows: ecoMap.TotalRows, cols: ecoMap.TotalColumns, seatLabelScheme: ecoMap.SeatLabelScheme ?? "ABCDEF", ct);

            // 6) FareClasses + FareRules
            var fareY = await EnsureFareClassAsync(db, tenant.Id, airline.Id, code: "Y", name: "Economy Saver", CabinClass.Economy, refundable: false, changeable: true, ct);
            var fareC = await EnsureFareClassAsync(db, tenant.Id, airline.Id, code: "C", name: "Business Flex", CabinClass.Business, refundable: true, changeable: true, ct);

            await EnsureFareRuleAsync(db, tenant.Id, fareY.Id, rulesJson: "{\"refund\":false,\"change\":true,\"carryOn\":\"7kg\"}", ct);
            await EnsureFareRuleAsync(db, tenant.Id, fareC.Id, rulesJson: "{\"refund\":true,\"change\":true,\"carryOn\":\"10kg\",\"lounge\":true}", ct);

            // 7) Flights (next day, stable within the day)
            var d1 = now.Date.AddDays(1);

            var dep1 = new DateTimeOffset(d1.Year, d1.Month, d1.Day, 9, 0, 0, VnOffset);
            var arr1 = new DateTimeOffset(d1.Year, d1.Month, d1.Day, 11, 10, 0, VnOffset);
            var dep2 = new DateTimeOffset(d1.Year, d1.Month, d1.Day, 14, 0, 0, VnOffset);
            var arr2 = new DateTimeOffset(d1.Year, d1.Month, d1.Day, 16, 10, 0, VnOffset);

            var fl1 = await EnsureFlightAsync(db, tenant.Id, airline.Id, aircraft.Id, sgn.Id, han.Id, flightNumber: "VN201", dep1, arr1, ct);
            var fl2 = await EnsureFlightAsync(db, tenant.Id, airline.Id, aircraft.Id, han.Id, sgn.Id, flightNumber: "VN202", dep2, arr2, ct);

            // 8) Ancillaries
            await EnsureAncillaryAsync(db, tenant.Id, airline.Id, code: "BAG20", name: "Hành lý ký gửi 20kg", AncillaryType.Baggage, currency: "VND", price: 250_000m, rulesJson: "{\"kg\":20}", ct);
            await EnsureAncillaryAsync(db, tenant.Id, airline.Id, code: "MEAL01", name: "Suất ăn tiêu chuẩn", AncillaryType.Meal, currency: "VND", price: 80_000m, rulesJson: "{\"meal\":\"standard\"}", ct);

            // 9) Offers + OfferSegments + OfferTaxFeeLines (UPsert)
            // Offer TTL: 15 minutes
            var requestedAt = now;
            var expiresAt = now.AddMinutes(15);

            // Offer for VN201 - Economy (Y)
            var offerY = await EnsureOfferAsync(
                db, tenant.Id,
                airline.Id, fl1.Id, fareY.Id,
                status: OfferStatus.Active,
                currency: "VND",
                baseFare: 1_200_000m,
                taxesFees: 200_000m,
                total: 1_400_000m,
                seatsAvailable: 9,
                requestedAt, expiresAt,
                conditionsJson: "{\"fare\":\"Y\",\"refundable\":false,\"changeable\":true}",
                metadataJson: "{\"seed\":\"VMM001_VN201_Y\"}",
                ct);

            await EnsureOfferSegmentAsync(db, tenant.Id, offerY.Id, segmentIndex: 0, sgn.Id, han.Id, dep1, arr1, flightNumber: "VN201", ct);

            await EnsureOfferTaxFeeLineAsync(db, tenant.Id, offerY.Id, sortOrder: 0, lineType: TaxFeeLineType.BaseFare, code: "BASE", name: "Giá vé", currency: "VND", amount: 1_200_000m, ct);
            await EnsureOfferTaxFeeLineAsync(db, tenant.Id, offerY.Id, sortOrder: 1, lineType: TaxFeeLineType.Tax, code: "VAT", name: "Thuế", currency: "VND", amount: 120_000m, ct);
            await EnsureOfferTaxFeeLineAsync(db, tenant.Id, offerY.Id, sortOrder: 2, lineType: TaxFeeLineType.Fee, code: "AIRPORT", name: "Phí sân bay", currency: "VND", amount: 80_000m, ct);

            // Offer for VN201 - Business (C)
            var offerC = await EnsureOfferAsync(
                db, tenant.Id,
                airline.Id, fl1.Id, fareC.Id,
                status: OfferStatus.Active,
                currency: "VND",
                baseFare: 2_800_000m,
                taxesFees: 350_000m,
                total: 3_150_000m,
                seatsAvailable: 4,
                requestedAt, expiresAt,
                conditionsJson: "{\"fare\":\"C\",\"refundable\":true,\"changeable\":true}",
                metadataJson: "{\"seed\":\"VMM001_VN201_C\"}",
                ct);

            await EnsureOfferSegmentAsync(db, tenant.Id, offerC.Id, segmentIndex: 0, sgn.Id, han.Id, dep1, arr1, flightNumber: "VN201", ct);

            await EnsureOfferTaxFeeLineAsync(db, tenant.Id, offerC.Id, sortOrder: 0, lineType: TaxFeeLineType.BaseFare, code: "BASE", name: "Giá vé", currency: "VND", amount: 2_800_000m, ct);
            await EnsureOfferTaxFeeLineAsync(db, tenant.Id, offerC.Id, sortOrder: 1, lineType: TaxFeeLineType.Tax, code: "VAT", name: "Thuế", currency: "VND", amount: 280_000m, ct);
            await EnsureOfferTaxFeeLineAsync(db, tenant.Id, offerC.Id, sortOrder: 2, lineType: TaxFeeLineType.Fee, code: "AIRPORT", name: "Phí sân bay", currency: "VND", amount: 70_000m, ct);

            logger.LogInformation("Flight demo seed completed for tenant {TenantCode}.", TenantCode);
        }

        // --------------------------
        // Tenants
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
                logger.LogInformation("Created FLIGHT tenant {Code}.", code);
                return tenant;
            }

            var changed = false;
            if (tenant.IsDeleted) { tenant.IsDeleted = false; changed = true; }
            if (tenant.Name != name) { tenant.Name = name; changed = true; }
            if (tenant.Type != type) { tenant.Type = type; changed = true; }
            if (tenant.Status != TenantStatus.Active) { tenant.Status = TenantStatus.Active; changed = true; }
            if (tenant.HoldMinutes != holdMinutes) { tenant.HoldMinutes = holdMinutes; changed = true; }

            if (changed)
            {
                tenant.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Updated FLIGHT tenant {Code}.", code);
            }

            return tenant;
        }

        // --------------------------
        // Catalog: Locations
        // --------------------------

        private static async Task<Location> EnsureLocationAsync(
            AppDbContext db,
            Guid tenantId,
            LocationType type,
            string name,
            string code,
            string? timeZone,
            string? airportIataCode,
            string? airportIcaoCode,
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
                    AirportIataCode = airportIataCode,
                    AirportIcaoCode = airportIcaoCode,
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
            if (existing.AirportIataCode != airportIataCode) { existing.AirportIataCode = airportIataCode; changed = true; }
            if (existing.AirportIcaoCode != airportIcaoCode) { existing.AirportIcaoCode = airportIcaoCode; changed = true; }
            if (existing.TimeZone != timeZone) { existing.TimeZone = timeZone; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        // --------------------------
        // Flight: Airports / Airlines / Fleet
        // --------------------------

        private static async Task<Airport> EnsureAirportAsync(
            AppDbContext db,
            Guid tenantId,
            Guid locationId,
            string code,
            string name,
            string? iata,
            string? icao,
            string? timeZone,
            CancellationToken ct)
        {
            var existing = await db.FlightAirports.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var a = new Airport
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LocationId = locationId,
                    Code = code,
                    Name = name,
                    IataCode = iata,
                    IcaoCode = icao,
                    TimeZone = timeZone,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };
                db.FlightAirports.Add(a);
                await db.SaveChangesAsync(ct);
                return a;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.LocationId != locationId) { existing.LocationId = locationId; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.IataCode != iata) { existing.IataCode = iata; changed = true; }
            if (existing.IcaoCode != icao) { existing.IcaoCode = icao; changed = true; }
            if (existing.TimeZone != timeZone) { existing.TimeZone = timeZone; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task<Airline> EnsureAirlineAsync(
            AppDbContext db,
            Guid tenantId,
            string code,
            string name,
            string? iata,
            string? icao,
            CancellationToken ct)
        {
            var existing = await db.FlightAirlines.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var a = new Airline
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Code = code,
                    Name = name,
                    IataCode = iata,
                    IcaoCode = icao,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };
                db.FlightAirlines.Add(a);
                await db.SaveChangesAsync(ct);
                return a;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.IataCode != iata) { existing.IataCode = iata; changed = true; }
            if (existing.IcaoCode != icao) { existing.IcaoCode = icao; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task<AircraftModel> EnsureAircraftModelAsync(
            AppDbContext db,
            Guid tenantId,
            string code,
            string manufacturer,
            string model,
            int? typicalCapacity,
            CancellationToken ct)
        {
            var existing = await db.FlightAircraftModels.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var am = new AircraftModel
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Code = code,
                    Manufacturer = manufacturer,
                    Model = model,
                    TypicalSeatCapacity = typicalCapacity,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };
                db.FlightAircraftModels.Add(am);
                await db.SaveChangesAsync(ct);
                return am;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Manufacturer != manufacturer) { existing.Manufacturer = manufacturer; changed = true; }
            if (existing.Model != model) { existing.Model = model; changed = true; }
            if (existing.TypicalSeatCapacity != typicalCapacity) { existing.TypicalSeatCapacity = typicalCapacity; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task<Aircraft> EnsureAircraftAsync(
            AppDbContext db,
            Guid tenantId,
            Guid aircraftModelId,
            Guid airlineId,
            string code,
            string? registration,
            string? name,
            CancellationToken ct)
        {
            var existing = await db.FlightAircrafts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var ac = new Aircraft
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AircraftModelId = aircraftModelId,
                    AirlineId = airlineId,
                    Code = code,
                    Registration = registration,
                    Name = name,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };
                db.FlightAircrafts.Add(ac);
                await db.SaveChangesAsync(ct);
                return ac;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.AircraftModelId != aircraftModelId) { existing.AircraftModelId = aircraftModelId; changed = true; }
            if (existing.AirlineId != airlineId) { existing.AirlineId = airlineId; changed = true; }
            if (existing.Registration != registration) { existing.Registration = registration; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task<CabinSeatMap> EnsureCabinSeatMapAsync(
            AppDbContext db,
            Guid tenantId,
            Guid aircraftModelId,
            CabinClass cabinClass,
            string code,
            string name,
            int rows,
            int cols,
            string seatLabelScheme,
            CancellationToken ct)
        {
            var existing = await db.FlightCabinSeatMaps.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (existing is null)
            {
                var sm = new CabinSeatMap
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AircraftModelId = aircraftModelId,
                    CabinClass = cabinClass,
                    Code = code,
                    Name = name,
                    TotalRows = rows,
                    TotalColumns = cols,
                    SeatLabelScheme = seatLabelScheme,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };
                db.FlightCabinSeatMaps.Add(sm);
                await db.SaveChangesAsync(ct);
                return sm;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.AircraftModelId != aircraftModelId) { existing.AircraftModelId = aircraftModelId; changed = true; }
            if (existing.CabinClass != cabinClass) { existing.CabinClass = cabinClass; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.TotalRows != rows) { existing.TotalRows = rows; changed = true; }
            if (existing.TotalColumns != cols) { existing.TotalColumns = cols; changed = true; }
            if (existing.SeatLabelScheme != seatLabelScheme) { existing.SeatLabelScheme = seatLabelScheme; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        /// <summary>
        /// UPsert cabin seats by SeatNumber (cannot Remove+Insert because of unique index + soft delete).
        /// </summary>
        private static async Task EnsureCabinSeatsForSeatMapAsync(
            AppDbContext db,
            Guid tenantId,
            Guid cabinSeatMapId,
            int rows,
            int cols,
            string seatLabelScheme,
            CancellationToken ct)
        {
            seatLabelScheme = (seatLabelScheme ?? "ABCDEF").Trim();
            if (seatLabelScheme.Length < cols)
                throw new InvalidOperationException("SeatLabelScheme length must be >= TotalColumns.");

            var now = DateTimeOffset.Now;

            var existing = await db.FlightCabinSeats.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.CabinSeatMapId == cabinSeatMapId)
                .ToListAsync(ct);

            var bySeatNumber = existing
                .GroupBy(x => x.SeatNumber)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

            // Soft-delete duplicates if any
            foreach (var g in existing.GroupBy(x => x.SeatNumber))
            {
                var keep = g.OrderByDescending(x => x.CreatedAt).First();
                foreach (var extra in g.Where(x => x.Id != keep.Id))
                {
                    if (!extra.IsDeleted)
                    {
                        extra.IsDeleted = true;
                        extra.UpdatedAt = now;
                    }
                }
            }

            var touched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var letter = seatLabelScheme[c].ToString();
                    var seatNumber = $"{r + 1}{letter}";
                    touched.Add(seatNumber);

                    var isWindow = (c == 0 || c == cols - 1);
                    var isAisle = (cols >= 6) ? (c == 2 || c == 3) : (cols >= 4 && (c == 1 || c == cols - 2));

                    if (bySeatNumber.TryGetValue(seatNumber, out var seat))
                    {
                        var changed = false;
                        if (seat.RowIndex != r) { seat.RowIndex = r; changed = true; }
                        if (seat.ColumnIndex != c) { seat.ColumnIndex = c; changed = true; }
                        if (seat.IsWindow != isWindow) { seat.IsWindow = isWindow; changed = true; }
                        if (seat.IsAisle != isAisle) { seat.IsAisle = isAisle; changed = true; }
                        if (!seat.IsActive) { seat.IsActive = true; changed = true; }
                        if (seat.IsDeleted) { seat.IsDeleted = false; changed = true; }

                        if (changed) seat.UpdatedAt = now;
                    }
                    else
                    {
                        db.FlightCabinSeats.Add(new CabinSeat
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            CabinSeatMapId = cabinSeatMapId,
                            SeatNumber = seatNumber,
                            RowIndex = r,
                            ColumnIndex = c,
                            IsWindow = isWindow,
                            IsAisle = isAisle,
                            SeatClass = "Standard",
                            PriceModifier = 0m,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedAt = now
                        });
                    }
                }
            }

            // Soft-delete seats not in desired layout (future-proof)
            foreach (var seat in bySeatNumber.Values)
            {
                if (!touched.Contains(seat.SeatNumber) && !seat.IsDeleted)
                {
                    seat.IsDeleted = true;
                    seat.UpdatedAt = now;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task<FareClass> EnsureFareClassAsync(
            AppDbContext db,
            Guid tenantId,
            Guid airlineId,
            string code,
            string name,
            CabinClass cabinClass,
            bool refundable,
            bool changeable,
            CancellationToken ct)
        {
            var existing = await db.FlightFareClasses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.AirlineId == airlineId && x.Code == code, ct);

            if (existing is null)
            {
                var fc = new FareClass
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AirlineId = airlineId,
                    Code = code,
                    Name = name,
                    CabinClass = cabinClass,
                    IsRefundable = refundable,
                    IsChangeable = changeable,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };
                db.FlightFareClasses.Add(fc);
                await db.SaveChangesAsync(ct);
                return fc;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.CabinClass != cabinClass) { existing.CabinClass = cabinClass; changed = true; }
            if (existing.IsRefundable != refundable) { existing.IsRefundable = refundable; changed = true; }
            if (existing.IsChangeable != changeable) { existing.IsChangeable = changeable; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        private static async Task EnsureFareRuleAsync(
            AppDbContext db,
            Guid tenantId,
            Guid fareClassId,
            string rulesJson,
            CancellationToken ct)
        {
            var now = DateTimeOffset.Now;

            var existing = await db.FlightFareRules.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.FareClassId == fareClassId, ct);

            if (existing is null)
            {
                db.FlightFareRules.Add(new FareRule
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FareClassId = fareClassId,
                    RulesJson = string.IsNullOrWhiteSpace(rulesJson) ? "{}" : rulesJson,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now
                });
                await db.SaveChangesAsync(ct);
                return;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }
            if (existing.RulesJson != rulesJson) { existing.RulesJson = rulesJson; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task<TicketBooking.Domain.Flight.Flight> EnsureFlightAsync(
            AppDbContext db,
            Guid tenantId,
            Guid airlineId,
            Guid aircraftId,
            Guid fromAirportId,
            Guid toAirportId,
            string flightNumber,
            DateTimeOffset departureAt,
            DateTimeOffset arrivalAt,
            CancellationToken ct)
        {
            var existing = await db.FlightFlights.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.FlightNumber == flightNumber && x.DepartureAt == departureAt, ct);

            if (existing is null)
            {
                var f = new TicketBooking.Domain.Flight.Flight
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AirlineId = airlineId,
                    AircraftId = aircraftId,
                    FromAirportId = fromAirportId,
                    ToAirportId = toAirportId,
                    FlightNumber = flightNumber,
                    DepartureAt = departureAt,
                    ArrivalAt = arrivalAt,
                    Status = FlightStatus.Published,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.Now
                };
                db.FlightFlights.Add(f);
                await db.SaveChangesAsync(ct);
                return f;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.AirlineId != airlineId) { existing.AirlineId = airlineId; changed = true; }
            if (existing.AircraftId != aircraftId) { existing.AircraftId = aircraftId; changed = true; }
            if (existing.FromAirportId != fromAirportId) { existing.FromAirportId = fromAirportId; changed = true; }
            if (existing.ToAirportId != toAirportId) { existing.ToAirportId = toAirportId; changed = true; }
            if (existing.ArrivalAt != arrivalAt) { existing.ArrivalAt = arrivalAt; changed = true; }
            if (existing.Status != FlightStatus.Published) { existing.Status = FlightStatus.Published; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(ct);
            }

            return existing;
        }

        /// <summary>
        /// Offer: choose one "current" offer for (FlightId, FareClassId) and update it.
        /// Soft-delete extras (future-proof).
        /// </summary>
        private static async Task<Offer> EnsureOfferAsync(
            AppDbContext db,
            Guid tenantId,
            Guid airlineId,
            Guid flightId,
            Guid fareClassId,
            OfferStatus status,
            string currency,
            decimal baseFare,
            decimal taxesFees,
            decimal total,
            int seatsAvailable,
            DateTimeOffset requestedAt,
            DateTimeOffset expiresAt,
            string? conditionsJson,
            string? metadataJson,
            CancellationToken ct)
        {
            var now = DateTimeOffset.Now;
            currency = currency.ToUpperInvariant();

            var offers = await db.FlightOffers.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.FlightId == flightId && x.FareClassId == fareClassId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);

            var keep = offers.FirstOrDefault();
            if (keep is null)
            {
                keep = new Offer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AirlineId = airlineId,
                    FlightId = flightId,
                    FareClassId = fareClassId,
                    Status = status,
                    CurrencyCode = currency,
                    BaseFare = baseFare,
                    TaxesFees = taxesFees,
                    TotalPrice = total,
                    SeatsAvailable = seatsAvailable,
                    RequestedAt = requestedAt,
                    ExpiresAt = expiresAt,
                    ConditionsJson = conditionsJson,
                    MetadataJson = metadataJson,
                    IsDeleted = false,
                    CreatedAt = now
                };
                db.FlightOffers.Add(keep);
                await db.SaveChangesAsync(ct);
                return keep;
            }

            // Soft-delete extras
            foreach (var extra in offers.Skip(1))
            {
                if (!extra.IsDeleted)
                {
                    extra.IsDeleted = true;
                    extra.UpdatedAt = now;
                }
            }

            // Update keep
            var changed = false;
            if (keep.IsDeleted) { keep.IsDeleted = false; changed = true; }
            if (keep.AirlineId != airlineId) { keep.AirlineId = airlineId; changed = true; }
            if (keep.Status != status) { keep.Status = status; changed = true; }
            if (!string.Equals(keep.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) { keep.CurrencyCode = currency; changed = true; }
            if (keep.BaseFare != baseFare) { keep.BaseFare = baseFare; changed = true; }
            if (keep.TaxesFees != taxesFees) { keep.TaxesFees = taxesFees; changed = true; }
            if (keep.TotalPrice != total) { keep.TotalPrice = total; changed = true; }
            if (keep.SeatsAvailable != seatsAvailable) { keep.SeatsAvailable = seatsAvailable; changed = true; }
            if (keep.RequestedAt != requestedAt) { keep.RequestedAt = requestedAt; changed = true; }
            if (keep.ExpiresAt != expiresAt) { keep.ExpiresAt = expiresAt; changed = true; }
            if (keep.ConditionsJson != conditionsJson) { keep.ConditionsJson = conditionsJson; changed = true; }
            if (keep.MetadataJson != metadataJson) { keep.MetadataJson = metadataJson; changed = true; }

            if (changed)
            {
                keep.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);
            return keep;
        }

        private static async Task EnsureOfferSegmentAsync(
            AppDbContext db,
            Guid tenantId,
            Guid offerId,
            int segmentIndex,
            Guid fromAirportId,
            Guid toAirportId,
            DateTimeOffset departureAt,
            DateTimeOffset arrivalAt,
            string? flightNumber,
            CancellationToken ct)
        {
            var now = DateTimeOffset.Now;

            var existing = await db.FlightOfferSegments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OfferId == offerId && x.SegmentIndex == segmentIndex, ct);

            if (existing is null)
            {
                db.FlightOfferSegments.Add(new OfferSegment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OfferId = offerId,
                    SegmentIndex = segmentIndex,
                    FromAirportId = fromAirportId,
                    ToAirportId = toAirportId,
                    DepartureAt = departureAt,
                    ArrivalAt = arrivalAt,
                    FlightNumber = flightNumber,
                    IsDeleted = false,
                    CreatedAt = now
                });
                await db.SaveChangesAsync(ct);
                return;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.FromAirportId != fromAirportId) { existing.FromAirportId = fromAirportId; changed = true; }
            if (existing.ToAirportId != toAirportId) { existing.ToAirportId = toAirportId; changed = true; }
            if (existing.DepartureAt != departureAt) { existing.DepartureAt = departureAt; changed = true; }
            if (existing.ArrivalAt != arrivalAt) { existing.ArrivalAt = arrivalAt; changed = true; }
            if (existing.FlightNumber != flightNumber) { existing.FlightNumber = flightNumber; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// UPsert OfferTaxFeeLines by (OfferId, SortOrder, Code) because unique index exists and soft delete cannot be reinserted.
        /// </summary>
        private static async Task EnsureOfferTaxFeeLineAsync(
            AppDbContext db,
            Guid tenantId,
            Guid offerId,
            int sortOrder,
            TaxFeeLineType lineType,
            string code,
            string name,
            string currency,
            decimal amount,
            CancellationToken ct)
        {
            var now = DateTimeOffset.Now;
            currency = currency.ToUpperInvariant();

            var existing = await db.FlightOfferTaxFeeLines.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OfferId == offerId && x.SortOrder == sortOrder && x.Code == code, ct);

            if (existing is null)
            {
                db.FlightOfferTaxFeeLines.Add(new OfferTaxFeeLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OfferId = offerId,
                    SortOrder = sortOrder,
                    LineType = lineType,
                    Code = code,
                    Name = name,
                    CurrencyCode = currency,
                    Amount = amount,
                    IsDeleted = false,
                    CreatedAt = now
                });
                await db.SaveChangesAsync(ct);
                return;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.LineType != lineType) { existing.LineType = lineType; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (!string.Equals(existing.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) { existing.CurrencyCode = currency; changed = true; }
            if (existing.Amount != amount) { existing.Amount = amount; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task EnsureAncillaryAsync(
            AppDbContext db,
            Guid tenantId,
            Guid airlineId,
            string code,
            string name,
            AncillaryType type,
            string currency,
            decimal price,
            string? rulesJson,
            CancellationToken ct)
        {
            var now = DateTimeOffset.Now;
            currency = currency.ToUpperInvariant();

            var existing = await db.FlightAncillaryDefinitions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.AirlineId == airlineId && x.Code == code, ct);

            if (existing is null)
            {
                db.FlightAncillaryDefinitions.Add(new AncillaryDefinition
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AirlineId = airlineId,
                    Code = code,
                    Name = name,
                    Type = type,
                    CurrencyCode = currency,
                    Price = price,
                    RulesJson = rulesJson,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now
                });
                await db.SaveChangesAsync(ct);
                return;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.Type != type) { existing.Type = type; changed = true; }
            if (!string.Equals(existing.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) { existing.CurrencyCode = currency; changed = true; }
            if (existing.Price != price) { existing.Price = price; changed = true; }
            if (existing.RulesJson != rulesJson) { existing.RulesJson = rulesJson; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
            }
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
