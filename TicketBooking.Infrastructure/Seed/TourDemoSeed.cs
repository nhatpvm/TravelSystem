// FILE #244: TicketBooking.Infrastructure/Seed/TourDemoSeed.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Infrastructure.Seed;

public static class TourDemoSeed
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("TourDemoSeed");
        var cfg = sp.GetService<IConfiguration>();
        var db = sp.GetRequiredService<AppDbContext>();
        var tenantContext = ResolveTenantContext(sp);

        var onlyTenantCodes = (cfg?["Seed:Demo:TourTenantCodes"] ?? cfg?["Seed:TourTenantCodes"] ?? "TOUR001")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tenants = await db.Tenants.IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && onlyTenantCodes.Contains(x.Code))
            .Select(x => new SeedTenantInfo
            {
                Id = x.Id,
                Code = x.Code
            })
            .ToListAsync(ct);

        if (tenants.Count == 0)
        {
            logger.LogWarning("Tour demo seed skipped: no tenants matched Seed:Demo:TourTenantCodes.");
            return;
        }

        logger.LogInformation("Tour demo seed starting for {Count} tenant(s).", tenants.Count);

        foreach (var tenant in tenants)
        {
            ct.ThrowIfCancellationRequested();

            UseTenantContextOrThrow(tenantContext, tenant.Id, tenant.Code);

            try
            {
                await SeedForTenantAsync(db, tenant.Id, tenant.Code, logger, ct);
            }
            finally
            {
                db.ChangeTracker.Clear();
                ClearTenantContext(tenantContext);
            }
        }

        logger.LogInformation("Tour demo seed finished.");
    }

    private static async Task SeedForTenantAsync(
        AppDbContext db,
        Guid tenantId,
        string tenantCode,
        ILogger logger,
        CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(DateTime.Now);

        var tour1 = await EnsureTourAsync(
            db,
            tenantId,
            code: "TOUR-DEMO-01",
            apply: x =>
            {
                x.ProviderId = null;
                x.PrimaryLocationId = null;
                x.Name = "Đà Nẵng - Hội An - Huế 3N2Đ";
                x.Slug = $"da-nang-hoi-an-hue-3n2d-{tenantCode.ToLowerInvariant()}";
                x.Type = TourType.Domestic;
                x.Status = TourStatus.Active;
                x.Difficulty = TourDifficulty.Easy;
                x.DurationDays = 3;
                x.DurationNights = 2;
                x.MinGuests = 1;
                x.MaxGuests = 30;
                x.MinAge = 0;
                x.MaxAge = 75;
                x.IsFeatured = true;
                x.IsFeaturedOnHome = true;
                x.IsPrivateTourSupported = true;
                x.IsInstantConfirm = false;
                x.CountryCode = "VN";
                x.Province = "Đà Nẵng";
                x.City = "Đà Nẵng";
                x.MeetingPointSummary = "Sân bay Đà Nẵng / trung tâm thành phố Đà Nẵng";
                x.ShortDescription = "Hành trình khám phá Đà Nẵng - Hội An - Huế 3 ngày 2 đêm, phù hợp gia đình và nhóm bạn.";
                x.DescriptionMarkdown =
                    "## Đà Nẵng - Hội An - Huế 3N2Đ\n\n" +
                    "- Tham quan Bà Nà / bán đảo Sơn Trà / Hội An\n" +
                    "- Khám phá Cố đô Huế\n" +
                    "- Khách sạn tiêu chuẩn 3-4 sao\n" +
                    "- Xe đưa đón và HDV suốt tuyến";
                x.DescriptionHtml =
                    "<h2>Đà Nẵng - Hội An - Huế 3N2Đ</h2><ul><li>Tham quan Đà Nẵng - Hội An - Huế</li><li>Khách sạn 3-4 sao</li><li>Xe đưa đón + HDV</li></ul>";
                x.HighlightsJson = """
                                   ["Bà Nà Hills","Phố cổ Hội An","Đại Nội Huế","Ẩm thực miền Trung"]
                                   """;
                x.IncludesJson = """
                                 ["Xe du lịch đời mới","Khách sạn 3-4 sao","Ăn theo chương trình","Hướng dẫn viên","Vé tham quan theo chương trình"]
                                 """;
                x.ExcludesJson = """
                                 ["Chi phí cá nhân","VAT","Phụ thu phòng đơn","Vé tự chọn ngoài chương trình"]
                                 """;
                x.TermsJson = """
                              {"booking":"Đặt cọc trước để giữ chỗ","childPolicy":"Giá trẻ em áp dụng theo chiều cao/độ tuổi","cancellation":"Áp dụng theo chính sách từng mốc thời gian"}
                              """;
                x.MetadataJson = """
                                 {"theme":"central-vietnam","product":"tour-demo","seed":"phase-tour"}
                                 """;
                x.CoverImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=1200&auto=format&fit=crop";
                x.CoverMediaAssetId = null;
                x.CurrencyCode = "VND";
                x.IsActive = true;
                x.IsDeleted = false;
            },
            ct);

        var tour2 = await EnsureTourAsync(
            db,
            tenantId,
            code: "TOUR-DEMO-02",
            apply: x =>
            {
                x.ProviderId = null;
                x.PrimaryLocationId = null;
                x.Name = "Phú Quốc 4N3Đ - Grand World - Cáp treo Hòn Thơm";
                x.Slug = $"phu-quoc-4n3d-{tenantCode.ToLowerInvariant()}";
                x.Type = TourType.Domestic;
                x.Status = TourStatus.Active;
                x.Difficulty = TourDifficulty.Easy;
                x.DurationDays = 4;
                x.DurationNights = 3;
                x.MinGuests = 1;
                x.MaxGuests = 25;
                x.MinAge = 0;
                x.MaxAge = 75;
                x.IsFeatured = true;
                x.IsFeaturedOnHome = false;
                x.IsPrivateTourSupported = true;
                x.IsInstantConfirm = false;
                x.CountryCode = "VN";
                x.Province = "Kiên Giang";
                x.City = "Phú Quốc";
                x.MeetingPointSummary = "Sân bay Phú Quốc / điểm đón khu Dương Đông";
                x.ShortDescription = "Tour nghỉ dưỡng Phú Quốc 4 ngày 3 đêm, kết hợp vui chơi Grand World và cáp treo Hòn Thơm.";
                x.DescriptionMarkdown =
                    "## Phú Quốc 4N3Đ\n\n" +
                    "- Grand World Phú Quốc\n" +
                    "- Cáp treo Hòn Thơm\n" +
                    "- Tắm biển - nghỉ dưỡng resort\n" +
                    "- Lịch trình nhẹ nhàng, phù hợp gia đình";
                x.DescriptionHtml =
                    "<h2>Phú Quốc 4N3Đ</h2><ul><li>Grand World</li><li>Cáp treo Hòn Thơm</li><li>Resort nghỉ dưỡng</li></ul>";
                x.HighlightsJson = """
                                   ["Grand World","Cáp treo Hòn Thơm","Sunset Town","Bãi Sao"]
                                   """;
                x.IncludesJson = """
                                 ["Khách sạn/resort","Xe đưa đón","Một số bữa ăn","Hướng dẫn viên","Vé tham quan theo chương trình"]
                                 """;
                x.ExcludesJson = """
                                 ["Chi tiêu cá nhân","Phụ thu phòng đơn","VAT","Các trò chơi ngoài chương trình"]
                                 """;
                x.TermsJson = """
                              {"booking":"Giữ chỗ theo lịch khởi hành","childPolicy":"Giá trẻ em theo độ tuổi","cancellation":"Theo mốc thời gian gần ngày đi"}
                              """;
                x.MetadataJson = """
                                 {"theme":"island","product":"tour-demo","seed":"phase-tour"}
                                 """;
                x.CoverImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1200&auto=format&fit=crop";
                x.CoverMediaAssetId = null;
                x.CurrencyCode = "VND";
                x.IsActive = true;
                x.IsDeleted = false;
            },
            ct);

        await SeedTour1ChildrenAsync(db, tenantId, tenantCode, tour1, today, now, ct);
        await SeedTour2ChildrenAsync(db, tenantId, tenantCode, tour2, today, now, ct);

        logger.LogInformation("Tour demo seeded for tenant {TenantCode}. Tours: {Tour1}, {Tour2}", tenantCode, tour1.Code, tour2.Code);
    }

    private static async Task<Tour> EnsureTourAsync(
        AppDbContext db,
        Guid tenantId,
        string code,
        Action<Tour> apply,
        CancellationToken ct)
    {
        var now = DateTimeOffset.Now;

        var entity = await db.Tours.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (entity is null)
        {
            entity = new Tour
            {
                Id = StableGuid(tenantId, $"tour:{code}"),
                TenantId = tenantId,
                Code = code,
                CreatedAt = now
            };

            apply(entity);
            entity.UpdatedAt = now;
            db.Tours.Add(entity);
        }
        else
        {
            apply(entity);
            entity.IsDeleted = false;
            entity.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return entity;
    }

    private static async Task SeedTour1ChildrenAsync(
        AppDbContext db,
        Guid tenantId,
        string tenantCode,
        Tour tour,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await ReplaceTourImagesAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourImage
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:image:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1200&auto=format&fit=crop",
                    Caption = "Biển miền Trung",
                    AltText = "Biển miền Trung",
                    Title = "Biển miền Trung",
                    IsPrimary = true,
                    IsCover = true,
                    IsFeatured = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourImage
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:image:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=1200&auto=format&fit=crop",
                    Caption = "Phố cổ Hội An",
                    AltText = "Phố cổ Hội An",
                    Title = "Phố cổ Hội An",
                    IsPrimary = false,
                    IsCover = false,
                    IsFeatured = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourImage
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:image:3"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1516483638261-f4dbaf036963?q=80&w=1200&auto=format&fit=crop",
                    Caption = "Đại Nội Huế",
                    AltText = "Đại Nội Huế",
                    Title = "Đại Nội Huế",
                    IsPrimary = false,
                    IsCover = false,
                    IsFeatured = false,
                    SortOrder = 3,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourContactsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourContact
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:contact:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Name = "Bộ phận Sales Tour",
                    Title = "Tư vấn tour",
                    Department = "Sales",
                    Phone = "0901-100-200",
                    Email = $"tour-sales-{tenantCode.ToLowerInvariant()}@demo.local",
                    ContactType = TourContactType.Sales,
                    IsPrimary = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourContact
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:contact:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Name = "Hotline vận hành",
                    Title = "Điều hành tour",
                    Department = "Operation",
                    Phone = "0909-888-777",
                    Email = $"tour-ops-{tenantCode.ToLowerInvariant()}@demo.local",
                    ContactType = TourContactType.Operation,
                    IsPrimary = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourPoliciesAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourPolicy
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:policy:general"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "GENERAL",
                    Name = "Điều khoản chung",
                    Type = TourPolicyType.General,
                    ShortDescription = "Các điều khoản chung áp dụng cho tour.",
                    DescriptionMarkdown = "Khách cần có mặt trước giờ hẹn tối thiểu 30 phút. Vui lòng mang giấy tờ tùy thân hợp lệ.",
                    DescriptionHtml = "<p>Khách cần có mặt trước giờ hẹn tối thiểu 30 phút. Vui lòng mang giấy tờ tùy thân hợp lệ.</p>",
                    PolicyJson = """{"arrivalLeadMinutes":30}""",
                    IsHighlighted = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourPolicy
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:policy:cancellation"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "CANCEL",
                    Name = "Chính sách hủy",
                    Type = TourPolicyType.Cancellation,
                    ShortDescription = "Hủy gần ngày đi sẽ phát sinh phí.",
                    DescriptionMarkdown = "- Trước 7 ngày: miễn phí\n- 3-6 ngày: phí 30%\n- 1-2 ngày: phí 70%\n- Trong ngày: không hoàn",
                    DescriptionHtml = "<ul><li>Trước 7 ngày: miễn phí</li><li>3-6 ngày: phí 30%</li><li>1-2 ngày: phí 70%</li><li>Trong ngày: không hoàn</li></ul>",
                    PolicyJson = """{"rules":[{"fromDay":7,"feePercent":0},{"fromDay":3,"toDay":6,"feePercent":30},{"fromDay":1,"toDay":2,"feePercent":70},{"sameDay":true,"feePercent":100}]}""",
                    IsHighlighted = true,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourPolicy
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:policy:child"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "CHILD",
                    Name = "Chính sách trẻ em",
                    Type = TourPolicyType.Child,
                    ShortDescription = "Giá trẻ em theo độ tuổi.",
                    DescriptionMarkdown = "- Dưới 5 tuổi: giá em bé\n- 5-10 tuổi: giá trẻ em\n- Từ 11 tuổi: giá người lớn",
                    DescriptionHtml = "<ul><li>Dưới 5 tuổi: giá em bé</li><li>5-10 tuổi: giá trẻ em</li><li>Từ 11 tuổi: giá người lớn</li></ul>",
                    PolicyJson = """{"babyMaxAge":4,"childMinAge":5,"childMaxAge":10,"adultMinAge":11}""",
                    IsHighlighted = false,
                    SortOrder = 3,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourFaqsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourFaq
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:faq:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Question = "Tour có đón tại sân bay không?",
                    AnswerMarkdown = "Có. Tour hỗ trợ đón sân bay Đà Nẵng theo khung giờ quy định trong ngày khởi hành.",
                    AnswerHtml = "<p>Có. Tour hỗ trợ đón sân bay Đà Nẵng theo khung giờ quy định trong ngày khởi hành.</p>",
                    Type = TourFaqType.PickupDropoff,
                    IsHighlighted = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourFaq
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:faq:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Question = "Tour có phù hợp cho trẻ nhỏ không?",
                    AnswerMarkdown = "Lịch trình tương đối nhẹ, phù hợp cho gia đình có trẻ nhỏ. Tuy nhiên nên cân nhắc lịch di chuyển ngày 2.",
                    AnswerHtml = "<p>Lịch trình tương đối nhẹ, phù hợp cho gia đình có trẻ nhỏ. Tuy nhiên nên cân nhắc lịch di chuyển ngày 2.</p>",
                    Type = TourFaqType.ChildPolicy,
                    IsHighlighted = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourFaq
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:faq:3"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Question = "Có thể đổi ngày khởi hành không?",
                    AnswerMarkdown = "Có thể hỗ trợ đổi ngày nếu lịch mới còn chỗ và theo chính sách hiện hành.",
                    AnswerHtml = "<p>Có thể hỗ trợ đổi ngày nếu lịch mới còn chỗ và theo chính sách hiện hành.</p>",
                    Type = TourFaqType.Booking,
                    IsHighlighted = false,
                    SortOrder = 3,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourReviewsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourReview
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:review:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Rating = 4.8m,
                    Title = "Lịch trình hợp lý",
                    Content = "Tour đi vừa phải, HDV nhiệt tình, khách sạn ổn và ăn uống khá ngon.",
                    ReviewerName = "Nguyễn Minh Anh",
                    Status = TourReviewStatus.Approved,
                    IsApproved = true,
                    IsPublic = true,
                    ReplyContent = "Cảm ơn anh/chị đã đồng hành cùng tour.",
                    ReplyAt = now.AddDays(-5),
                    PublishedAt = now.AddDays(-6),
                    ApprovedAt = now.AddDays(-6),
                    IsDeleted = false,
                    CreatedAt = now.AddDays(-7),
                    UpdatedAt = now.AddDays(-5)
                },
                new TourReview
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:review:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Rating = 4.6m,
                    Title = "Phù hợp gia đình",
                    Content = "Đi với ba mẹ rất ổn, lịch trình không bị quá gấp.",
                    ReviewerName = "Trần Quốc Bảo",
                    Status = TourReviewStatus.Approved,
                    IsApproved = true,
                    IsPublic = true,
                    ReplyContent = null,
                    ReplyAt = null,
                    PublishedAt = now.AddDays(-10),
                    ApprovedAt = now.AddDays(-10),
                    IsDeleted = false,
                    CreatedAt = now.AddDays(-11),
                    UpdatedAt = now.AddDays(-10)
                }
            },
            ct);

        await ReplacePickupPointsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourPickupPoint
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:pickup:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "DAD-AIRPORT",
                    Name = "Sân bay Đà Nẵng",
                    AddressLine = "Cảng hàng không quốc tế Đà Nẵng",
                    District = "Hải Châu",
                    Province = "Đà Nẵng",
                    CountryCode = "VN",
                    PickupTime = new TimeOnly(7, 30),
                    IsDefault = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourPickupPoint
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:pickup:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "DAD-CENTER",
                    Name = "Trung tâm Đà Nẵng",
                    AddressLine = "Đường 2/9, Đà Nẵng",
                    District = "Hải Châu",
                    Province = "Đà Nẵng",
                    CountryCode = "VN",
                    PickupTime = new TimeOnly(8, 0),
                    IsDefault = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceDropoffPointsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourDropoffPoint
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:dropoff:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "DAD-CENTER",
                    Name = "Trung tâm Đà Nẵng",
                    AddressLine = "Đường 2/9, Đà Nẵng",
                    District = "Hải Châu",
                    Province = "Đà Nẵng",
                    CountryCode = "VN",
                    DropoffTime = new TimeOnly(18, 30),
                    IsDefault = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceItineraryAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new DaySeed
                {
                    DayNumber = 1,
                    Title = "Đón khách - Sơn Trà - Hội An",
                    StartLocation = "Đà Nẵng",
                    EndLocation = "Hội An",
                    AccommodationName = "Khách sạn 3-4 sao tại Đà Nẵng",
                    IncludesBreakfast = false,
                    IncludesLunch = true,
                    IncludesDinner = true,
                    TransportationSummary = "Xe du lịch",
                    Items =
                    {
                        new ItemSeed { SortOrder = 1, Type = TourItineraryItemType.Transfer, Title = "Đón khách sân bay Đà Nẵng", StartTime = new TimeOnly(7,30), EndTime = new TimeOnly(9,0), LocationName = "Sân bay Đà Nẵng", TransportationMode = "Xe du lịch" },
                        new ItemSeed { SortOrder = 2, Type = TourItineraryItemType.Sightseeing, Title = "Tham quan bán đảo Sơn Trà", StartTime = new TimeOnly(9,30), EndTime = new TimeOnly(11,30), LocationName = "Sơn Trà", IncludesTicket = true },
                        new ItemSeed { SortOrder = 3, Type = TourItineraryItemType.Meal, Title = "Ăn trưa đặc sản miền Trung", StartTime = new TimeOnly(12,0), EndTime = new TimeOnly(13,0), LocationName = "Đà Nẵng", IncludesMeal = true },
                        new ItemSeed { SortOrder = 4, Type = TourItineraryItemType.Sightseeing, Title = "Phố cổ Hội An", StartTime = new TimeOnly(15,30), EndTime = new TimeOnly(18,0), LocationName = "Hội An", IncludesTicket = true },
                        new ItemSeed { SortOrder = 5, Type = TourItineraryItemType.Meal, Title = "Ăn tối tại Hội An", StartTime = new TimeOnly(18,15), EndTime = new TimeOnly(19,15), LocationName = "Hội An", IncludesMeal = true }
                    }
                },
                new DaySeed
                {
                    DayNumber = 2,
                    Title = "Huế - Đại Nội - Chùa Thiên Mụ",
                    StartLocation = "Đà Nẵng",
                    EndLocation = "Huế",
                    AccommodationName = "Khách sạn tại Đà Nẵng",
                    IncludesBreakfast = true,
                    IncludesLunch = true,
                    IncludesDinner = false,
                    TransportationSummary = "Xe du lịch xuyên hầm Hải Vân",
                    Items =
                    {
                        new ItemSeed { SortOrder = 1, Type = TourItineraryItemType.Meal, Title = "Ăn sáng tại khách sạn", StartTime = new TimeOnly(7,0), EndTime = new TimeOnly(8,0), IncludesMeal = true },
                        new ItemSeed { SortOrder = 2, Type = TourItineraryItemType.Transfer, Title = "Khởi hành đi Huế", StartTime = new TimeOnly(8,0), EndTime = new TimeOnly(10,30), TransportationMode = "Xe du lịch" },
                        new ItemSeed { SortOrder = 3, Type = TourItineraryItemType.Sightseeing, Title = "Tham quan Đại Nội", StartTime = new TimeOnly(10,45), EndTime = new TimeOnly(12,0), LocationName = "Đại Nội Huế", IncludesTicket = true },
                        new ItemSeed { SortOrder = 4, Type = TourItineraryItemType.Meal, Title = "Ăn trưa tại Huế", StartTime = new TimeOnly(12,15), EndTime = new TimeOnly(13,15), IncludesMeal = true },
                        new ItemSeed { SortOrder = 5, Type = TourItineraryItemType.Sightseeing, Title = "Chùa Thiên Mụ", StartTime = new TimeOnly(14,0), EndTime = new TimeOnly(15,0), LocationName = "Huế" }
                    }
                },
                new DaySeed
                {
                    DayNumber = 3,
                    Title = "Mua sắm đặc sản - tiễn khách",
                    StartLocation = "Đà Nẵng",
                    EndLocation = "Đà Nẵng",
                    AccommodationName = null,
                    IncludesBreakfast = true,
                    IncludesLunch = false,
                    IncludesDinner = false,
                    TransportationSummary = "Xe du lịch",
                    Items =
                    {
                        new ItemSeed { SortOrder = 1, Type = TourItineraryItemType.Meal, Title = "Ăn sáng tại khách sạn", StartTime = new TimeOnly(7,0), EndTime = new TimeOnly(8,0), IncludesMeal = true },
                        new ItemSeed { SortOrder = 2, Type = TourItineraryItemType.FreeTime, Title = "Mua sắm đặc sản", StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,30), LocationName = "Đà Nẵng" },
                        new ItemSeed { SortOrder = 3, Type = TourItineraryItemType.Transfer, Title = "Tiễn khách sân bay / trung tâm", StartTime = new TimeOnly(11,0), EndTime = new TimeOnly(13,0), TransportationMode = "Xe du lịch" }
                    }
                }
            },
            ct);

        var addonIds = await ReplaceAddonsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourAddon
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:addon:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "SINGLE",
                    Name = "Phụ thu phòng đơn",
                    Type = TourAddonType.SingleSupplement,
                    ShortDescription = "Phụ thu dành cho khách ở phòng riêng.",
                    CurrencyCode = "VND",
                    BasePrice = 950000,
                    OriginalPrice = 1200000,
                    IsPerPerson = true,
                    IsRequired = false,
                    AllowQuantitySelection = false,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    IsDefaultSelected = false,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourAddon
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:addon:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "AIRPORT-PICKUP",
                    Name = "Đón sân bay ngoài khung giờ",
                    Type = TourAddonType.Transfer,
                    ShortDescription = "Xe riêng đón sân bay ngoài khung giờ miễn phí.",
                    CurrencyCode = "VND",
                    BasePrice = 250000,
                    OriginalPrice = null,
                    IsPerPerson = false,
                    IsRequired = false,
                    AllowQuantitySelection = false,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    IsDefaultSelected = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceSchedulesAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new ScheduleSeed
                {
                    Code = "SCH-01",
                    Name = "Khởi hành tuần 1",
                    DepartureDate = today.AddDays(7),
                    ReturnDate = today.AddDays(9),
                    DepartureTime = new TimeOnly(7, 30),
                    ReturnTime = new TimeOnly(18, 30),
                    BookingOpenAt = now.AddDays(-1),
                    BookingCutoffAt = now.AddDays(5),
                    MeetingPointSummary = "Sân bay Đà Nẵng",
                    PickupSummary = "Đón sân bay + trung tâm",
                    DropoffSummary = "Trả trung tâm Đà Nẵng",
                    Notes = "Lịch khởi hành chuẩn",
                    Status = TourScheduleStatus.Open,
                    IsGuaranteedDeparture = true,
                    IsInstantConfirm = false,
                    IsFeatured = true,
                    MinGuestsToOperate = 10,
                    MaxGuests = 30,
                    Capacity = new TourScheduleCapacity
                    {
                        TotalSlots = 30,
                        SoldSlots = 8,
                        HeldSlots = 2,
                        BlockedSlots = 0,
                        MinGuestsToOperate = 10,
                        MaxGuestsPerBooking = 8,
                        WarningThreshold = 5,
                        Status = TourCapacityStatus.Open,
                        AllowWaitlist = true,
                        AutoCloseWhenFull = true,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    Prices =
                    {
                        new TourSchedulePrice { PriceType = TourPriceType.Adult, CurrencyCode = "VND", Price = 3990000, OriginalPrice = 4490000, MinAge = 11, IsDefault = true, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Child, CurrencyCode = "VND", Price = 2990000, OriginalPrice = 3290000, MinAge = 5, MaxAge = 10, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Baby, CurrencyCode = "VND", Price = 590000, OriginalPrice = 690000, MaxAge = 4, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now }
                    },
                    AddonPrices =
                    {
                        new AddonPriceSeed { AddonCode = "SINGLE", Price = 900000, OriginalPrice = 950000, CurrencyCode = "VND", IsRequired = false, IsDefault = false },
                        new AddonPriceSeed { AddonCode = "AIRPORT-PICKUP", Price = 250000, OriginalPrice = null, CurrencyCode = "VND", IsRequired = false, IsDefault = false }
                    }
                },
                new ScheduleSeed
                {
                    Code = "SCH-02",
                    Name = "Khởi hành tuần 2",
                    DepartureDate = today.AddDays(14),
                    ReturnDate = today.AddDays(16),
                    DepartureTime = new TimeOnly(7, 30),
                    ReturnTime = new TimeOnly(18, 30),
                    BookingOpenAt = now.AddDays(-1),
                    BookingCutoffAt = now.AddDays(12),
                    MeetingPointSummary = "Sân bay Đà Nẵng",
                    PickupSummary = "Đón sân bay + trung tâm",
                    DropoffSummary = "Trả trung tâm Đà Nẵng",
                    Notes = "Đoàn cuối tuần",
                    Status = TourScheduleStatus.Open,
                    IsGuaranteedDeparture = false,
                    IsInstantConfirm = false,
                    IsFeatured = false,
                    MinGuestsToOperate = 12,
                    MaxGuests = 30,
                    Capacity = new TourScheduleCapacity
                    {
                        TotalSlots = 30,
                        SoldSlots = 4,
                        HeldSlots = 1,
                        BlockedSlots = 0,
                        MinGuestsToOperate = 12,
                        MaxGuestsPerBooking = 8,
                        WarningThreshold = 5,
                        Status = TourCapacityStatus.Open,
                        AllowWaitlist = true,
                        AutoCloseWhenFull = true,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    Prices =
                    {
                        new TourSchedulePrice { PriceType = TourPriceType.Adult, CurrencyCode = "VND", Price = 3890000, OriginalPrice = 4290000, MinAge = 11, IsDefault = true, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Child, CurrencyCode = "VND", Price = 2890000, OriginalPrice = 3190000, MinAge = 5, MaxAge = 10, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Baby, CurrencyCode = "VND", Price = 590000, OriginalPrice = 690000, MaxAge = 4, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now }
                    },
                    AddonPrices =
                    {
                        new AddonPriceSeed { AddonCode = "SINGLE", Price = 950000, OriginalPrice = 1100000, CurrencyCode = "VND", IsRequired = false, IsDefault = false },
                        new AddonPriceSeed { AddonCode = "AIRPORT-PICKUP", Price = 250000, OriginalPrice = null, CurrencyCode = "VND", IsRequired = false, IsDefault = false }
                    }
                },
                new ScheduleSeed
                {
                    Code = "SCH-03",
                    Name = "Khởi hành tháng sau",
                    DepartureDate = today.AddDays(30),
                    ReturnDate = today.AddDays(32),
                    DepartureTime = new TimeOnly(7, 30),
                    ReturnTime = new TimeOnly(18, 30),
                    BookingOpenAt = now.AddDays(-1),
                    BookingCutoffAt = now.AddDays(28),
                    MeetingPointSummary = "Sân bay Đà Nẵng",
                    PickupSummary = "Đón sân bay + trung tâm",
                    DropoffSummary = "Trả trung tâm Đà Nẵng",
                    Notes = "Lịch khởi hành sớm ưu đãi",
                    Status = TourScheduleStatus.Open,
                    IsGuaranteedDeparture = false,
                    IsInstantConfirm = false,
                    IsFeatured = false,
                    MinGuestsToOperate = 10,
                    MaxGuests = 30,
                    Capacity = new TourScheduleCapacity
                    {
                        TotalSlots = 30,
                        SoldSlots = 1,
                        HeldSlots = 0,
                        BlockedSlots = 0,
                        MinGuestsToOperate = 10,
                        MaxGuestsPerBooking = 8,
                        WarningThreshold = 5,
                        Status = TourCapacityStatus.Open,
                        AllowWaitlist = true,
                        AutoCloseWhenFull = true,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    Prices =
                    {
                        new TourSchedulePrice { PriceType = TourPriceType.Adult, CurrencyCode = "VND", Price = 3790000, OriginalPrice = 4290000, MinAge = 11, IsDefault = true, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Child, CurrencyCode = "VND", Price = 2790000, OriginalPrice = 3090000, MinAge = 5, MaxAge = 10, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Baby, CurrencyCode = "VND", Price = 590000, OriginalPrice = 690000, MaxAge = 4, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now }
                    },
                    AddonPrices =
                    {
                        new AddonPriceSeed { AddonCode = "SINGLE", Price = 900000, OriginalPrice = 950000, CurrencyCode = "VND", IsRequired = false, IsDefault = false },
                        new AddonPriceSeed { AddonCode = "AIRPORT-PICKUP", Price = 200000, OriginalPrice = 250000, CurrencyCode = "VND", IsRequired = false, IsDefault = false }
                    }
                }
            },
            addonIds,
            ct);
    }

    private static async Task SeedTour2ChildrenAsync(
        AppDbContext db,
        Guid tenantId,
        string tenantCode,
        Tour tour,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await ReplaceTourImagesAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourImage
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:image:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1200&auto=format&fit=crop",
                    Caption = "Biển Phú Quốc",
                    AltText = "Biển Phú Quốc",
                    Title = "Biển Phú Quốc",
                    IsPrimary = true,
                    IsCover = true,
                    IsFeatured = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourImage
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:image:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1519046904884-53103b34b206?q=80&w=1200&auto=format&fit=crop",
                    Caption = "Bãi Sao",
                    AltText = "Bãi Sao",
                    Title = "Bãi Sao",
                    IsPrimary = false,
                    IsCover = false,
                    IsFeatured = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourContactsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourContact
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:contact:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Name = "Tư vấn tour Phú Quốc",
                    Title = "Chuyên viên tư vấn",
                    Department = "Sales",
                    Phone = "0902-333-222",
                    Email = $"phuquoc-sales-{tenantCode.ToLowerInvariant()}@demo.local",
                    ContactType = TourContactType.Sales,
                    IsPrimary = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourPoliciesAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourPolicy
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:policy:general"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "GENERAL",
                    Name = "Điều khoản chung",
                    Type = TourPolicyType.General,
                    ShortDescription = "Điều khoản áp dụng tour nghỉ dưỡng biển.",
                    DescriptionMarkdown = "Khách cần mang CCCD/Hộ chiếu hợp lệ và tuân thủ giờ tập trung.",
                    DescriptionHtml = "<p>Khách cần mang CCCD/Hộ chiếu hợp lệ và tuân thủ giờ tập trung.</p>",
                    PolicyJson = """{"document":"CCCD/Hộ chiếu"}""",
                    IsHighlighted = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourPolicy
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:policy:cancellation"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "CANCEL",
                    Name = "Chính sách hủy",
                    Type = TourPolicyType.Cancellation,
                    ShortDescription = "Phí hủy phụ thuộc thời điểm gần ngày khởi hành.",
                    DescriptionMarkdown = "- Trước 10 ngày: miễn phí\n- 5-9 ngày: phí 40%\n- 2-4 ngày: phí 70%\n- 0-1 ngày: 100%",
                    DescriptionHtml = "<ul><li>Trước 10 ngày: miễn phí</li><li>5-9 ngày: 40%</li><li>2-4 ngày: 70%</li><li>0-1 ngày: 100%</li></ul>",
                    PolicyJson = """{"rules":[{"fromDay":10,"feePercent":0},{"fromDay":5,"toDay":9,"feePercent":40},{"fromDay":2,"toDay":4,"feePercent":70},{"toDay":1,"feePercent":100}]}""",
                    IsHighlighted = true,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourFaqsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourFaq
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:faq:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Question = "Tour có bao gồm vé cáp treo Hòn Thơm không?",
                    AnswerMarkdown = "Có, vé cáp treo Hòn Thơm đã bao gồm trong giá cơ bản của tour demo này.",
                    AnswerHtml = "<p>Có, vé cáp treo Hòn Thơm đã bao gồm trong giá cơ bản của tour demo này.</p>",
                    Type = TourFaqType.General,
                    IsHighlighted = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourFaq
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:faq:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Question = "Có hỗ trợ người lớn tuổi không?",
                    AnswerMarkdown = "Tour phù hợp người lớn tuổi nếu sức khỏe ổn định. Một số điểm tham quan có di chuyển bộ vừa phải.",
                    AnswerHtml = "<p>Tour phù hợp người lớn tuổi nếu sức khỏe ổn định. Một số điểm tham quan có di chuyển bộ vừa phải.</p>",
                    Type = TourFaqType.HealthSafety,
                    IsHighlighted = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceTourReviewsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourReview
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:review:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Rating = 4.9m,
                    Title = "Nghỉ dưỡng thoải mái",
                    Content = "Lịch trình đẹp, resort ổn, điểm vui chơi phù hợp gia đình.",
                    ReviewerName = "Lê Khánh Vân",
                    Status = TourReviewStatus.Approved,
                    IsApproved = true,
                    IsPublic = true,
                    ReplyContent = "Cảm ơn anh/chị đã lựa chọn tour.",
                    ReplyAt = now.AddDays(-8),
                    PublishedAt = now.AddDays(-9),
                    ApprovedAt = now.AddDays(-9),
                    IsDeleted = false,
                    CreatedAt = now.AddDays(-10),
                    UpdatedAt = now.AddDays(-8)
                }
            },
            ct);

        await ReplacePickupPointsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourPickupPoint
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:pickup:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "PQC-AIRPORT",
                    Name = "Sân bay Phú Quốc",
                    AddressLine = "Cảng hàng không Phú Quốc",
                    Province = "Kiên Giang",
                    CountryCode = "VN",
                    PickupTime = new TimeOnly(9, 0),
                    IsDefault = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceDropoffPointsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourDropoffPoint
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:dropoff:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "PQC-AIRPORT",
                    Name = "Sân bay Phú Quốc",
                    AddressLine = "Cảng hàng không Phú Quốc",
                    Province = "Kiên Giang",
                    CountryCode = "VN",
                    DropoffTime = new TimeOnly(16, 0),
                    IsDefault = true,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceItineraryAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new DaySeed
                {
                    DayNumber = 1,
                    Title = "Đón khách - Grand World",
                    StartLocation = "Phú Quốc",
                    EndLocation = "Phú Quốc",
                    AccommodationName = "Resort/Hotel 4 sao",
                    IncludesBreakfast = false,
                    IncludesLunch = true,
                    IncludesDinner = false,
                    TransportationSummary = "Xe du lịch",
                    Items =
                    {
                        new ItemSeed { SortOrder = 1, Type = TourItineraryItemType.Transfer, Title = "Đón khách sân bay", StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,0), TransportationMode = "Xe du lịch" },
                        new ItemSeed { SortOrder = 2, Type = TourItineraryItemType.Meal, Title = "Ăn trưa đặc sản", StartTime = new TimeOnly(11,30), EndTime = new TimeOnly(12,30), IncludesMeal = true },
                        new ItemSeed { SortOrder = 3, Type = TourItineraryItemType.Sightseeing, Title = "Grand World", StartTime = new TimeOnly(14,0), EndTime = new TimeOnly(18,0), IncludesTicket = true }
                    }
                },
                new DaySeed
                {
                    DayNumber = 2,
                    Title = "Cáp treo Hòn Thơm - Bãi Sao",
                    StartLocation = "Phú Quốc",
                    EndLocation = "Phú Quốc",
                    AccommodationName = "Resort/Hotel 4 sao",
                    IncludesBreakfast = true,
                    IncludesLunch = true,
                    IncludesDinner = false,
                    TransportationSummary = "Xe du lịch + cáp treo",
                    Items =
                    {
                        new ItemSeed { SortOrder = 1, Type = TourItineraryItemType.Meal, Title = "Ăn sáng", StartTime = new TimeOnly(7,0), EndTime = new TimeOnly(8,0), IncludesMeal = true },
                        new ItemSeed { SortOrder = 2, Type = TourItineraryItemType.Sightseeing, Title = "Cáp treo Hòn Thơm", StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(12,0), IncludesTicket = true },
                        new ItemSeed { SortOrder = 3, Type = TourItineraryItemType.Meal, Title = "Ăn trưa", StartTime = new TimeOnly(12,15), EndTime = new TimeOnly(13,15), IncludesMeal = true },
                        new ItemSeed { SortOrder = 4, Type = TourItineraryItemType.FreeTime, Title = "Tắm biển Bãi Sao", StartTime = new TimeOnly(14,0), EndTime = new TimeOnly(16,30) }
                    }
                },
                new DaySeed
                {
                    DayNumber = 3,
                    Title = "Tự do nghỉ dưỡng / tham quan lựa chọn",
                    StartLocation = "Phú Quốc",
                    EndLocation = "Phú Quốc",
                    AccommodationName = "Resort/Hotel 4 sao",
                    IncludesBreakfast = true,
                    IncludesLunch = false,
                    IncludesDinner = false,
                    TransportationSummary = "Tự do",
                    Items =
                    {
                        new ItemSeed { SortOrder = 1, Type = TourItineraryItemType.Meal, Title = "Ăn sáng", StartTime = new TimeOnly(7,0), EndTime = new TimeOnly(8,0), IncludesMeal = true },
                        new ItemSeed { SortOrder = 2, Type = TourItineraryItemType.FreeTime, Title = "Tự do vui chơi / nghỉ dưỡng", StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(16,0), IsOptional = true }
                    }
                },
                new DaySeed
                {
                    DayNumber = 4,
                    Title = "Mua sắm - tiễn khách",
                    StartLocation = "Phú Quốc",
                    EndLocation = "Phú Quốc",
                    AccommodationName = null,
                    IncludesBreakfast = true,
                    IncludesLunch = false,
                    IncludesDinner = false,
                    TransportationSummary = "Xe du lịch",
                    Items =
                    {
                        new ItemSeed { SortOrder = 1, Type = TourItineraryItemType.Meal, Title = "Ăn sáng", StartTime = new TimeOnly(7,0), EndTime = new TimeOnly(8,0), IncludesMeal = true },
                        new ItemSeed { SortOrder = 2, Type = TourItineraryItemType.FreeTime, Title = "Mua sắm đặc sản", StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,30) },
                        new ItemSeed { SortOrder = 3, Type = TourItineraryItemType.Transfer, Title = "Tiễn sân bay", StartTime = new TimeOnly(11,0), EndTime = new TimeOnly(13,0), TransportationMode = "Xe du lịch" }
                    }
                }
            },
            ct);

        var addonIds = await ReplaceAddonsAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new TourAddon
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:addon:1"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "SINGLE",
                    Name = "Phụ thu phòng đơn",
                    Type = TourAddonType.SingleSupplement,
                    ShortDescription = "Phòng riêng cho khách có nhu cầu.",
                    CurrencyCode = "VND",
                    BasePrice = 1800000,
                    OriginalPrice = 2200000,
                    IsPerPerson = true,
                    IsRequired = false,
                    AllowQuantitySelection = false,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    IsDefaultSelected = false,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new TourAddon
                {
                    Id = StableGuid(tenantId, $"{tour.Code}:addon:2"),
                    TenantId = tenantId,
                    TourId = tour.Id,
                    Code = "CANOE-3ISLAND",
                    Name = "Cano 3 đảo tự chọn",
                    Type = TourAddonType.ExtraService,
                    ShortDescription = "Gói vui chơi tự chọn bằng cano.",
                    CurrencyCode = "VND",
                    BasePrice = 790000,
                    OriginalPrice = 890000,
                    IsPerPerson = true,
                    IsRequired = false,
                    AllowQuantitySelection = false,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    IsDefaultSelected = false,
                    SortOrder = 2,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ct);

        await ReplaceSchedulesAsync(
            db,
            tenantId,
            tour.Id,
            now,
            new[]
            {
                new ScheduleSeed
                {
                    Code = "SCH-01",
                    Name = "Khởi hành đầu tháng",
                    DepartureDate = today.AddDays(10),
                    ReturnDate = today.AddDays(13),
                    DepartureTime = new TimeOnly(9, 0),
                    ReturnTime = new TimeOnly(16, 0),
                    BookingOpenAt = now.AddDays(-1),
                    BookingCutoffAt = now.AddDays(8),
                    MeetingPointSummary = "Sân bay Phú Quốc",
                    PickupSummary = "Đón sân bay",
                    DropoffSummary = "Trả sân bay",
                    Notes = "Tour nghỉ dưỡng tiêu chuẩn",
                    Status = TourScheduleStatus.Open,
                    IsGuaranteedDeparture = true,
                    IsInstantConfirm = false,
                    IsFeatured = true,
                    MinGuestsToOperate = 8,
                    MaxGuests = 25,
                    Capacity = new TourScheduleCapacity
                    {
                        TotalSlots = 25,
                        SoldSlots = 6,
                        HeldSlots = 1,
                        BlockedSlots = 0,
                        MinGuestsToOperate = 8,
                        MaxGuestsPerBooking = 6,
                        WarningThreshold = 4,
                        Status = TourCapacityStatus.Open,
                        AllowWaitlist = true,
                        AutoCloseWhenFull = true,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    Prices =
                    {
                        new TourSchedulePrice { PriceType = TourPriceType.Adult, CurrencyCode = "VND", Price = 6990000, OriginalPrice = 7590000, MinAge = 11, IsDefault = true, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Child, CurrencyCode = "VND", Price = 5390000, OriginalPrice = 5790000, MinAge = 5, MaxAge = 10, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Baby, CurrencyCode = "VND", Price = 990000, OriginalPrice = 1190000, MaxAge = 4, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now }
                    },
                    AddonPrices =
                    {
                        new AddonPriceSeed { AddonCode = "SINGLE", Price = 1800000, OriginalPrice = 2200000, CurrencyCode = "VND", IsRequired = false, IsDefault = false },
                        new AddonPriceSeed { AddonCode = "CANOE-3ISLAND", Price = 750000, OriginalPrice = 850000, CurrencyCode = "VND", IsRequired = false, IsDefault = false }
                    }
                },
                new ScheduleSeed
                {
                    Code = "SCH-02",
                    Name = "Khởi hành giữa tháng",
                    DepartureDate = today.AddDays(24),
                    ReturnDate = today.AddDays(27),
                    DepartureTime = new TimeOnly(9, 0),
                    ReturnTime = new TimeOnly(16, 0),
                    BookingOpenAt = now.AddDays(-1),
                    BookingCutoffAt = now.AddDays(22),
                    MeetingPointSummary = "Sân bay Phú Quốc",
                    PickupSummary = "Đón sân bay",
                    DropoffSummary = "Trả sân bay",
                    Notes = "Tour cuối tuần",
                    Status = TourScheduleStatus.Open,
                    IsGuaranteedDeparture = false,
                    IsInstantConfirm = false,
                    IsFeatured = false,
                    MinGuestsToOperate = 8,
                    MaxGuests = 25,
                    Capacity = new TourScheduleCapacity
                    {
                        TotalSlots = 25,
                        SoldSlots = 3,
                        HeldSlots = 0,
                        BlockedSlots = 0,
                        MinGuestsToOperate = 8,
                        MaxGuestsPerBooking = 6,
                        WarningThreshold = 4,
                        Status = TourCapacityStatus.Open,
                        AllowWaitlist = true,
                        AutoCloseWhenFull = true,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    Prices =
                    {
                        new TourSchedulePrice { PriceType = TourPriceType.Adult, CurrencyCode = "VND", Price = 6890000, OriginalPrice = 7490000, MinAge = 11, IsDefault = true, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Child, CurrencyCode = "VND", Price = 5290000, OriginalPrice = 5690000, MinAge = 5, MaxAge = 10, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
                        new TourSchedulePrice { PriceType = TourPriceType.Baby, CurrencyCode = "VND", Price = 990000, OriginalPrice = 1190000, MaxAge = 4, IsDefault = false, IsIncludedTax = true, IsIncludedFee = true, IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now }
                    },
                    AddonPrices =
                    {
                        new AddonPriceSeed { AddonCode = "SINGLE", Price = 1800000, OriginalPrice = 2200000, CurrencyCode = "VND", IsRequired = false, IsDefault = false },
                        new AddonPriceSeed { AddonCode = "CANOE-3ISLAND", Price = 790000, OriginalPrice = 890000, CurrencyCode = "VND", IsRequired = false, IsDefault = false }
                    }
                }
            },
            addonIds,
            ct);
    }

    private static async Task ReplaceTourImagesAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourImage> rows,
        CancellationToken ct)
    {
        var old = await db.TourImages.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .ToListAsync(ct);

        if (old.Count > 0)
            db.TourImages.RemoveRange(old);

        db.TourImages.AddRange(rows);

        await db.SaveChangesAsync(ct);
    }

    private static async Task ReplaceTourContactsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourContact> rows,
        CancellationToken ct)
    {
        var old = await db.TourContacts.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .ToListAsync(ct);

        if (old.Count > 0)
            db.TourContacts.RemoveRange(old);

        db.TourContacts.AddRange(rows);

        await db.SaveChangesAsync(ct);
    }

    private static async Task ReplaceTourPoliciesAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourPolicy> rows,
        CancellationToken ct)
    {
        var old = await db.TourPolicies.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .ToListAsync(ct);

        if (old.Count > 0)
            db.TourPolicies.RemoveRange(old);

        db.TourPolicies.AddRange(rows);

        await db.SaveChangesAsync(ct);
    }

    private static async Task ReplaceTourFaqsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourFaq> rows,
        CancellationToken ct)
    {
        var old = await db.TourFaqs.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .ToListAsync(ct);

        if (old.Count > 0)
            db.TourFaqs.RemoveRange(old);

        db.TourFaqs.AddRange(rows);

        await db.SaveChangesAsync(ct);
    }

    private static async Task ReplaceTourReviewsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourReview> rows,
        CancellationToken ct)
    {
        var old = await db.TourReviews.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .ToListAsync(ct);

        if (old.Count > 0)
            db.TourReviews.RemoveRange(old);

        db.TourReviews.AddRange(rows);

        await db.SaveChangesAsync(ct);
    }

    private static async Task ReplacePickupPointsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourPickupPoint> rows,
        CancellationToken ct)
    {
        var old = await db.TourPickupPoints.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .ToListAsync(ct);

        if (old.Count > 0)
            db.TourPickupPoints.RemoveRange(old);

        db.TourPickupPoints.AddRange(rows);

        await db.SaveChangesAsync(ct);
    }

    private static async Task ReplaceDropoffPointsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourDropoffPoint> rows,
        CancellationToken ct)
    {
        var old = await db.TourDropoffPoints.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .ToListAsync(ct);

        if (old.Count > 0)
            db.TourDropoffPoints.RemoveRange(old);

        db.TourDropoffPoints.AddRange(rows);

        await db.SaveChangesAsync(ct);
    }

    private static async Task ReplaceItineraryAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<DaySeed> days,
        CancellationToken ct)
    {
        var dayIds = await db.TourItineraryDays.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (dayIds.Count > 0)
        {
            var oldItems = await db.TourItineraryItems.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && dayIds.Contains(x.TourItineraryDayId))
                .ToListAsync(ct);

            if (oldItems.Count > 0)
                db.TourItineraryItems.RemoveRange(oldItems);

            var oldDays = await db.TourItineraryDays.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId)
                .ToListAsync(ct);

            if (oldDays.Count > 0)
                db.TourItineraryDays.RemoveRange(oldDays);

            await db.SaveChangesAsync(ct);
        }

        foreach (var day in days.OrderBy(x => x.DayNumber))
        {
            var dayId = StableGuid(tenantId, $"tour:{tourId}:day:{day.DayNumber}");

            var entity = new TourItineraryDay
            {
                Id = dayId,
                TenantId = tenantId,
                TourId = tourId,
                DayNumber = day.DayNumber,
                Title = day.Title,
                ShortDescription = day.ShortDescription,
                DescriptionMarkdown = day.DescriptionMarkdown,
                DescriptionHtml = day.DescriptionHtml,
                StartLocation = day.StartLocation,
                EndLocation = day.EndLocation,
                AccommodationName = day.AccommodationName,
                IncludesBreakfast = day.IncludesBreakfast,
                IncludesLunch = day.IncludesLunch,
                IncludesDinner = day.IncludesDinner,
                TransportationSummary = day.TransportationSummary,
                Notes = day.Notes,
                MetadataJson = day.MetadataJson,
                SortOrder = day.DayNumber,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.TourItineraryDays.Add(entity);

            foreach (var item in day.Items.OrderBy(x => x.SortOrder))
            {
                db.TourItineraryItems.Add(new TourItineraryItem
                {
                    Id = StableGuid(tenantId, $"tour:{tourId}:day:{day.DayNumber}:item:{item.SortOrder}:{item.Title}"),
                    TenantId = tenantId,
                    TourItineraryDayId = dayId,
                    Type = item.Type,
                    Title = item.Title,
                    ShortDescription = item.ShortDescription,
                    DescriptionMarkdown = item.DescriptionMarkdown,
                    DescriptionHtml = item.DescriptionHtml,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    LocationName = item.LocationName,
                    AddressLine = item.AddressLine,
                    TransportationMode = item.TransportationMode,
                    IncludesTicket = item.IncludesTicket,
                    IncludesMeal = item.IncludesMeal,
                    IsOptional = item.IsOptional,
                    RequiresAdditionalFee = item.RequiresAdditionalFee,
                    Notes = item.Notes,
                    MetadataJson = item.MetadataJson,
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<string, Guid>> ReplaceAddonsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<TourAddon> rows,
        CancellationToken ct)
    {
        var addonIds = await db.TourAddons.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (addonIds.Count > 0)
        {
            var oldAddonPrices = await db.TourScheduleAddonPrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && addonIds.Contains(x.TourAddonId))
                .ToListAsync(ct);

            if (oldAddonPrices.Count > 0)
                db.TourScheduleAddonPrices.RemoveRange(oldAddonPrices);

            var oldAddons = await db.TourAddons.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId)
                .ToListAsync(ct);

            if (oldAddons.Count > 0)
                db.TourAddons.RemoveRange(oldAddons);

            await db.SaveChangesAsync(ct);
        }

        var list = rows.ToList();
        db.TourAddons.AddRange(list);
        await db.SaveChangesAsync(ct);

        return list.ToDictionary(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task ReplaceSchedulesAsync(
        AppDbContext db,
        Guid tenantId,
        Guid tourId,
        DateTimeOffset now,
        IEnumerable<ScheduleSeed> seeds,
        Dictionary<string, Guid> addonIds,
        CancellationToken ct)
    {
        var scheduleIds = await db.TourSchedules.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (scheduleIds.Count > 0)
        {
            var oldAddonPrices = await db.TourScheduleAddonPrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && scheduleIds.Contains(x.TourScheduleId))
                .ToListAsync(ct);

            if (oldAddonPrices.Count > 0)
                db.TourScheduleAddonPrices.RemoveRange(oldAddonPrices);

            var oldPrices = await db.TourSchedulePrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && scheduleIds.Contains(x.TourScheduleId))
                .ToListAsync(ct);

            if (oldPrices.Count > 0)
                db.TourSchedulePrices.RemoveRange(oldPrices);

            var oldCaps = await db.TourScheduleCapacities.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && scheduleIds.Contains(x.TourScheduleId))
                .ToListAsync(ct);

            if (oldCaps.Count > 0)
                db.TourScheduleCapacities.RemoveRange(oldCaps);

            var oldSchedules = await db.TourSchedules.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId)
                .ToListAsync(ct);

            if (oldSchedules.Count > 0)
                db.TourSchedules.RemoveRange(oldSchedules);

            await db.SaveChangesAsync(ct);
        }

        foreach (var seed in seeds)
        {
            var scheduleId = StableGuid(tenantId, $"tour:{tourId}:schedule:{seed.Code}");

            var schedule = new TourSchedule
            {
                Id = scheduleId,
                TenantId = tenantId,
                TourId = tourId,
                Code = seed.Code,
                Name = seed.Name,
                DepartureDate = seed.DepartureDate,
                ReturnDate = seed.ReturnDate,
                DepartureTime = seed.DepartureTime,
                ReturnTime = seed.ReturnTime,
                BookingOpenAt = seed.BookingOpenAt,
                BookingCutoffAt = seed.BookingCutoffAt,
                MeetingPointSummary = seed.MeetingPointSummary,
                PickupSummary = seed.PickupSummary,
                DropoffSummary = seed.DropoffSummary,
                Notes = seed.Notes,
                InternalNotes = seed.InternalNotes,
                CancellationNotes = seed.CancellationNotes,
                MetadataJson = seed.MetadataJson,
                Status = seed.Status,
                IsGuaranteedDeparture = seed.IsGuaranteedDeparture,
                IsInstantConfirm = seed.IsInstantConfirm,
                IsFeatured = seed.IsFeatured,
                MinGuestsToOperate = seed.MinGuestsToOperate,
                MaxGuests = seed.MaxGuests,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.TourSchedules.Add(schedule);

            seed.Capacity.Id = StableGuid(tenantId, $"tour:{tourId}:schedule:{seed.Code}:capacity");
            seed.Capacity.TenantId = tenantId;
            seed.Capacity.TourScheduleId = scheduleId;
            db.TourScheduleCapacities.Add(seed.Capacity);

            var priceIndex = 0;
            foreach (var price in seed.Prices)
            {
                priceIndex++;
                price.Id = StableGuid(tenantId, $"tour:{tourId}:schedule:{seed.Code}:price:{price.PriceType}:{priceIndex}");
                price.TenantId = tenantId;
                price.TourScheduleId = scheduleId;
                db.TourSchedulePrices.Add(price);
            }

            var addonIndex = 0;
            foreach (var addon in seed.AddonPrices)
            {
                addonIndex++;
                if (!addonIds.TryGetValue(addon.AddonCode, out var addonId))
                    continue;

                db.TourScheduleAddonPrices.Add(new TourScheduleAddonPrice
                {
                    Id = StableGuid(tenantId, $"tour:{tourId}:schedule:{seed.Code}:addon:{addon.AddonCode}:{addonIndex}"),
                    TenantId = tenantId,
                    TourScheduleId = scheduleId,
                    TourAddonId = addonId,
                    CurrencyCode = string.IsNullOrWhiteSpace(addon.CurrencyCode) ? "VND" : addon.CurrencyCode,
                    Price = addon.Price,
                    OriginalPrice = addon.OriginalPrice,
                    IsPerPerson = addon.IsPerPerson,
                    IsRequired = addon.IsRequired,
                    IsDefaultSelected = addon.IsDefault,
                    AllowQuantitySelection = addon.AllowQuantitySelection,
                    MinQuantity = addon.MinQuantity,
                    MaxQuantity = addon.MaxQuantity,
                    Notes = addon.Notes,
                    MetadataJson = addon.MetadataJson,
                    SortOrder = addonIndex,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static object ResolveTenantContext(IServiceProvider sp)
    {
        var service = sp.GetService<ITenantContext>();
        if (service is null)
            throw new InvalidOperationException("ITenantContext service not found. TourDemoSeed requires tenant context to satisfy tenant-owned writes.");

        return service;
    }

    private static void UseTenantContextOrThrow(object tenantContext, Guid tenantId, string tenantCode)
    {
        if (TryInvokeTenantSetter(tenantContext, tenantId, tenantCode))
            return;

        var ok = false;
        ok |= TrySetProperty(tenantContext, "TenantId", tenantId);
        ok |= TrySetProperty(tenantContext, "CurrentTenantId", tenantId);
        ok |= TrySetProperty(tenantContext, "TenantCode", tenantCode);
        ok |= TrySetProperty(tenantContext, "CurrentTenantCode", tenantCode);
        ok |= TrySetProperty(tenantContext, "HasTenant", true);
        ok |= TrySetProperty(tenantContext, "IsResolved", true);

        if (!ok)
            throw new InvalidOperationException("Unable to set tenant context for TourDemoSeed. Please update UseTenantContextOrThrow to match your ITenantContext implementation.");
    }

    private static void ClearTenantContext(object tenantContext)
    {
        if (TryInvokeTenantClear(tenantContext))
            return;

        TrySetProperty(tenantContext, "TenantId", null);
        TrySetProperty(tenantContext, "CurrentTenantId", null);
        TrySetProperty(tenantContext, "TenantCode", null);
        TrySetProperty(tenantContext, "CurrentTenantCode", null);
        TrySetProperty(tenantContext, "HasTenant", false);
        TrySetProperty(tenantContext, "IsResolved", false);
    }

    private static bool TryInvokeTenantSetter(object tenantContext, Guid tenantId, string tenantCode)
    {
        var type = tenantContext.GetType();
        var methods = type.GetMethods();

        foreach (var method in methods)
        {
            if (method.Name is not ("SetTenant" or "SetCurrentTenant" or "SwitchTenant" or "UseTenant"))
                continue;

            var args = method.GetParameters();
            try
            {
                if (args.Length == 2 && args[0].ParameterType == typeof(Guid) && args[1].ParameterType == typeof(string))
                {
                    method.Invoke(tenantContext, new object?[] { tenantId, tenantCode });
                    return true;
                }

                if (args.Length == 2 && args[0].ParameterType == typeof(Guid?) && args[1].ParameterType == typeof(string))
                {
                    method.Invoke(tenantContext, new object?[] { tenantId, tenantCode });
                    return true;
                }

                if (args.Length == 1 && args[0].ParameterType == typeof(Guid))
                {
                    method.Invoke(tenantContext, new object?[] { tenantId });
                    return true;
                }

                if (args.Length == 1 && args[0].ParameterType == typeof(Guid?))
                {
                    method.Invoke(tenantContext, new object?[] { tenantId });
                    return true;
                }
            }
            catch
            {
                // ignore and keep trying
            }
        }

        return false;
    }

    private static bool TryInvokeTenantClear(object tenantContext)
    {
        var type = tenantContext.GetType();
        var methods = type.GetMethods();

        foreach (var method in methods)
        {
            if (method.Name is not ("Clear" or "ClearTenant" or "Reset" or "ClearCurrentTenant"))
                continue;

            if (method.GetParameters().Length != 0)
                continue;

            try
            {
                method.Invoke(tenantContext, null);
                return true;
            }
            catch
            {
                // ignore
            }
        }

        return false;
    }

    private static bool TrySetProperty(object target, string propertyName, object? value)
    {
        try
        {
            var prop = target.GetType().GetProperty(propertyName);
            if (prop is null || !prop.CanWrite) return false;

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (value is null)
            {
                prop.SetValue(target, null);
                return true;
            }

            if (targetType == typeof(Guid))
            {
                if (value is Guid g)
                {
                    prop.SetValue(target, g);
                    return true;
                }
                return false;
            }

            if (targetType == typeof(string))
            {
                prop.SetValue(target, value.ToString());
                return true;
            }

            if (targetType == typeof(bool))
            {
                if (value is bool b)
                {
                    prop.SetValue(target, b);
                    return true;
                }
                return false;
            }

            prop.SetValue(target, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Guid StableGuid(Guid tenantId, string key)
    {
        using var md5 = MD5.Create();
        var input = Encoding.UTF8.GetBytes($"{tenantId:N}:{key}");
        var hash = md5.ComputeHash(input);
        return new Guid(hash);
    }

    private sealed class SeedTenantInfo
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
    }

    private sealed class DaySeed
    {
        public int DayNumber { get; set; }
        public string Title { get; set; } = "";
        public string? ShortDescription { get; set; }
        public string? DescriptionMarkdown { get; set; }
        public string? DescriptionHtml { get; set; }
        public string? StartLocation { get; set; }
        public string? EndLocation { get; set; }
        public string? AccommodationName { get; set; }
        public bool IncludesBreakfast { get; set; }
        public bool IncludesLunch { get; set; }
        public bool IncludesDinner { get; set; }
        public string? TransportationSummary { get; set; }
        public string? Notes { get; set; }
        public string? MetadataJson { get; set; }
        public List<ItemSeed> Items { get; } = new();
    }

    private sealed class ItemSeed
    {
        public int SortOrder { get; set; }
        public TourItineraryItemType Type { get; set; } = TourItineraryItemType.Activity;
        public string Title { get; set; } = "";
        public string? ShortDescription { get; set; }
        public string? DescriptionMarkdown { get; set; }
        public string? DescriptionHtml { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? LocationName { get; set; }
        public string? AddressLine { get; set; }
        public string? TransportationMode { get; set; }
        public bool IncludesTicket { get; set; }
        public bool IncludesMeal { get; set; }
        public bool IsOptional { get; set; }
        public bool RequiresAdditionalFee { get; set; }
        public string? Notes { get; set; }
        public string? MetadataJson { get; set; }
    }

    private sealed class ScheduleSeed
    {
        public string Code { get; set; } = "";
        public string? Name { get; set; }
        public DateOnly DepartureDate { get; set; }
        public DateOnly ReturnDate { get; set; }
        public TimeOnly? DepartureTime { get; set; }
        public TimeOnly? ReturnTime { get; set; }
        public DateTimeOffset? BookingOpenAt { get; set; }
        public DateTimeOffset? BookingCutoffAt { get; set; }
        public string? MeetingPointSummary { get; set; }
        public string? PickupSummary { get; set; }
        public string? DropoffSummary { get; set; }
        public string? Notes { get; set; }
        public string? InternalNotes { get; set; }
        public string? CancellationNotes { get; set; }
        public string? MetadataJson { get; set; }
        public TourScheduleStatus Status { get; set; }
        public bool IsGuaranteedDeparture { get; set; }
        public bool IsInstantConfirm { get; set; }
        public bool IsFeatured { get; set; }
        public int? MinGuestsToOperate { get; set; }
        public int? MaxGuests { get; set; }
        public TourScheduleCapacity Capacity { get; set; } = new();
        public List<TourSchedulePrice> Prices { get; } = new();
        public List<AddonPriceSeed> AddonPrices { get; } = new();
    }

    private sealed class AddonPriceSeed
    {
        public string AddonCode { get; set; } = "";
        public string CurrencyCode { get; set; } = "VND";
        public decimal? Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public bool IsPerPerson { get; set; } = true;
        public bool IsRequired { get; set; }
        public bool IsDefault { get; set; }
        public bool AllowQuantitySelection { get; set; }
        public int? MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
        public string? Notes { get; set; }
        public string? MetadataJson { get; set; }
    }
}
