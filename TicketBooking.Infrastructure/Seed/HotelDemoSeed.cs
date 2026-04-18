// FILE #164: TicketBooking.Infrastructure/Seed/HotelDemoSeed.cs
#nullable enable

using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed;

/// <summary>
/// Phase 11 - Hotel PRO demo seed (idempotent, multi-tenant safe).
///
/// Why this seed is "safe" with your AuditTenantSoftDeleteInterceptor:
/// - Before inserting ANY tenant-owned entity, we set tenant context (ITenantContext) via reflection.
/// - We also set TenantId on entities explicitly.
/// - We query tenants directly by SQL (no dependency on Tenant entity type).
///
/// What it seeds (minimal but coherent "PRO"):
/// - 1 Hotel per tenant
/// - Hotel amenities + links
/// - 2 RoomTypes per hotel + room amenities + beds + occupancy rules
/// - MealPlans + mapping
/// - CancellationPolicy + rules
/// - CheckInOutRule + PropertyPolicy
/// - 1 RatePlan (BAR) + RatePlanRoomTypes
/// - Inventory (RoomTypeInventory) + DailyRates for next N days
/// - ExtraServices + prices
/// - Add-on: HotelContact, RoomTypePolicy, PromoRateOverride (stub), HotelReview (stub)
///
/// Program.cs integration snippet is at the bottom of this file.
/// </summary>
public static class HotelDemoSeed
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("HotelDemoSeed");
        var cfg = sp.GetService<IConfiguration>();

        var db = sp.GetRequiredService<AppDbContext>();

        // Resolve ITenantContext WITHOUT hard dependency on its assembly/type name.
        var tenantContext = ResolveTenantContext(sp);

        var onlyTenantCodes = (cfg?["Seed:Demo:HotelTenantCodes"] ?? cfg?["Seed:HotelTenantCodes"])?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var daysToSeed = 14;
        if (int.TryParse(cfg?["Seed:Demo:HotelDays"], out var nDays) && nDays is >= 1 and <= 60)
            daysToSeed = nDays;

        var tenants = await LoadTenantsAsync(db, ct);
        if (tenants.Count == 0)
        {
            logger.LogWarning("No tenants found in [tenants].[Tenants]. Hotel demo seed skipped.");
            return;
        }

        var filtered = tenants;
        if (onlyTenantCodes is { Count: > 0 })
            filtered = tenants.Where(t => onlyTenantCodes.Contains(t.Code)).ToList();

        if (filtered.Count == 0)
        {
            logger.LogWarning("Hotel demo seed skipped: no tenants matched Seed:Demo:HotelTenantCodes.");
            return;
        }

        logger.LogInformation("Hotel demo seed starting for {Count} tenant(s). Days={Days}", filtered.Count, daysToSeed);

        foreach (var t in filtered)
        {
            ct.ThrowIfCancellationRequested();

            // IMPORTANT: set tenant context BEFORE adding tenant-owned entities.
            UseTenantContextOrThrow(tenantContext, t.Id, t.Code);

            await SeedForTenantAsync(db, t.Id, t.Code, daysToSeed, logger, ct);

            // keep change tracker clean between tenants
            db.ChangeTracker.Clear();
        }

        logger.LogInformation("Hotel demo seed finished.");
    }

    // =========================
    // Tenant loop
    // =========================

    private static async Task SeedForTenantAsync(
        AppDbContext db,
        Guid tenantId,
        string tenantCode,
        int daysToSeed,
        ILogger logger,
        CancellationToken ct)
    {
        var now = DateTimeOffset.Now;

        var hotelCode = $"HTL_{tenantCode}_DEMO_01";
        var hotelId = StableGuid(tenantId, $"hotel:{hotelCode}");

        var existingHotel = await db.Set<Hotel>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == hotelCode, ct);

        hotelId = existingHotel?.Id ?? hotelId;


        if (existingHotel is not null)
        {
            logger.LogInformation("Tenant {TenantCode}: Hotel {HotelCode} already exists. Skipping core hotel create.", tenantCode, hotelCode);
            existingHotel.Name = $"Demo Hotel {tenantCode}";
            existingHotel.Slug = $"demo-hotel-{tenantCode.ToLowerInvariant()}";
            existingHotel.TimeZone = "Asia/Ho_Chi_Minh";
            existingHotel.CountryCode = "VN";
            existingHotel.Province = "Đồng Nai";
            existingHotel.City = "Biên Hòa";
            existingHotel.AddressLine = "KCN (Demo) - Đường số 1";
            existingHotel.ShortDescription = "Khách sạn demo chuẩn PRO cho hệ thống TicketBooking.";
            existingHotel.DescriptionMarkdown = "## Demo Hotel\n\nNội dung demo cho Phase 11 (Hotel PRO).";
            existingHotel.DescriptionHtml = "<h2>Demo Hotel</h2><p>Nội dung demo cho Phase 11 (Hotel PRO).</p>";
            existingHotel.Phone = "0900-000-000";
            existingHotel.Email = $"hotel-{tenantCode.ToLowerInvariant()}@demo.local";
            existingHotel.CoverImageUrl = "https://picsum.photos/seed/hotel/1200/600";
            existingHotel.DefaultCheckInTime = new TimeOnly(14, 0);
            existingHotel.DefaultCheckOutTime = new TimeOnly(12, 0);
            existingHotel.Status = HotelStatus.Active;
            existingHotel.IsActive = true;
            existingHotel.IsDeleted = false;
            existingHotel.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
        }

        // ---------- dictionaries ----------
        var wifiAmenity = await GetOrCreateHotelAmenityAsync(db, tenantId, "WIFI", "Wi-Fi miễn phí", sort: 10, ct);
        var parkingAmenity = await GetOrCreateHotelAmenityAsync(db, tenantId, "PARKING", "Bãi đậu xe", sort: 20, ct);
        var poolAmenity = await GetOrCreateHotelAmenityAsync(db, tenantId, "POOL", "Hồ bơi", sort: 30, ct);

        var acAmenity = await GetOrCreateRoomAmenityAsync(db, tenantId, "AC", "Máy lạnh", sort: 10, ct);
        var tvAmenity = await GetOrCreateRoomAmenityAsync(db, tenantId, "TV", "TV", sort: 20, ct);
        var minibarAmenity = await GetOrCreateRoomAmenityAsync(db, tenantId, "MINIBAR", "Minibar", sort: 30, ct);

        var bedQueen = await GetOrCreateBedTypeAsync(db, tenantId, "QUEEN", "Giường Queen", ct);
        var bedTwin = await GetOrCreateBedTypeAsync(db, tenantId, "TWIN", "2 giường đơn", ct);

        var mpRO = await GetOrCreateMealPlanAsync(db, tenantId, "RO", "Không bao gồm bữa sáng", ct);
        var mpBB = await GetOrCreateMealPlanAsync(db, tenantId, "BB", "Bao gồm bữa sáng", ct);


        // ---------- hotel ----------
        if (existingHotel is null)
        {
            var h = new Hotel
            {
                Id = hotelId,
                TenantId = tenantId,
                Code = hotelCode,
                Name = $"Demo Hotel {tenantCode}",
                Slug = $"demo-hotel-{tenantCode.ToLowerInvariant()}",
                TimeZone = "Asia/Ho_Chi_Minh",
                CountryCode = "VN",
                Province = "Đồng Nai",
                City = "Biên Hòa",
                AddressLine = "KCN (Demo) - Đường số 1",
                ShortDescription = "Khách sạn demo chuẩn PRO cho hệ thống TicketBooking.",
                DescriptionMarkdown = "## Demo Hotel\n\nNội dung demo cho Phase 11 (Hotel PRO).",
                DescriptionHtml = "<h2>Demo Hotel</h2><p>Nội dung demo cho Phase 11 (Hotel PRO).</p>",
                Phone = "0900-000-000",
                Email = $"hotel-{tenantCode.ToLowerInvariant()}@demo.local",
                WebsiteUrl = null,
                CoverImageUrl = "https://picsum.photos/seed/hotel/1200/600",
                Status = HotelStatus.Active,
                DefaultCheckInTime = new TimeOnly(14, 0),
                DefaultCheckOutTime = new TimeOnly(12, 0),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Add(h);
            await db.SaveChangesAsync(ct);

            existingHotel = h;

            // Link amenities
            await EnsureHotelAmenityLinkAsync(db, tenantId, h.Id, wifiAmenity.Id, ct);
            await EnsureHotelAmenityLinkAsync(db, tenantId, h.Id, parkingAmenity.Id, ct);
            await EnsureHotelAmenityLinkAsync(db, tenantId, h.Id, poolAmenity.Id, ct);

            // 1 cover image row
            await EnsureHotelImageAsync(db, tenantId, h.Id, ct);

            // Add-on: primary contact
            await EnsureHotelContactAsync(db, tenantId, h.Id, ct);


            await db.SaveChangesAsync(ct);

            logger.LogInformation("Tenant {TenantCode}: Created Hotel {HotelCode}", tenantCode, hotelCode);
        }
        var cancellationPolicy = await GetOrCreateCancellationPolicyAsync(db, tenantId, hotelId, ct);
        await EnsureCancellationPolicyRulesAsync(db, tenantId, cancellationPolicy.Id, ct);
        var checkInOutRule = await GetOrCreateCheckInOutRuleAsync(db, tenantId, hotelId, ct);
        var propertyPolicy = await GetOrCreatePropertyPolicyAsync(db, tenantId, hotelId, ct);

        var hotel = existingHotel!;

        // ---------- room types ----------
        var rtStdCode = "STD_KING";
        var rtStd = await GetOrCreateRoomTypeAsync(
            db,
            tenantId,
            hotel.Id,
            rtStdCode,
            "Standard King",
            sort: 10,
            areaSquareMeters: 28,
            defaultAdults: 2,
            defaultChildren: 0,
            maxAdults: 2,
            maxChildren: 1,
            maxGuests: 3,
            totalUnits: 10,
            ct);

        var rtDlxCode = "DLX_TWIN";
        var rtDlx = await GetOrCreateRoomTypeAsync(
            db,
            tenantId,
            hotel.Id,
            rtDlxCode,
            "Deluxe Twin",
            sort: 20,
            areaSquareMeters: 36,
            defaultAdults: 2,
            defaultChildren: 1,
            maxAdults: 3,
            maxChildren: 2,
            maxGuests: 4,
            totalUnits: 6,
            ct);

        // Beds
        await EnsureRoomTypeBedAsync(db, tenantId, rtStd.Id, bedQueen.Id, count: 1, ct);
        await EnsureRoomTypeBedAsync(db, tenantId, rtDlx.Id, bedTwin.Id, count: 2, ct);

        // Room amenities
        await EnsureRoomAmenityLinkAsync(db, tenantId, rtStd.Id, acAmenity.Id, ct);
        await EnsureRoomAmenityLinkAsync(db, tenantId, rtStd.Id, tvAmenity.Id, ct);

        await EnsureRoomAmenityLinkAsync(db, tenantId, rtDlx.Id, acAmenity.Id, ct);
        await EnsureRoomAmenityLinkAsync(db, tenantId, rtDlx.Id, tvAmenity.Id, ct);
        await EnsureRoomAmenityLinkAsync(db, tenantId, rtDlx.Id, minibarAmenity.Id, ct);

        // Occupancy rules (simple)
        await EnsureOccupancyRuleAsync(
            db,
            tenantId,
            rtStd.Id,
            minAdults: 1,
            maxAdults: 2,
            minChildren: 0,
            maxChildren: 1,
            minGuests: 1,
            maxGuests: 3,
            maxInfants: 1,
            ct);
        await EnsureOccupancyRuleAsync(
            db,
            tenantId,
            rtDlx.Id,
            minAdults: 1,
            maxAdults: 3,
            minChildren: 0,
            maxChildren: 2,
            minGuests: 1,
            maxGuests: 4,
            maxInfants: 1,
            ct);

        // Meal plan mapping
        await EnsureRoomTypeMealPlanAsync(db, tenantId, rtStd.Id, mpRO.Id, ct);
        await EnsureRoomTypeMealPlanAsync(db, tenantId, rtStd.Id, mpBB.Id, ct);

        await EnsureRoomTypeMealPlanAsync(db, tenantId, rtDlx.Id, mpRO.Id, ct);
        await EnsureRoomTypeMealPlanAsync(db, tenantId, rtDlx.Id, mpBB.Id, ct);

        // Room type policy (add-on)
        await EnsureRoomTypePolicyAsync(db, tenantId, rtStd.Id, ct);
        await EnsureRoomTypePolicyAsync(db, tenantId, rtDlx.Id, ct);

        await db.SaveChangesAsync(ct);

        // ---------- rate plan ----------
        var rpCode = "BAR";
        var ratePlan = await GetOrCreateRatePlanAsync(db, tenantId, hotel.Id, rpCode, "Best Available Rate", cancellationPolicy.Id, checkInOutRule.Id, propertyPolicy.Id, ct);

        // Map rate plan to room types
        var rpRtStd = await GetOrCreateRatePlanRoomTypeAsync(db, tenantId, ratePlan.Id, rtStd.Id, currencyCode: "VND", basePrice: 800_000m, ct);
        var rpRtDlx = await GetOrCreateRatePlanRoomTypeAsync(db, tenantId, ratePlan.Id, rtDlx.Id, currencyCode: "VND", basePrice: 1_150_000m, ct);

        // Rate plan policy json
        await EnsureRatePlanPolicyAsync(db, tenantId, ratePlan.Id, ct);

        await db.SaveChangesAsync(ct);

        // ---------- inventory + rates ----------
        var start = DateOnly.FromDateTime(DateTime.Now.Date);
        for (var i = 0; i < daysToSeed; i++)
        {
            ct.ThrowIfCancellationRequested();

            var d = start.AddDays(i);

            await EnsureInventoryAsync(db, tenantId, rtStd.Id, d, total: 10, stopSell: 0, ct);
            await EnsureInventoryAsync(db, tenantId, rtDlx.Id, d, total: 6, stopSell: 0, ct);

            // small weekday/weekend variation
            var dow = d.DayOfWeek;
            var weekendFactor = (dow is DayOfWeek.Friday or DayOfWeek.Saturday) ? 1.15m : 1.00m;

            await EnsureDailyRateAsync(db, tenantId, rpRtStd.Id, d, currency: "VND", price: RoundToThousand(800_000m * weekendFactor), ct);
            await EnsureDailyRateAsync(db, tenantId, rpRtDlx.Id, d, currency: "VND", price: RoundToThousand(1_150_000m * weekendFactor), ct);
        }

        // ---------- extra services ----------
        var svcAirport = await GetOrCreateExtraServiceAsync(db, tenantId, hotel.Id, "AIRPORT_PICKUP", "Đưa đón sân bay", ct);
        await EnsureExtraServicePriceAsync(db, tenantId, svcAirport.Id, start, start.AddDays(daysToSeed - 1), "VND", 250_000m, ct);

        // ---------- add-on stubs ----------
        await EnsurePromoRateOverrideStubAsync(db, tenantId, rpRtStd.Id, start, start.AddDays(daysToSeed - 1), ct);
        await EnsureHotelReviewStubAsync(db, tenantId, hotel.Id, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Tenant {TenantCode}: Hotel seed OK ({HotelCode})", tenantCode, hotelCode);
    }

    // =========================
    // Get/Create helpers
    // =========================

    private static async Task<HotelAmenity> GetOrCreateHotelAmenityAsync(AppDbContext db, Guid tenantId, string code, string name, int sort, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"hotelAmenity:{code}");
        var existing = await db.Set<HotelAmenity>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (existing is not null) return existing;

        var a = new HotelAmenity
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            Name = name,
            SortOrder = sort,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        // if Scope exists, set "Hotel" (string or enum)
        SetEnumOrString(a, "Scope", "Hotel");

        db.Add(a);
        await db.SaveChangesAsync(ct);
        return a;
    }

    private static async Task<RoomAmenity> GetOrCreateRoomAmenityAsync(AppDbContext db, Guid tenantId, string code, string name, int sort, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"roomAmenity:{code}");
        var existing = await db.Set<RoomAmenity>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (existing is not null) return existing;

        var a = new RoomAmenity
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            Name = name,
            SortOrder = sort,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(a);
        await db.SaveChangesAsync(ct);
        return a;
    }

    private static async Task<BedType> GetOrCreateBedTypeAsync(AppDbContext db, Guid tenantId, string code, string name, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"bedType:{code}");
        var existing = await db.Set<BedType>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (existing is not null) return existing;

        var b = new BedType
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(b);
        await db.SaveChangesAsync(ct);
        return b;
    }

    private static async Task<MealPlan> GetOrCreateMealPlanAsync(AppDbContext db, Guid tenantId, string code, string name, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"mealPlan:{code}");
        var existing = await db.Set<MealPlan>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (existing is not null) return existing;

        var mp = new MealPlan
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(mp);
        await db.SaveChangesAsync(ct);
        return mp;
    }

    private static async Task<CancellationPolicy> GetOrCreateCancellationPolicyAsync(AppDbContext db, Guid tenantId, Guid hotelId, CancellationToken ct)
    {
        var code = "FLEX";
        var id = StableGuid(tenantId, $"cancelPolicy:{hotelId}:{code}");

        var existing = await db.Set<CancellationPolicy>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.HotelId == hotelId && x.Code == code, ct);

        if (existing is not null) return existing;

        var p = new CancellationPolicy
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            Code = code,
            Name = "Linh hoạt",
            Description = "Miễn phí hủy trong thời hạn cho phép. Sau đó có thể tính phí theo quy định.",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        EnsureNonZeroEnum(p, "Type");

        db.Add(p);
        await db.SaveChangesAsync(ct);
        return p;
    }

    private static async Task EnsureCancellationPolicyRulesAsync(AppDbContext db, Guid tenantId, Guid cancellationPolicyId, CancellationToken ct)
    {
        // Rule 1: free cancel until 3 days before check-in
        var id1 = StableGuid(tenantId, $"cancelPolicyRule:{cancellationPolicyId}:1");
        var r1 = await db.Set<CancellationPolicyRule>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id1, ct);

        if (r1 is null)
        {
            r1 = new CancellationPolicyRule
            {
                Id = id1,
                TenantId = tenantId,
                CancellationPolicyId = cancellationPolicyId,
                Priority = 10,
                CurrencyCode = "VND",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.Now
            };

            // Try set window fields if exist (HoursBefore/DaysBefore etc.)
            TrySetInt(r1, "DaysBefore", 3);
            TrySetDecimal(r1, "PenaltyAmount", 0m);
            TrySetDecimal(r1, "PenaltyPercent", 0m);

            db.Add(r1);
        }

        // Rule 2: within 3 days => 1 night penalty (as percent)
        var id2 = StableGuid(tenantId, $"cancelPolicyRule:{cancellationPolicyId}:2");
        var r2 = await db.Set<CancellationPolicyRule>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id2, ct);

        if (r2 is null)
        {
            r2 = new CancellationPolicyRule
            {
                Id = id2,
                TenantId = tenantId,
                CancellationPolicyId = cancellationPolicyId,
                Priority = 20,
                CurrencyCode = "VND",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.Now
            };

            TrySetInt(r2, "DaysBefore", 0);
            TrySetDecimal(r2, "PenaltyPercent", 100m);

            db.Add(r2);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<CheckInOutRule> GetOrCreateCheckInOutRuleAsync(AppDbContext db, Guid tenantId, Guid hotelId, CancellationToken ct)
    {
        var code = "DEFAULT";
        var id = StableGuid(tenantId, $"checkInOut:{hotelId}:{code}");

        var existing = await db.Set<CheckInOutRule>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.HotelId == hotelId && x.Code == code, ct);

        if (existing is not null) return existing;

        var r = new CheckInOutRule
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            Code = code,
            Name = "Giờ nhận/trả phòng tiêu chuẩn",
            CheckInFrom = new TimeOnly(14, 0),
            CheckInTo = new TimeOnly(23, 0),
            CheckOutFrom = new TimeOnly(6, 0),
            CheckOutTo = new TimeOnly(12, 0),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(r);
        await db.SaveChangesAsync(ct);
        return r;
    }

    private static async Task<PropertyPolicy> GetOrCreatePropertyPolicyAsync(AppDbContext db, Guid tenantId, Guid hotelId, CancellationToken ct)
    {
        var code = "HOUSE_RULES";
        var id = StableGuid(tenantId, $"propertyPolicy:{hotelId}:{code}");

        var existing = await db.Set<PropertyPolicy>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.HotelId == hotelId && x.Code == code, ct);

        if (existing is not null) return existing;

        var p = new PropertyPolicy
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            Code = code,
            Name = "Nội quy cơ bản",
            PolicyJson = """
            {
              "noSmoking": true,
              "petsAllowed": false,
              "quietHours": "22:00-06:00"
            }
            """,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(p);
        await db.SaveChangesAsync(ct);
        return p;
    }

    private static async Task<RoomType> GetOrCreateRoomTypeAsync(
        AppDbContext db,
        Guid tenantId,
        Guid hotelId,
        string code,
        string name,
        int sort,
        int areaSquareMeters,
        int defaultAdults,
        int defaultChildren,
        int maxAdults,
        int maxChildren,
        int maxGuests,
        int totalUnits,
        CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"roomType:{hotelId}:{code}");
        var existing = await db.Set<RoomType>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.HotelId == hotelId && x.Code == code, ct);

        if (existing is not null)
        {
            existing.Name = name;
            existing.SortOrder = sort;
            existing.DescriptionMarkdown = $"**{name}** (demo room type).";
            existing.DescriptionHtml = $"<p><strong>{name}</strong> (demo room type).</p>";
            existing.AreaSquareMeters = areaSquareMeters;
            existing.DefaultAdults = defaultAdults;
            existing.DefaultChildren = defaultChildren;
            existing.MaxAdults = maxAdults;
            existing.MaxChildren = maxChildren;
            existing.MaxGuests = maxGuests;
            existing.TotalUnits = totalUnits;
            existing.CoverImageUrl = "https://picsum.photos/seed/room/1200/600";
            existing.Status = RoomTypeStatus.Active;
            existing.IsActive = true;
            existing.IsDeleted = false;
            existing.UpdatedAt = DateTimeOffset.Now;
            await db.SaveChangesAsync(ct);
            await EnsureRoomTypeImageAsync(db, tenantId, existing.Id, ct);
            return existing;
        }

        var rt = new RoomType
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            Code = code,
            Name = name,
            SortOrder = sort,
            DescriptionMarkdown = $"**{name}** (demo room type).",
            DescriptionHtml = $"<p><strong>{name}</strong> (demo room type).</p>",
            AreaSquareMeters = areaSquareMeters,
            DefaultAdults = defaultAdults,
            DefaultChildren = defaultChildren,
            MaxAdults = maxAdults,
            MaxChildren = maxChildren,
            MaxGuests = maxGuests,
            TotalUnits = totalUnits,
            CoverImageUrl = "https://picsum.photos/seed/room/1200/600",
            Status = RoomTypeStatus.Active,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(rt);
        await db.SaveChangesAsync(ct);

        // add 1 image row
        await EnsureRoomTypeImageAsync(db, tenantId, rt.Id, ct);

        return rt;
    }

    private static async Task<RatePlan> GetOrCreateRatePlanAsync(
        AppDbContext db,
        Guid tenantId,
        Guid hotelId,
        string code,
        string name,
        Guid cancellationPolicyId,
        Guid checkInOutRuleId,
        Guid propertyPolicyId,
        CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"ratePlan:{hotelId}:{code}");
        var existing = await db.Set<RatePlan>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.HotelId == hotelId && x.Code == code, ct);

        if (existing is not null)
        {
            existing.Name = name;
            existing.Description = "Giá tốt nhất theo ngày (demo).";
            existing.Status = RatePlanStatus.Active;
            existing.Type = RatePlanType.Public;
            existing.Refundable = true;
            existing.BreakfastIncluded = false;
            existing.IsActive = true;
            existing.IsDeleted = false;
            existing.UpdatedAt = DateTimeOffset.Now;

            TrySetGuid(existing, "CancellationPolicyId", cancellationPolicyId);
            TrySetGuid(existing, "CheckInOutRuleId", checkInOutRuleId);
            TrySetGuid(existing, "PropertyPolicyId", propertyPolicyId);

            await db.SaveChangesAsync(ct);
            return existing;
        }

        var rp = new RatePlan
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            Code = code,
            Name = name,
            Description = "Giá tốt nhất theo ngày (demo).",
            Type = RatePlanType.Public,
            Status = RatePlanStatus.Active,
            Refundable = true,
            BreakfastIncluded = false,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        // Optional FK properties exist in your entity (configured in HotelConfigurations)
        TrySetGuid(rp, "CancellationPolicyId", cancellationPolicyId);
        TrySetGuid(rp, "CheckInOutRuleId", checkInOutRuleId);
        TrySetGuid(rp, "PropertyPolicyId", propertyPolicyId);

        db.Add(rp);
        await db.SaveChangesAsync(ct);
        return rp;
    }

    private static async Task<RatePlanRoomType> GetOrCreateRatePlanRoomTypeAsync(
        AppDbContext db,
        Guid tenantId,
        Guid ratePlanId,
        Guid roomTypeId,
        string currencyCode,
        decimal basePrice,
        CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"ratePlanRoomType:{ratePlanId}:{roomTypeId}");
        var existing = await db.Set<RatePlanRoomType>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RatePlanId == ratePlanId && x.RoomTypeId == roomTypeId, ct);

        if (existing is not null) return existing;

        var x = new RatePlanRoomType
        {
            Id = id,
            TenantId = tenantId,
            RatePlanId = ratePlanId,
            RoomTypeId = roomTypeId,
            CurrencyCode = currencyCode,
            BasePrice = basePrice,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(x);
        await db.SaveChangesAsync(ct);
        return x;
    }

    private static async Task EnsureRatePlanPolicyAsync(AppDbContext db, Guid tenantId, Guid ratePlanId, CancellationToken ct)
    {
        var existing = await db.Set<RatePlanPolicy>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RatePlanId == ratePlanId, ct);

        if (existing is not null) return;

        var p = new RatePlanPolicy
        {
            Id = StableGuid(tenantId, $"ratePlanPolicy:{ratePlanId}"),
            TenantId = tenantId,
            RatePlanId = ratePlanId,
            PolicyJson = """
            {
              "payAtHotel": true,
              "breakfastIncluded": false,
              "refundable": true
            }
            """,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(p);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureInventoryAsync(AppDbContext db, Guid tenantId, Guid roomTypeId, DateOnly date, int total, int stopSell, CancellationToken ct)
    {
        var existing = await db.Set<RoomTypeInventory>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId && x.Date == date, ct);

        if (existing is not null) return;

        var inv = new RoomTypeInventory
        {
            Id = StableGuid(tenantId, $"inv:{roomTypeId}:{date:yyyyMMdd}"),
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            Date = date,
            TotalUnits = total,
            SoldUnits = 0,
            HeldUnits = 0,
            Status = stopSell > 0 ? InventoryStatus.Closed : InventoryStatus.Open,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        EnsureNonZeroEnum(inv, "Status");

        db.Add(inv);
        // caller batches SaveChanges
    }

    private static async Task EnsureDailyRateAsync(AppDbContext db, Guid tenantId, Guid ratePlanRoomTypeId, DateOnly date, string currency, decimal price, CancellationToken ct)
    {
        var existing = await db.Set<DailyRate>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RatePlanRoomTypeId == ratePlanRoomTypeId && x.Date == date, ct);

        if (existing is not null) return;

        var r = new DailyRate
        {
            Id = StableGuid(tenantId, $"rate:{ratePlanRoomTypeId}:{date:yyyyMMdd}"),
            TenantId = tenantId,
            RatePlanRoomTypeId = ratePlanRoomTypeId,
            Date = date,
            CurrencyCode = currency,
            Price = price,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(r);
        // caller batches SaveChanges
    }

    private static async Task<ExtraService> GetOrCreateExtraServiceAsync(AppDbContext db, Guid tenantId, Guid hotelId, string code, string name, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"extraService:{hotelId}:{code}");

        var existing = await db.Set<ExtraService>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.HotelId == hotelId && x.Code == code, ct);

        if (existing is not null) return existing;

        var s = new ExtraService
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            Code = code,
            Name = name,
            Description = "Dịch vụ demo.",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        EnsureNonZeroEnum(s, "Type");

        db.Add(s);
        await db.SaveChangesAsync(ct);
        return s;
    }

    private static async Task EnsureExtraServicePriceAsync(
        AppDbContext db,
        Guid tenantId,
        Guid extraServiceId,
        DateOnly startDate,
        DateOnly endDate,
        string currency,
        decimal price,
        CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"extraServicePrice:{extraServiceId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}");

        var existing = await db.Set<ExtraServicePrice>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (existing is not null) return;

        var p = new ExtraServicePrice
        {
            Id = id,
            TenantId = tenantId,
            ExtraServiceId = extraServiceId,
            StartDate = startDate,
            EndDate = endDate,
            CurrencyCode = currency,
            Price = price,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(p);
        // caller will SaveChanges
    }

    private static async Task EnsureHotelAmenityLinkAsync(AppDbContext db, Guid tenantId, Guid hotelId, Guid amenityId, CancellationToken ct)
    {
        var existing = await db.Set<HotelAmenityLink>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.HotelId == hotelId && x.AmenityId == amenityId, ct);

        if (existing is not null) return;

        var link = new HotelAmenityLink
        {
            Id = StableGuid(tenantId, $"hotelAmenityLink:{hotelId}:{amenityId}"),
            TenantId = tenantId,
            HotelId = hotelId,
            AmenityId = amenityId,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(link);
    }

    private static async Task EnsureRoomAmenityLinkAsync(AppDbContext db, Guid tenantId, Guid roomTypeId, Guid amenityId, CancellationToken ct)
    {
        var existing = await db.Set<RoomAmenityLink>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId && x.AmenityId == amenityId, ct);

        if (existing is not null) return;

        var link = new RoomAmenityLink
        {
            Id = StableGuid(tenantId, $"roomAmenityLink:{roomTypeId}:{amenityId}"),
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            AmenityId = amenityId,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(link);
    }

    private static async Task EnsureRoomTypeBedAsync(AppDbContext db, Guid tenantId, Guid roomTypeId, Guid bedTypeId, int count, CancellationToken ct)
    {
        var existing = await db.Set<RoomTypeBed>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId && x.BedTypeId == bedTypeId, ct);

        if (existing is not null)
        {
            // Update count if property exists
            TrySetInt(existing, "Count", count);
            return;
        }

        var link = new RoomTypeBed
        {
            Id = StableGuid(tenantId, $"roomTypeBed:{roomTypeId}:{bedTypeId}"),
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            BedTypeId = bedTypeId,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        TrySetInt(link, "Count", count);

        db.Add(link);
    }

    private static async Task EnsureOccupancyRuleAsync(
        AppDbContext db,
        Guid tenantId,
        Guid roomTypeId,
        int minAdults,
        int maxAdults,
        int minChildren,
        int maxChildren,
        int minGuests,
        int maxGuests,
        int maxInfants,
        CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"occ:{roomTypeId}");
        var existing = await db.Set<RoomTypeOccupancyRule>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (existing is not null)
        {
            existing.MinAdults = minAdults;
            existing.MaxAdults = maxAdults;
            existing.MinChildren = minChildren;
            existing.MaxChildren = maxChildren;
            existing.MinGuests = minGuests;
            existing.MaxGuests = maxGuests;
            existing.MaxInfants = maxInfants;
            existing.AllowsInfants = maxInfants > 0;
            existing.IsActive = true;
            existing.IsDeleted = false;
            existing.UpdatedAt = DateTimeOffset.Now;

            await db.SaveChangesAsync(ct);
            return;
        }

        var rule = new RoomTypeOccupancyRule
        {
            Id = id,
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            MinAdults = minAdults,
            MaxAdults = maxAdults,
            MinChildren = minChildren,
            MaxChildren = maxChildren,
            MinGuests = minGuests,
            MaxGuests = maxGuests,
            MaxInfants = maxInfants,
            AllowsInfants = maxInfants > 0,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        EnsureNonZeroEnum(rule, "RuleType");

        db.Add(rule);
    }

    private static async Task EnsureRoomTypeMealPlanAsync(AppDbContext db, Guid tenantId, Guid roomTypeId, Guid mealPlanId, CancellationToken ct)
    {
        var existing = await db.Set<RoomTypeMealPlan>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId && x.MealPlanId == mealPlanId, ct);

        if (existing is not null) return;

        var link = new RoomTypeMealPlan
        {
            Id = StableGuid(tenantId, $"roomTypeMeal:{roomTypeId}:{mealPlanId}"),
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            MealPlanId = mealPlanId,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(link);
    }

    private static async Task EnsureHotelImageAsync(AppDbContext db, Guid tenantId, Guid hotelId, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"hotelImage:{hotelId}:cover");

        var existing = await db.Set<HotelImage>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (existing is not null) return;

        var img = new HotelImage
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            ImageUrl = "https://picsum.photos/seed/hotelimg/1200/600",
            Title = "Cover",
            AltText = "Hotel cover image",
            SortOrder = 1,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        SetEnumOrString(img, "Kind", "Cover");

        db.Add(img);
    }

    private static async Task EnsureRoomTypeImageAsync(AppDbContext db, Guid tenantId, Guid roomTypeId, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"roomTypeImage:{roomTypeId}:1");

        var existing = await db.Set<RoomTypeImage>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (existing is not null) return;

        var img = new RoomTypeImage
        {
            Id = id,
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            ImageUrl = "https://picsum.photos/seed/roomimg/1200/600",
            Title = "Room",
            AltText = "Room image",
            SortOrder = 1,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        SetEnumOrString(img, "Kind", "Gallery");

        db.Add(img);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureHotelContactAsync(AppDbContext db, Guid tenantId, Guid hotelId, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"hotelContact:{hotelId}:primary");

        var existing = await db.Set<HotelContact>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (existing is not null) return;

        var c = new HotelContact
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            ContactName = "Lễ tân (Demo)",
            RoleTitle = "Front Office",
            Phone = "0900-111-222",
            Email = "frontdesk@demo.local",
            IsPrimary = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(c);
    }

    private static async Task EnsureRoomTypePolicyAsync(AppDbContext db, Guid tenantId, Guid roomTypeId, CancellationToken ct)
    {
        var existing = await db.Set<RoomTypePolicy>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId, ct);

        if (existing is not null) return;

        var p = new RoomTypePolicy
        {
            Id = StableGuid(tenantId, $"roomTypePolicy:{roomTypeId}"),
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            PolicyJson = """
            {
              "earlyCheckIn": "subject_to_availability",
              "lateCheckOut": "fee_may_apply",
              "extraBed": "limited"
            }
            """,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        db.Add(p);
    }

    private static async Task EnsurePromoRateOverrideStubAsync(
        AppDbContext db,
        Guid tenantId,
        Guid ratePlanRoomTypeId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"promoOverride:{ratePlanRoomTypeId}:{start:yyyyMMdd}:{end:yyyyMMdd}");

        var existing = await db.Set<PromoRateOverride>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (existing is not null) return;

        var p = new PromoRateOverride
        {
            Id = id,
            TenantId = tenantId,
            RatePlanRoomTypeId = ratePlanRoomTypeId,
            PromoCode = "DEMO10",
            StartDate = start,
            EndDate = end,
            CurrencyCode = "VND",
            DiscountPercent = 10m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        // Optional link to common promo entity
        TrySetGuid(p, "PromoCodeId", Guid.Empty);

        db.Add(p);
    }

    private static async Task EnsureHotelReviewStubAsync(AppDbContext db, Guid tenantId, Guid hotelId, CancellationToken ct)
    {
        var id = StableGuid(tenantId, $"hotelReview:{hotelId}:1");

        var existing = await db.Set<HotelReview>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (existing is not null) return;

        var r = new HotelReview
        {
            Id = id,
            TenantId = tenantId,
            HotelId = hotelId,
            Title = "Trải nghiệm tốt (Demo)",
            Content = "Khách sạn demo sạch sẽ, vị trí thuận tiện. Sẽ quay lại!",
            Rating = 5,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        EnsureNonZeroEnum(r, "Status");

        // Optional BookingId
        TrySetGuid(r, "BookingId", Guid.Empty);

        db.Add(r);
    }

    // =========================
    // Tenant discovery (SQL)
    // =========================

    private sealed record TenantRow(Guid Id, string Code);

    private static async Task<List<TenantRow>> LoadTenantsAsync(AppDbContext db, CancellationToken ct)
    {
        var rows = new List<TenantRow>();

        var conn = db.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        // Assumptions (as per your architecture):
        // - schema: tenants
        // - table: Tenants
        // - columns: Id (uniqueidentifier), Code (nvarchar)
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT [Id], [Code]
                          FROM [tenants].[Tenants]
                          WHERE [IsDeleted] = 0
                          ORDER BY [Code]
                          """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetGuid(0);
            var code = reader.GetString(1);
            if (!string.IsNullOrWhiteSpace(code))
                rows.Add(new TenantRow(id, code.Trim()));
        }

        return rows;

        static async Task EnsureOpenAsync(DbConnection conn, CancellationToken ct)
        {
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
        }
    }

    // =========================
    // Tenant context (reflection)
    // =========================

    private static object? ResolveTenantContext(IServiceProvider sp)
    {
        // Find ITenantContext type in loaded assemblies
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t is { IsInterface: true, Name: "ITenantContext" });

        return type is null ? null : sp.GetService(type);
    }

    private static void UseTenantContextOrThrow(object? tenantContext, Guid tenantId, string tenantCode)
    {
        if (tenantContext is null)
        {
            // If your interceptor requires tenant context, this should not be null.
            throw new InvalidOperationException("ITenantContext service not found. Hotel demo seed cannot satisfy tenant enforcement.");
        }

        // Try common method names
        var tcType = tenantContext.GetType();
        var methods = new[]
        {
            "SetTenant",
            "SwitchTenant",
            "UseTenant",
            "SetCurrentTenant",
            "SetTenantContext"
        };

        foreach (var m in methods)
        {
            // (Guid tenantId, string tenantCode)
            var mi2 = tcType.GetMethod(m, new[] { typeof(Guid), typeof(string) });
            if (mi2 is not null)
            {
                mi2.Invoke(tenantContext, new object[] { tenantId, tenantCode });
                return;
            }

            // (Guid tenantId)
            var mi1 = tcType.GetMethod(m, new[] { typeof(Guid) });
            if (mi1 is not null)
            {
                mi1.Invoke(tenantContext, new object[] { tenantId });
                // best effort set code too
                TrySetStringByReflection(tenantContext, "TenantCode", tenantCode);
                TrySetBoolByReflection(tenantContext, "HasTenant", true);
                return;
            }
        }

        // Fallback: try set properties/fields
        var okId = TrySetGuidByReflection(tenantContext, "TenantId", tenantId) || TrySetGuidByReflection(tenantContext, "_tenantId", tenantId);
        var okCode = TrySetStringByReflection(tenantContext, "TenantCode", tenantCode) || TrySetStringByReflection(tenantContext, "_tenantCode", tenantCode);
        var okHas = TrySetBoolByReflection(tenantContext, "HasTenant", true) || TrySetBoolByReflection(tenantContext, "_hasTenant", true);

        if (okId && okHas)
            return;

        throw new InvalidOperationException("Unable to set tenant context (ITenantContext). Add/confirm a SetTenant/SwitchTenant method or writable TenantId/HasTenant.");
    }

    private static bool TrySetGuidByReflection(object obj, string name, Guid value)
    {
        try
        {
            var t = obj.GetType();
            var p = t.GetProperty(name);
            if (p is not null && p.CanWrite && (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)))
            {
                p.SetValue(obj, p.PropertyType == typeof(Guid?) ? (Guid?)value : value);
                return true;
            }

            var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f is not null && (f.FieldType == typeof(Guid) || f.FieldType == typeof(Guid?)))
            {
                f.SetValue(obj, f.FieldType == typeof(Guid?) ? (Guid?)value : value);
                return true;
            }
        }
        catch { }
        return false;
    }

    private static bool TrySetStringByReflection(object obj, string name, string value)
    {
        try
        {
            var t = obj.GetType();
            var p = t.GetProperty(name);
            if (p is not null && p.CanWrite && p.PropertyType == typeof(string))
            {
                p.SetValue(obj, value);
                return true;
            }

            var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f is not null && f.FieldType == typeof(string))
            {
                f.SetValue(obj, value);
                return true;
            }
        }
        catch { }
        return false;
    }

    private static bool TrySetBoolByReflection(object obj, string name, bool value)
    {
        try
        {
            var t = obj.GetType();
            var p = t.GetProperty(name);
            if (p is not null && p.CanWrite && (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?)))
            {
                p.SetValue(obj, p.PropertyType == typeof(bool?) ? (bool?)value : value);
                return true;
            }

            var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f is not null && (f.FieldType == typeof(bool) || f.FieldType == typeof(bool?)))
            {
                f.SetValue(obj, f.FieldType == typeof(bool?) ? (bool?)value : value);
                return true;
            }
        }
        catch { }
        return false;
    }

    // =========================
    // Reflection setters for unknown optional properties
    // =========================

    private static void EnsureNonZeroEnum(object entity, string propertyName)
    {
        try
        {
            var pi = entity.GetType().GetProperty(propertyName);
            if (pi is null) return;

            var t = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
            if (!t.IsEnum) return;

            var current = pi.GetValue(entity);
            if (current is null) return;

            var currentInt = Convert.ToInt32(current);
            if (currentInt != 0) return;

            var values = Enum.GetValues(t).Cast<object>().Select(Convert.ToInt32).Distinct().OrderBy(x => x).ToList();
            var firstNonZero = values.FirstOrDefault(x => x != 0);
            if (firstNonZero == 0) return;

            var boxed = Enum.ToObject(t, firstNonZero);
            pi.SetValue(entity, boxed);
        }
        catch { }
    }

    private static void SetEnumOrString(object entity, string propertyName, string desiredName)
    {
        try
        {
            var pi = entity.GetType().GetProperty(propertyName);
            if (pi is null || !pi.CanWrite) return;

            var t = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
            if (t == typeof(string))
            {
                pi.SetValue(entity, desiredName);
                return;
            }

            if (t.IsEnum)
            {
                // Try parse by name
                if (Enum.TryParse(t, desiredName, ignoreCase: true, out var parsed) && parsed is not null)
                {
                    pi.SetValue(entity, parsed);
                    return;
                }

                // Fallback: first non-zero
                var values = Enum.GetValues(t).Cast<object>().Select(Convert.ToInt32).Distinct().OrderBy(x => x).ToList();
                var firstNonZero = values.FirstOrDefault(x => x != 0);
                if (firstNonZero != 0)
                    pi.SetValue(entity, Enum.ToObject(t, firstNonZero));
            }
        }
        catch { }
    }

    private static void TrySetGuid(object entity, string propertyName, Guid value)
    {
        try
        {
            var pi = entity.GetType().GetProperty(propertyName);
            if (pi is null || !pi.CanWrite) return;

            if (pi.PropertyType == typeof(Guid))
                pi.SetValue(entity, value);
            else if (pi.PropertyType == typeof(Guid?))
                pi.SetValue(entity, (Guid?)value);
        }
        catch { }
    }

    private static void TrySetInt(object entity, string propertyName, int value)
    {
        try
        {
            var pi = entity.GetType().GetProperty(propertyName);
            if (pi is null || !pi.CanWrite) return;

            if (pi.PropertyType == typeof(int))
                pi.SetValue(entity, value);
            else if (pi.PropertyType == typeof(int?))
                pi.SetValue(entity, (int?)value);
            else if (pi.PropertyType == typeof(short))
                pi.SetValue(entity, (short)value);
            else if (pi.PropertyType == typeof(short?))
                pi.SetValue(entity, (short?)value);
        }
        catch { }
    }

    private static void TrySetDecimal(object entity, string propertyName, decimal value)
    {
        try
        {
            var pi = entity.GetType().GetProperty(propertyName);
            if (pi is null || !pi.CanWrite) return;

            if (pi.PropertyType == typeof(decimal))
                pi.SetValue(entity, value);
            else if (pi.PropertyType == typeof(decimal?))
                pi.SetValue(entity, (decimal?)value);
        }
        catch { }
    }

    // =========================
    // Deterministic IDs / Math
    // =========================

    private static Guid StableGuid(Guid tenantId, string key)
    {
        // Deterministic GUID based on SHA256(tenantId:key)
        var input = $"{tenantId:N}:{key}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        Span<byte> g = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(g);

        // Make it RFC4122 (v4-ish) to avoid weird GUID versions
        g[7] = (byte)((g[7] & 0x0F) | 0x40);
        g[8] = (byte)((g[8] & 0x3F) | 0x80);

        return new Guid(g);
    }

    private static decimal RoundToThousand(decimal x)
        => Math.Round(x / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;

    // =========================
    // Program.cs integration
    // =========================
    /*
      // Program.cs (after migrations and other seeds)
      using (var scope = app.Services.CreateScope())
      {
          var sp = scope.ServiceProvider;

          // Example: controlled by config
          var cfg = sp.GetRequiredService<IConfiguration>();
          if (cfg.GetValue<bool>("Seed:Demo"))
          {
              await TicketBooking.Infrastructure.Seed.HotelDemoSeed.SeedAsync(sp);
          }
      }

      // Optional config:
      // "Seed": {
      //   "Demo": true,
      //   "Demo:HotelTenantCodes": "KS001,KS002",
      //   "Demo:HotelDays": "14"
      // }
    */
}
