/*
  Polish local defense data after reset/seed.

  Scope:
  - Keeps existing admin accounts and tenants.
  - Renames seeded business records away from Demo labels.
  - Keeps row identities/relationships intact so future API restarts remain idempotent.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRANSACTION;

-- Identity display name used by seeded customer-facing orders.
IF COL_LENGTH(N'dbo.AspNetUsers', N'FullName') IS NOT NULL
BEGIN
    UPDATE dbo.AspNetUsers
    SET FullName = N'Nguyễn Minh Anh'
    WHERE Email = N'customer@ticketbooking.local'
      AND (FullName IS NULL OR FullName = N'Customer Demo');
END

-- Catalog / bus.
UPDATE catalog.Locations
SET Name = N'Trạm dừng chân Bảo Lộc',
    NormalizedName = N'TRAM DUNG CHAN BAO LOC',
    ShortName = N'Trạm Bảo Lộc',
    AddressLine = COALESCE(NULLIF(AddressLine, N''), N'QL20, Bảo Lộc, Lâm Đồng')
WHERE Code = N'NX001-REST';

UPDATE bus.StopPoints
SET Name = N'Trạm dừng chân Bảo Lộc',
    AddressLine = N'QL20'
WHERE Name LIKE N'%Demo%' OR Name = N'Trạm dừng chân Demo';

UPDATE fleet.SeatMaps
SET Name = CASE Code
    WHEN N'NX001-SM-01' THEN N'Sơ đồ giường nằm 20 chỗ'
    WHEN N'NX001-SM-02' THEN N'Sơ đồ limousine 24 chỗ'
    ELSE Name
END
WHERE Code IN (N'NX001-SM-01', N'NX001-SM-02');

UPDATE fleet.Vehicles
SET Name = CASE Code
    WHEN N'NX001-V-01' THEN N'Xe giường nằm 34 chỗ 01'
    WHEN N'NX001-V-02' THEN N'Xe limousine 24 phòng 02'
    ELSE Name
END,
PlateNumber = CASE Code
    WHEN N'NX001-V-01' THEN N'51B-268.68'
    WHEN N'NX001-V-02' THEN N'51B-286.86'
    ELSE PlateNumber
END
WHERE Code IN (N'NX001-V-01', N'NX001-V-02');

UPDATE bus.Trips
SET Name = CASE Code
    WHEN N'NX001-TRIP-1' THEN N'Chuyến sáng'
    WHEN N'NX001-TRIP-2' THEN N'Chuyến chiều'
    ELSE REPLACE(Name, N' (Demo)', N'')
END
WHERE Code IN (N'NX001-TRIP-1', N'NX001-TRIP-2') OR Name LIKE N'%(Demo)%';

-- Train / flight.
UPDATE catalog.Providers
SET Name = N'Đường sắt Việt Nam',
    Slug = N'duong-sat-viet-nam'
WHERE Code = N'VT001-TRAIN';

UPDATE train.Trips
SET Name = REPLACE(Name, N' (Demo)', N'')
WHERE Name LIKE N'%(Demo)%';

UPDATE flight.Aircrafts
SET Name = N'Airbus A320 Vietnam Airlines'
WHERE Code = N'VNA-A320-01';

-- CMS.
UPDATE cms.NewsPosts
SET Title = REPLACE(Title, N' (Demo)', N''),
    SeoTitle = REPLACE(SeoTitle, N' (Demo)', N''),
    OgTitle = REPLACE(OgTitle, N' (Demo)', N''),
    TwitterTitle = REPLACE(TwitterTitle, N' (Demo)', N''),
    ContentMarkdown = REPLACE(ContentMarkdown, N'Đây là bài viết demo phục vụ Phase 7 CMS/SEO.', N'Đây là bài viết giới thiệu nội dung và quy trình sử dụng hệ thống.'),
    ContentHtml = REPLACE(ContentHtml, N'Đây là bài viết demo phục vụ Phase 7 CMS/SEO.', N'Đây là bài viết giới thiệu nội dung và quy trình sử dụng hệ thống.')
WHERE Title LIKE N'%(Demo)%'
   OR ContentMarkdown LIKE N'%bài viết demo%';

UPDATE cms.NewsPosts
SET ContentMarkdown = REPLACE(REPLACE(ContentMarkdown, N' (Demo)', N''), N'bài viết demo', N'bài viết'),
    ContentHtml = REPLACE(REPLACE(ContentHtml, N' (Demo)', N''), N'bài viết demo', N'bài viết')
WHERE ContentMarkdown LIKE N'%demo%' OR ContentHtml LIKE N'%demo%';

UPDATE cms.NewsPosts
SET ContentMarkdown = REPLACE(ContentMarkdown, N'demo', N'giới thiệu'),
    ContentHtml = REPLACE(ContentHtml, N'demo', N'giới thiệu')
WHERE ContentMarkdown LIKE N'%demo%' OR ContentHtml LIKE N'%demo%';

UPDATE cms.NewsPostRevisions
SET Title = REPLACE(Title, N' (Demo)', N''),
    SeoTitle = REPLACE(SeoTitle, N' (Demo)', N''),
    OgTitle = REPLACE(OgTitle, N' (Demo)', N''),
    TwitterTitle = REPLACE(TwitterTitle, N' (Demo)', N''),
    ContentMarkdown = REPLACE(ContentMarkdown, N'Đây là bài viết demo phục vụ Phase 7 CMS/SEO.', N'Đây là bài viết giới thiệu nội dung và quy trình sử dụng hệ thống.'),
    ContentHtml = REPLACE(ContentHtml, N'Đây là bài viết demo phục vụ Phase 7 CMS/SEO.', N'Đây là bài viết giới thiệu nội dung và quy trình sử dụng hệ thống.')
WHERE Title LIKE N'%(Demo)%'
   OR ContentMarkdown LIKE N'%bài viết demo%';

UPDATE cms.NewsPostRevisions
SET ContentMarkdown = REPLACE(REPLACE(ContentMarkdown, N' (Demo)', N''), N'bài viết demo', N'bài viết'),
    ContentHtml = REPLACE(REPLACE(ContentHtml, N' (Demo)', N''), N'bài viết demo', N'bài viết')
WHERE ContentMarkdown LIKE N'%demo%' OR ContentHtml LIKE N'%demo%';

UPDATE cms.NewsPostRevisions
SET ContentMarkdown = REPLACE(ContentMarkdown, N'demo', N'giới thiệu'),
    ContentHtml = REPLACE(ContentHtml, N'demo', N'giới thiệu')
WHERE ContentMarkdown LIKE N'%demo%' OR ContentHtml LIKE N'%demo%';

UPDATE cms.NewsRedirects
SET Reason = REPLACE(Reason, N' demo', N'')
WHERE Reason LIKE N'%demo%';

-- Hotels.
UPDATE hotels.Hotels
SET Code = REPLACE(Code, N'_DEMO_01', N'_MAIN_01'),
    Name = N'Khách sạn Minh Nhật ' + REPLACE(REPLACE(Code, N'HTL_', N''), N'_DEMO_01', N''),
    Slug = N'khach-san-minh-nhat-' + LOWER(REPLACE(REPLACE(Code, N'HTL_', N''), N'_DEMO_01', N'')),
    AddressLine = N'12 Nguyễn Văn Linh',
    ShortDescription = N'Khách sạn tiêu chuẩn 4 sao, phù hợp công tác và du lịch gia đình.',
    DescriptionMarkdown = N'## Khách sạn Minh Nhật' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) + N'Không gian lưu trú hiện đại, vị trí thuận tiện và dịch vụ ổn định.',
    DescriptionHtml = N'<h2>Khách sạn Minh Nhật</h2><p>Không gian lưu trú hiện đại, vị trí thuận tiện và dịch vụ ổn định.</p>',
    Email = N'hotel-' + LOWER(REPLACE(REPLACE(Code, N'HTL_', N''), N'_DEMO_01', N'')) + N'@ticketbooking.local'
WHERE Code LIKE N'HTL[_]%[_]DEMO[_]01'
   OR Name LIKE N'%Demo%'
   OR ShortDescription LIKE N'%demo%';

UPDATE hotels.RoomTypes
SET DescriptionMarkdown = N'**' + Name + N'** - phù hợp nghỉ dưỡng và công tác.',
    DescriptionHtml = N'<p><strong>' + Name + N'</strong> - phù hợp nghỉ dưỡng và công tác.</p>'
WHERE DescriptionMarkdown LIKE N'%demo room type%'
   OR DescriptionHtml LIKE N'%demo room type%';

UPDATE hotels.RatePlans
SET Description = N'Giá tốt nhất theo ngày.'
WHERE Description LIKE N'%demo%';

UPDATE hotels.ExtraServices
SET Description = N'Dịch vụ tiện ích tại khách sạn.'
WHERE Description LIKE N'%demo%';

UPDATE hotels.HotelContacts
SET ContactName = N'Bộ phận lễ tân',
    Email = N'frontdesk@ticketbooking.local'
WHERE ContactName LIKE N'%(Demo)%'
   OR Email LIKE N'%@demo.local';

UPDATE hotels.PromoRateOverrides
SET PromoCode = N'WELCOME10'
WHERE PromoCode = N'DEMO10';

UPDATE hotels.HotelReviews
SET Title = REPLACE(Title, N' (Demo)', N''),
    Content = REPLACE(REPLACE(Content, N'Khách sạn demo', N'Khách sạn'), N'khách sạn demo', N'khách sạn')
WHERE Title LIKE N'%(Demo)%'
   OR Content LIKE N'%demo%';

UPDATE hotels.HotelReviews
SET Content = N'Khách sạn sạch sẽ, vị trí thuận tiện. Sẽ quay lại!'
WHERE Content LIKE N'%demo%';

-- Commerce defense sample records.
UPDATE commerce.CustomerOrders
SET OrderCode = CASE OrderCode
        WHEN N'DEMO-PAID-001' THEN N'BK-DEF-PAID-001'
        WHEN N'DEMO-REFUND-001' THEN N'BK-DEF-REFUND-001'
        ELSE OrderCode
    END,
    ContactFullName = CASE WHEN ContactFullName = N'Customer Demo' THEN N'Nguyễn Minh Anh' ELSE ContactFullName END,
    CustomerNote = CASE WHEN CustomerNote LIKE N'%Demo%' THEN N'Dữ liệu ổn định dùng cho buổi bảo vệ dự án.' ELSE CustomerNote END,
    SnapshotJson = CASE
        WHEN OrderCode IN (N'DEMO-PAID-001', N'BK-DEF-PAID-001')
            THEN JSON_MODIFY(REPLACE(SnapshotJson, N'demoLocked', N'defenseLocked'), '$.subtitle', N'Booking đã thanh toán cho buổi bảo vệ')
        WHEN OrderCode IN (N'DEMO-REFUND-001', N'BK-DEF-REFUND-001')
            THEN JSON_MODIFY(REPLACE(SnapshotJson, N'demoLocked', N'defenseLocked'), '$.subtitle', N'Booking hoàn tiền một phần cho buổi bảo vệ')
        ELSE REPLACE(SnapshotJson, N'demoLocked', N'defenseLocked')
    END,
    MetadataJson = REPLACE(MetadataJson, N'demoLocked', N'defenseLocked')
WHERE OrderCode IN (N'DEMO-PAID-001', N'DEMO-REFUND-001', N'BK-DEF-PAID-001', N'BK-DEF-REFUND-001');

UPDATE commerce.CustomerPayments
SET PaymentCode = CASE PaymentCode
        WHEN N'PAY-DEMO-PAID-001' THEN N'PAY-DEF-PAID-001'
        WHEN N'PAY-DEMO-REFUND-001' THEN N'PAY-DEF-REFUND-001'
        ELSE PaymentCode
    END,
    ProviderInvoiceNumber = CASE ProviderInvoiceNumber
        WHEN N'INV-DEMO-PAID-001' THEN N'INV-DEF-PAID-001'
        WHEN N'INV-DEMO-REFUND-001' THEN N'INV-DEF-REFUND-001'
        ELSE ProviderInvoiceNumber
    END,
    ProviderOrderId = CASE ProviderOrderId
        WHEN N'DEMO-PAY-DEMO-PAID-001' THEN N'DEF-PAY-DEF-PAID-001'
        WHEN N'DEMO-PAY-DEMO-REFUND-001' THEN N'DEF-PAY-DEF-REFUND-001'
        ELSE REPLACE(REPLACE(ProviderOrderId, N'DEMO-', N'DEF-'), N'PAY-DEMO-', N'PAY-DEF-')
    END,
    RequestPayloadJson = REPLACE(RequestPayloadJson, N'demoLocked', N'defenseLocked'),
    ProviderResponseJson = REPLACE(ProviderResponseJson, N'demoLocked', N'defenseLocked'),
    LastWebhookJson = REPLACE(LastWebhookJson, N'demoLocked', N'defenseLocked')
WHERE PaymentCode LIKE N'%DEMO%'
   OR ProviderInvoiceNumber LIKE N'%DEMO%'
   OR ProviderOrderId LIKE N'%DEMO%';

UPDATE commerce.CustomerTickets
SET TicketCode = CASE TicketCode
        WHEN N'TKT-DEMO-PAID-001' THEN N'TKT-DEF-PAID-001'
        WHEN N'TKT-DEMO-REFUND-001' THEN N'TKT-DEF-REFUND-001'
        ELSE TicketCode
    END,
    Subtitle = CASE
        WHEN Subtitle LIKE N'%demo paid%' THEN N'Ghế A01, A02 - đã xuất vé'
        WHEN Subtitle LIKE N'%demo partial refund%' THEN N'Ghế B05, B06 - hoàn tiền một phần'
        ELSE Subtitle
    END,
    SnapshotJson = CASE
        WHEN TicketCode IN (N'TKT-DEMO-PAID-001', N'TKT-DEF-PAID-001')
            THEN JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(REPLACE(SnapshotJson, N'demoLocked', N'defenseLocked'), '$.subtitle', N'Ghế A01, A02 - đã xuất vé'), '$.qrPayload', N'TKT-DEF-PAID-001'), '$.defenseLocked', CAST(1 AS bit))
        WHEN TicketCode IN (N'TKT-DEMO-REFUND-001', N'TKT-DEF-REFUND-001')
            THEN JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(REPLACE(SnapshotJson, N'demoLocked', N'defenseLocked'), '$.subtitle', N'Ghế B05, B06 - hoàn tiền một phần'), '$.qrPayload', N'TKT-DEF-REFUND-001'), '$.defenseLocked', CAST(1 AS bit))
        ELSE REPLACE(SnapshotJson, N'demoLocked', N'defenseLocked')
    END
WHERE TicketCode LIKE N'%DEMO%'
   OR Subtitle LIKE N'%demo%'
   OR SnapshotJson LIKE N'%demo%'
   OR SnapshotJson LIKE N'%DEMO%';

UPDATE commerce.CustomerRefundRequests
SET RefundCode = CASE WHEN RefundCode = N'REF-DEMO-PARTIAL-001' THEN N'REF-DEF-PARTIAL-001' ELSE RefundCode END,
    ReasonText = CASE WHEN ReasonText LIKE N'%demo%' THEN N'Khách đổi kế hoạch, hoàn tiền một phần theo quy trình đối soát.' ELSE ReasonText END,
    RefundReference = REPLACE(RefundReference, N'DEMO-', N'DEF-'),
    ReviewNote = CASE WHEN ReviewNote LIKE N'%demo%' THEN N'Đã duyệt hoàn tiền một phần cho bộ dữ liệu bảo vệ dự án.' ELSE ReviewNote END,
    SnapshotJson = REPLACE(SnapshotJson, N'demoLocked', N'defenseLocked')
WHERE RefundCode LIKE N'%DEMO%'
   OR RefundReference LIKE N'%DEMO%'
   OR ReasonText LIKE N'%demo%'
   OR ReviewNote LIKE N'%demo%';

UPDATE commerce.CustomerSupportTickets
SET TicketCode = CASE WHEN TicketCode = N'SUP-DEMO-001' THEN N'SUP-DEF-001' ELSE TicketCode END
WHERE TicketCode = N'SUP-DEMO-001';

UPDATE commerce.CustomerTenantPayoutAccounts
SET AccountHolder = CASE WHEN AccountHolder = N'NX001 Demo' THEN N'Công ty Vận tải NX001' ELSE AccountHolder END,
    Note = CASE WHEN Note LIKE N'%demo%' THEN N'Tài khoản nhận đối soát cho buổi bảo vệ dự án.' ELSE Note END
WHERE AccountHolder LIKE N'%Demo%' OR Note LIKE N'%demo%';

UPDATE commerce.CustomerSettlementBatches
SET BatchCode = CASE WHEN BatchCode = N'SET-DEMO-LOCKED-001' THEN N'SET-DEF-LOCKED-001' ELSE BatchCode END,
    Notes = N'Kỳ đối soát đã khóa cho buổi bảo vệ dự án.'
WHERE BatchCode LIKE N'%DEMO%' OR Notes LIKE N'%demo%';

UPDATE commerce.CustomerSettlementBatchLines
SET Description = REPLACE(REPLACE(REPLACE(Description,
        N'Demo paid booking payout', N'Doanh thu booking đã thanh toán'),
        N'Demo original sale before partial refund', N'Doanh thu gốc trước hoàn tiền một phần'),
        N'Demo partial refund adjustment', N'Điều chỉnh hoàn tiền một phần'),
    MetadataJson = REPLACE(MetadataJson, N'demoLocked', N'defenseLocked')
WHERE Description LIKE N'%Demo%'
   OR MetadataJson LIKE N'%demoLocked%';

COMMIT TRANSACTION;

SELECT N'catalog.Providers' AS [Table], COUNT(*) AS [RowsWithDemo]
FROM catalog.Providers WHERE Name LIKE N'%Demo%' OR Slug LIKE N'%demo%'
UNION ALL
SELECT N'fleet.Vehicles', COUNT(*) FROM fleet.Vehicles WHERE Name LIKE N'%demo%' OR PlateNumber IN (N'60A-999.99', N'60A-888.88')
UNION ALL
SELECT N'bus.Trips', COUNT(*) FROM bus.Trips WHERE Name LIKE N'%Demo%'
UNION ALL
SELECT N'train.Trips', COUNT(*) FROM train.Trips WHERE Name LIKE N'%Demo%'
UNION ALL
SELECT N'hotels.Hotels', COUNT(*) FROM hotels.Hotels WHERE Code LIKE N'%DEMO%' OR Name LIKE N'%Demo%' OR ShortDescription LIKE N'%demo%' OR Email LIKE N'%@demo.local'
UNION ALL
SELECT N'cms.NewsPosts', COUNT(*) FROM cms.NewsPosts WHERE Title LIKE N'%(Demo)%' OR ContentMarkdown LIKE N'%bài viết demo%'
UNION ALL
SELECT N'commerce.CustomerOrders', COUNT(*) FROM commerce.CustomerOrders WHERE OrderCode LIKE N'%DEMO%' OR ContactFullName = N'Customer Demo' OR CustomerNote LIKE N'%Demo%'
UNION ALL
SELECT N'commerce.CustomerPayments', COUNT(*) FROM commerce.CustomerPayments WHERE PaymentCode LIKE N'%DEMO%' OR ProviderInvoiceNumber LIKE N'%DEMO%' OR ProviderOrderId LIKE N'%DEMO%'
UNION ALL
SELECT N'commerce.CustomerTickets', COUNT(*) FROM commerce.CustomerTickets WHERE TicketCode LIKE N'%DEMO%' OR Subtitle LIKE N'%demo%'
UNION ALL
SELECT N'commerce.CustomerRefundRequests', COUNT(*) FROM commerce.CustomerRefundRequests WHERE RefundCode LIKE N'%DEMO%' OR RefundReference LIKE N'%DEMO%' OR ReasonText LIKE N'%demo%' OR ReviewNote LIKE N'%demo%';
