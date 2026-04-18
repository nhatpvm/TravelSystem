using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Domain.Commerce;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Commerce;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer/account")]
[Authorize]
public sealed class CustomerAccountController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerAccountController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<CustomerAccountPreferenceDto>> GetPreferences(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để sử dụng tính năng này." });

        var preference = await _db.CustomerAccountPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId.Value && !x.IsDeleted, ct);

        return Ok(MapPreference(preference));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<CustomerAccountPreferenceDto>> UpdatePreferences(
        [FromBody] UpdateCustomerAccountPreferenceRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để quản lý cài đặt tài khoản." });

        request ??= new UpdateCustomerAccountPreferenceRequest();

        var languageCode = NormalizeLanguageCode(request.LanguageCode);
        var currencyCode = NormalizeCurrencyCode(request.CurrencyCode);
        var themeMode = NormalizeThemeMode(request.ThemeMode);
        var now = DateTimeOffset.UtcNow;

        var preference = await _db.CustomerAccountPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId.Value && !x.IsDeleted, ct);

        if (preference is null)
        {
            preference = new CustomerAccountPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                CreatedAt = now,
                CreatedByUserId = userId.Value,
            };

            _db.CustomerAccountPreferences.Add(preference);
        }

        preference.LanguageCode = languageCode;
        preference.CurrencyCode = currencyCode;
        preference.ThemeMode = themeMode;
        preference.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        preference.SmsNotificationsEnabled = request.SmsNotificationsEnabled;
        preference.PushNotificationsEnabled = request.PushNotificationsEnabled;
        preference.UpdatedAt = now;
        preference.UpdatedByUserId = userId.Value;

        await _db.SaveChangesAsync(ct);
        return Ok(MapPreference(preference));
    }

    [HttpGet("passengers")]
    public async Task<ActionResult<List<CustomerSavedPassengerDto>>> ListPassengers(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem danh sách hành khách đã lưu." });

        var items = await _db.CustomerSavedPassengers
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.FullName)
            .Select(x => new CustomerSavedPassengerDto
            {
                Id = x.Id,
                FullName = x.FullName,
                PassengerType = x.PassengerType,
                Gender = x.Gender,
                DateOfBirth = x.DateOfBirth,
                NationalityCode = x.NationalityCode,
                IdNumber = x.IdNumber,
                PassportNumber = x.PassportNumber,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                IsDefault = x.IsDefault,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("passengers")]
    public async Task<ActionResult<CustomerSavedPassengerDto>> CreatePassenger(
        [FromBody] UpsertCustomerSavedPassengerRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để thêm hành khách đã lưu." });

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Vui lòng nhập họ và tên hành khách." });

        var passengerType = ParsePassengerType(request.PassengerType);
        var now = DateTimeOffset.UtcNow;

        if (request.IsDefault)
        {
            var existingDefaults = await _db.CustomerSavedPassengers
                .Where(x => x.UserId == userId.Value && x.IsDefault && !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
                item.UpdatedAt = now;
                item.UpdatedByUserId = userId.Value;
            }
        }

        var passenger = new CustomerSavedPassenger
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            FullName = request.FullName.Trim(),
            PassengerType = passengerType,
            Gender = Normalize(request.Gender),
            DateOfBirth = request.DateOfBirth,
            NationalityCode = Normalize(request.NationalityCode),
            IdNumber = Normalize(request.IdNumber),
            PassportNumber = Normalize(request.PassportNumber),
            Email = Normalize(request.Email),
            PhoneNumber = Normalize(request.PhoneNumber),
            IsDefault = request.IsDefault,
            Notes = Normalize(request.Notes),
            CreatedAt = now,
            CreatedByUserId = userId.Value,
        };

        _db.CustomerSavedPassengers.Add(passenger);
        await _db.SaveChangesAsync(ct);

        return Ok(MapPassenger(passenger));
    }

    [HttpPut("passengers/{id:guid}")]
    public async Task<ActionResult<CustomerSavedPassengerDto>> UpdatePassenger(
        Guid id,
        [FromBody] UpsertCustomerSavedPassengerRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để cập nhật hành khách đã lưu." });

        var passenger = await _db.CustomerSavedPassengers
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value && !x.IsDeleted, ct);

        if (passenger is null)
            return NotFound(new { message = "Không tìm thấy hồ sơ hành khách." });

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Vui lòng nhập họ và tên hành khách." });

        var now = DateTimeOffset.UtcNow;
        if (request.IsDefault)
        {
            var existingDefaults = await _db.CustomerSavedPassengers
                .Where(x => x.UserId == userId.Value && x.Id != id && x.IsDefault && !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
                item.UpdatedAt = now;
                item.UpdatedByUserId = userId.Value;
            }
        }

        passenger.FullName = request.FullName.Trim();
        passenger.PassengerType = ParsePassengerType(request.PassengerType);
        passenger.Gender = Normalize(request.Gender);
        passenger.DateOfBirth = request.DateOfBirth;
        passenger.NationalityCode = Normalize(request.NationalityCode);
        passenger.IdNumber = Normalize(request.IdNumber);
        passenger.PassportNumber = Normalize(request.PassportNumber);
        passenger.Email = Normalize(request.Email);
        passenger.PhoneNumber = Normalize(request.PhoneNumber);
        passenger.IsDefault = request.IsDefault;
        passenger.Notes = Normalize(request.Notes);
        passenger.UpdatedAt = now;
        passenger.UpdatedByUserId = userId.Value;

        await _db.SaveChangesAsync(ct);
        return Ok(MapPassenger(passenger));
    }

    [HttpDelete("passengers/{id:guid}")]
    public async Task<IActionResult> DeletePassenger(Guid id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xóa hành khách đã lưu." });

        var passenger = await _db.CustomerSavedPassengers
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value && !x.IsDeleted, ct);

        if (passenger is null)
            return NotFound(new { message = "Không tìm thấy hồ sơ hành khách." });

        passenger.IsDeleted = true;
        passenger.UpdatedAt = DateTimeOffset.UtcNow;
        passenger.UpdatedByUserId = userId.Value;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpGet("wishlist")]
    public async Task<ActionResult<List<CustomerWishlistItemDto>>> ListWishlist(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem danh sách yêu thích." });

        var items = await _db.CustomerWishlistItems
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CustomerWishlistItemDto
            {
                Id = x.Id,
                ProductType = x.ProductType,
                TargetId = x.TargetId,
                TargetSlug = x.TargetSlug,
                Title = x.Title,
                Subtitle = x.Subtitle,
                LocationText = x.LocationText,
                PriceText = x.PriceText,
                PriceValue = x.PriceValue,
                CurrencyCode = x.CurrencyCode,
                ImageUrl = x.ImageUrl,
                TargetUrl = x.TargetUrl,
                Metadata = ParseJson(x.MetadataJson),
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("wishlist")]
    public async Task<ActionResult<CustomerWishlistItemDto>> AddWishlist(
        [FromBody] UpsertCustomerWishlistRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để thêm vào danh sách yêu thích." });

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Vui lòng nhập tên mục yêu thích." });

        var productType = ParseProductType(request.ProductType);
        var targetSlug = Normalize(request.TargetSlug);
        if (!request.TargetId.HasValue && targetSlug is null)
            return BadRequest(new { message = "Vui lÃ²ng cung cáº¥p Ä‘á»‹nh danh hoáº·c slug cá»§a má»¥c yÃªu thÃ­ch." });

        var now = DateTimeOffset.UtcNow;
        var existing = await _db.CustomerWishlistItems
            .Where(x =>
                x.UserId == userId.Value &&
                x.ProductType == productType &&
                !x.IsDeleted)
            .Where(x =>
                (request.TargetId.HasValue && x.TargetId == request.TargetId) ||
                (targetSlug != null && x.TargetSlug == targetSlug))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            if (request.TargetId.HasValue)
                existing.TargetId = request.TargetId;

            existing.TargetSlug = targetSlug ?? existing.TargetSlug;
            existing.Title = request.Title.Trim();
            existing.Subtitle = Normalize(request.Subtitle);
            existing.LocationText = Normalize(request.LocationText);
            existing.PriceText = Normalize(request.PriceText);
            existing.PriceValue = request.PriceValue;
            existing.CurrencyCode = Normalize(request.CurrencyCode);
            existing.ImageUrl = Normalize(request.ImageUrl);
            existing.TargetUrl = Normalize(request.TargetUrl);
            existing.MetadataJson = Normalize(request.MetadataJson);
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = userId.Value;

            await _db.SaveChangesAsync(ct);
            return Ok(MapWishlist(existing));
        }

        var item = new CustomerWishlistItem
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            ProductType = productType,
            TargetId = request.TargetId,
            TargetSlug = targetSlug,
            Title = request.Title.Trim(),
            Subtitle = Normalize(request.Subtitle),
            LocationText = Normalize(request.LocationText),
            PriceText = Normalize(request.PriceText),
            PriceValue = request.PriceValue,
            CurrencyCode = Normalize(request.CurrencyCode),
            ImageUrl = Normalize(request.ImageUrl),
            TargetUrl = Normalize(request.TargetUrl),
            MetadataJson = Normalize(request.MetadataJson),
            CreatedAt = now,
            CreatedByUserId = userId.Value,
        };

        _db.CustomerWishlistItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return Ok(MapWishlist(item));
    }

    [HttpDelete("wishlist/{id:guid}")]
    public async Task<IActionResult> DeleteWishlist(Guid id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xóa mục yêu thích." });

        var item = await _db.CustomerWishlistItems
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value && !x.IsDeleted, ct);

        if (item is null)
            return NotFound(new { message = "Không tìm thấy mục yêu thích." });

        item.IsDeleted = true;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = userId.Value;
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<CustomerNotificationListResponse>> ListNotifications(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem thông báo." });

        var items = await _db.CustomerNotifications
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CustomerNotificationDto
            {
                Id = x.Id,
                Status = x.Status,
                Category = x.Category,
                Title = x.Title,
                Body = x.Body,
                ActionUrl = x.ActionUrl,
                ReferenceType = x.ReferenceType,
                ReferenceId = x.ReferenceId,
                Metadata = ParseJson(x.MetadataJson),
                CreatedAt = x.CreatedAt,
                ReadAt = x.ReadAt,
            })
            .ToListAsync(ct);

        return Ok(new CustomerNotificationListResponse
        {
            Total = items.Count,
            UnreadCount = items.Count(x => x.Status == CustomerNotificationStatus.Unread),
            Items = items,
        });
    }

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để cập nhật thông báo." });

        var notification = await _db.CustomerNotifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value && !x.IsDeleted, ct);

        if (notification is null)
            return NotFound(new { message = "Không tìm thấy thông báo." });

        notification.Status = CustomerNotificationStatus.Read;
        notification.ReadAt = notification.ReadAt ?? DateTimeOffset.UtcNow;
        notification.UpdatedAt = DateTimeOffset.UtcNow;
        notification.UpdatedByUserId = userId.Value;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để đánh dấu tất cả thông báo." });

        var now = DateTimeOffset.UtcNow;
        var items = await _db.CustomerNotifications
            .Where(x => x.UserId == userId.Value && x.Status == CustomerNotificationStatus.Unread && !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            item.Status = CustomerNotificationStatus.Read;
            item.ReadAt = now;
            item.UpdatedAt = now;
            item.UpdatedByUserId = userId.Value;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, updated = items.Count });
    }

    [HttpGet("payments")]
    public async Task<ActionResult<List<CustomerPaymentHistoryItemDto>>> ListPayments(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem lịch sử thanh toán." });

        var items = await (
            from payment in _db.CustomerPayments.AsNoTracking()
            join order in _db.CustomerOrders.AsNoTracking() on payment.OrderId equals order.Id
            where payment.UserId == userId.Value && !payment.IsDeleted && !order.IsDeleted
            orderby payment.CreatedAt descending
            select new
            {
                payment,
                order,
            })
            .ToListAsync(ct);

        return Ok(items.Select(x =>
        {
            var snapshot = ParseJson(x.order.SnapshotJson);
            return new CustomerPaymentHistoryItemDto
            {
                PaymentId = x.payment.Id,
                OrderId = x.order.Id,
                PaymentCode = x.payment.PaymentCode,
                OrderCode = x.order.OrderCode,
                ProductType = x.order.ProductType,
                PaymentStatus = x.payment.Status,
                OrderStatus = x.order.Status,
                CurrencyCode = x.payment.CurrencyCode,
                Amount = x.payment.Amount,
                PaidAmount = x.payment.PaidAmount,
                RefundedAmount = x.payment.RefundedAmount,
                Title = snapshot.TryGetProperty("title", out var title) ? title.GetString() ?? x.order.OrderCode : x.order.OrderCode,
                Subtitle = snapshot.TryGetProperty("subtitle", out var subtitle) ? subtitle.GetString() : null,
                CreatedAt = x.payment.CreatedAt,
                PaidAt = x.payment.PaidAt,
                ExpiresAt = x.payment.ExpiresAt,
            };
        }).ToList());
    }

    [HttpGet("vat-invoices")]
    public async Task<ActionResult<List<CustomerVatInvoiceDto>>> ListVatInvoices(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem yêu cầu hóa đơn VAT." });

        var items = await (
            from vat in _db.CustomerVatInvoiceRequests.AsNoTracking()
            join order in _db.CustomerOrders.AsNoTracking() on vat.OrderId equals order.Id
            where vat.UserId == userId.Value && !vat.IsDeleted && !order.IsDeleted
            orderby vat.CreatedAt descending
            select new CustomerVatInvoiceDto
            {
                Id = vat.Id,
                OrderId = vat.OrderId,
                OrderCode = order.OrderCode,
                RequestCode = vat.RequestCode,
                Status = vat.Status,
                CompanyName = vat.CompanyName,
                TaxCode = vat.TaxCode,
                CompanyAddress = vat.CompanyAddress,
                InvoiceEmail = vat.InvoiceEmail,
                InvoiceNumber = vat.InvoiceNumber,
                PdfUrl = vat.PdfUrl,
                CreatedAt = vat.CreatedAt,
                ProcessedAt = vat.ProcessedAt,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("vat-invoices")]
    public async Task<ActionResult<CustomerVatInvoiceDto>> CreateVatInvoice(
        [FromBody] CreateCustomerVatInvoiceRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để tạo yêu cầu hóa đơn VAT." });

        if (string.IsNullOrWhiteSpace(request.OrderCode))
            return BadRequest(new { message = "Vui lòng chọn đơn hàng cần xuất hóa đơn VAT." });

        if (string.IsNullOrWhiteSpace(request.CompanyName) ||
            string.IsNullOrWhiteSpace(request.TaxCode) ||
            string.IsNullOrWhiteSpace(request.CompanyAddress) ||
            string.IsNullOrWhiteSpace(request.InvoiceEmail))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ tên công ty, mã số thuế, địa chỉ và email nhận hóa đơn." });
        }

        var order = await _db.CustomerOrders
            .FirstOrDefaultAsync(x =>
                x.UserId == userId.Value &&
                x.OrderCode == request.OrderCode.Trim() &&
                !x.IsDeleted, ct);

        if (order is null)
            return NotFound(new { message = "Không tìm thấy đơn hàng để xuất hóa đơn VAT." });

        if (order.PaymentStatus == CustomerPaymentStatus.Pending)
            return BadRequest(new { message = "Bạn cần hoàn tất thanh toán trước khi yêu cầu xuất hóa đơn VAT." });

        var activeRequest = await _db.CustomerVatInvoiceRequests
            .AsNoTracking()
            .Where(x => x.OrderId == order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (activeRequest is not null && activeRequest.Status != CustomerVatInvoiceStatus.Rejected)
        {
            return BadRequest(new { message = "Đơn hàng này đã có yêu cầu xuất hóa đơn VAT đang xử lý hoặc đã được phát hành." });
        }

        var now = DateTimeOffset.UtcNow;
        var vat = new CustomerVatInvoiceRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            TenantId = order.TenantId,
            OrderId = order.Id,
            RequestCode = $"VAT-{now:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            Status = CustomerVatInvoiceStatus.Requested,
            CompanyName = request.CompanyName.Trim(),
            TaxCode = request.TaxCode.Trim(),
            CompanyAddress = request.CompanyAddress.Trim(),
            InvoiceEmail = request.InvoiceEmail.Trim(),
            Notes = Normalize(request.Notes),
            CreatedAt = now,
            CreatedByUserId = userId.Value,
        };

        order.VatInvoiceRequested = true;
        order.UpdatedAt = now;
        order.UpdatedByUserId = userId.Value;

        _db.CustomerVatInvoiceRequests.Add(vat);
        await _db.SaveChangesAsync(ct);

        return Ok(MapVat(vat, order.OrderCode));
    }

    [HttpGet("support-tickets")]
    public async Task<ActionResult<List<CustomerSupportTicketDto>>> ListSupportTickets(
        [FromQuery] string? ticketCode = null,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem yêu cầu hỗ trợ." });

        var query =
            from ticket in _db.CustomerSupportTickets.AsNoTracking()
            join order in _db.CustomerOrders.AsNoTracking() on ticket.OrderId equals order.Id into orderJoin
            from order in orderJoin.DefaultIfEmpty()
            where ticket.UserId == userId.Value && !ticket.IsDeleted
            orderby ticket.CreatedAt descending
            select new
            {
                Ticket = ticket,
                OrderCode = order != null && !order.IsDeleted ? order.OrderCode : null,
            };

        if (!string.IsNullOrWhiteSpace(ticketCode))
        {
            var normalized = ticketCode.Trim();
            query = query.Where(x => x.Ticket.TicketCode == normalized);
        }

        var items = await query
            .ToListAsync(ct);

        return Ok(items.Select(x => MapSupportTicket(x.Ticket, x.OrderCode)).ToList());
    }

    [HttpPost("support-tickets")]
    public async Task<ActionResult<CustomerSupportTicketDto>> CreateSupportTicket(
        [FromBody] CreateCustomerSupportTicketRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để tạo yêu cầu hỗ trợ." });

        request ??= new CreateCustomerSupportTicketRequest();

        if (string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Category) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ chủ đề, danh mục và nội dung yêu cầu." });
        }

        var user = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => new
            {
                x.Email,
                x.PhoneNumber,
            })
            .FirstOrDefaultAsync(ct);

        CustomerOrder? order = null;
        if (!string.IsNullOrWhiteSpace(request.OrderCode))
        {
            order = await _db.CustomerOrders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId.Value &&
                    x.OrderCode == request.OrderCode.Trim() &&
                    !x.IsDeleted, ct);

            if (order is null)
                return NotFound(new { message = "Không tìm thấy đơn hàng để gắn vào yêu cầu hỗ trợ." });
        }

        var now = DateTimeOffset.UtcNow;
        var ticket = new CustomerSupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            TenantId = order?.TenantId,
            OrderId = order?.Id,
            TicketCode = $"SP-{now:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            Status = CustomerSupportTicketStatus.Open,
            Category = request.Category.Trim(),
            Subject = request.Subject.Trim(),
            Content = request.Content.Trim(),
            ContactEmail = Normalize(request.ContactEmail) ?? Normalize(user?.Email),
            ContactPhone = Normalize(request.ContactPhone) ?? Normalize(user?.PhoneNumber),
            LastActivityAt = now,
            CreatedAt = now,
            CreatedByUserId = userId.Value,
        };

        _db.CustomerSupportTickets.Add(ticket);
        _db.CustomerNotifications.Add(new CustomerNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            TenantId = ticket.TenantId,
            Status = CustomerNotificationStatus.Unread,
            Category = "support",
            Title = $"Đã tạo yêu cầu hỗ trợ {ticket.TicketCode}",
            Body = $"Chúng tôi đã ghi nhận yêu cầu \"{ticket.Subject}\" và sẽ phản hồi sớm nhất có thể.",
            ActionUrl = "/support",
            ReferenceType = "support-ticket",
            ReferenceId = ticket.Id,
            CreatedAt = now,
            CreatedByUserId = userId.Value,
        });

        await _db.SaveChangesAsync(ct);
        return Ok(MapSupportTicket(ticket, order?.OrderCode));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static CustomerPassengerType ParsePassengerType(string raw)
    {
        return raw?.Trim().ToLowerInvariant() switch
        {
            "adult" => CustomerPassengerType.Adult,
            "child" => CustomerPassengerType.Child,
            "infant" => CustomerPassengerType.Infant,
            _ => CustomerPassengerType.Adult,
        };
    }

    private static CustomerProductType ParseProductType(string raw)
    {
        return raw?.Trim().ToLowerInvariant() switch
        {
            "bus" => CustomerProductType.Bus,
            "train" => CustomerProductType.Train,
            "flight" => CustomerProductType.Flight,
            "hotel" => CustomerProductType.Hotel,
            "tour" => CustomerProductType.Tour,
            _ => throw new InvalidOperationException("Loại dịch vụ không hợp lệ."),
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static CustomerSavedPassengerDto MapPassenger(CustomerSavedPassenger passenger)
    {
        return new CustomerSavedPassengerDto
        {
            Id = passenger.Id,
            FullName = passenger.FullName,
            PassengerType = passenger.PassengerType,
            Gender = passenger.Gender,
            DateOfBirth = passenger.DateOfBirth,
            NationalityCode = passenger.NationalityCode,
            IdNumber = passenger.IdNumber,
            PassportNumber = passenger.PassportNumber,
            Email = passenger.Email,
            PhoneNumber = passenger.PhoneNumber,
            IsDefault = passenger.IsDefault,
            Notes = passenger.Notes,
            CreatedAt = passenger.CreatedAt,
            UpdatedAt = passenger.UpdatedAt,
        };
    }

    private static CustomerWishlistItemDto MapWishlist(CustomerWishlistItem item)
    {
        return new CustomerWishlistItemDto
        {
            Id = item.Id,
            ProductType = item.ProductType,
            TargetId = item.TargetId,
            TargetSlug = item.TargetSlug,
            Title = item.Title,
            Subtitle = item.Subtitle,
            LocationText = item.LocationText,
            PriceText = item.PriceText,
            PriceValue = item.PriceValue,
            CurrencyCode = item.CurrencyCode,
            ImageUrl = item.ImageUrl,
            TargetUrl = item.TargetUrl,
            Metadata = ParseJson(item.MetadataJson),
            CreatedAt = item.CreatedAt,
        };
    }

    private static CustomerVatInvoiceDto MapVat(CustomerVatInvoiceRequest vat, string orderCode)
    {
        return new CustomerVatInvoiceDto
        {
            Id = vat.Id,
            OrderId = vat.OrderId,
            OrderCode = orderCode,
            RequestCode = vat.RequestCode,
            Status = vat.Status,
            CompanyName = vat.CompanyName,
            TaxCode = vat.TaxCode,
            CompanyAddress = vat.CompanyAddress,
            InvoiceEmail = vat.InvoiceEmail,
            InvoiceNumber = vat.InvoiceNumber,
            PdfUrl = vat.PdfUrl,
            CreatedAt = vat.CreatedAt,
            ProcessedAt = vat.ProcessedAt,
        };
    }

    private static CustomerSupportTicketDto MapSupportTicket(CustomerSupportTicket ticket, string? orderCode)
    {
        return new CustomerSupportTicketDto
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            Status = ticket.Status,
            Category = ticket.Category,
            Subject = ticket.Subject,
            Content = ticket.Content,
            OrderCode = orderCode,
            ContactEmail = ticket.ContactEmail,
            ContactPhone = ticket.ContactPhone,
            ResolutionNote = ticket.ResolutionNote,
            HasUnreadStaffReply = ticket.HasUnreadStaffReply,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            FirstResponseAt = ticket.FirstResponseAt,
            ResolvedAt = ticket.ResolvedAt,
            LastActivityAt = ticket.LastActivityAt,
        };
    }

    private static CustomerAccountPreferenceDto MapPreference(CustomerAccountPreference? preference)
    {
        return new CustomerAccountPreferenceDto
        {
            LanguageCode = NormalizeLanguageCode(preference?.LanguageCode),
            CurrencyCode = NormalizeCurrencyCode(preference?.CurrencyCode),
            ThemeMode = NormalizeThemeMode(preference?.ThemeMode),
            EmailNotificationsEnabled = preference?.EmailNotificationsEnabled ?? true,
            SmsNotificationsEnabled = preference?.SmsNotificationsEnabled ?? false,
            PushNotificationsEnabled = preference?.PushNotificationsEnabled ?? true,
            UpdatedAt = preference?.UpdatedAt,
        };
    }

    private static string NormalizeLanguageCode(string? value)
    {
        var normalized = Normalize(value)?.ToLowerInvariant();
        return normalized switch
        {
            "vi" => "vi",
            "en" => "en",
            "zh" or "zh-cn" => "zh-CN",
            "ko" => "ko",
            "th" => "th",
            _ => "vi",
        };
    }

    private static string NormalizeCurrencyCode(string? value)
    {
        var normalized = Normalize(value)?.ToUpperInvariant();
        return normalized switch
        {
            "VND" => "VND",
            "USD" => "USD",
            "EUR" => "EUR",
            "THB" => "THB",
            "KRW" => "KRW",
            _ => "VND",
        };
    }

    private static string NormalizeThemeMode(string? value)
    {
        var normalized = Normalize(value)?.ToLowerInvariant();
        return normalized switch
        {
            "dark" => "dark",
            _ => "light",
        };
    }

    private static JsonElement ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return JsonDocument.Parse("{}").RootElement.Clone();

        try
        {
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
    }
}
