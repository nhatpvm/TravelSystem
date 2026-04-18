using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Commerce;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CustomerOrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly AppDbContext _db;
    private readonly CommerceOptions _options;
    private readonly SePayGatewayService _sePayGatewayService;
    private readonly CustomerNotificationService _notificationService;
    private readonly TourPackageReservationService _tourReservationService;
    private readonly TourPackageBookingService _tourBookingService;

    public CustomerOrderService(
        AppDbContext db,
        IOptions<CommerceOptions> options,
        SePayGatewayService sePayGatewayService,
        CustomerNotificationService notificationService,
        TourPackageReservationService tourReservationService,
        TourPackageBookingService tourBookingService)
    {
        _db = db;
        _options = options.Value;
        _sePayGatewayService = sePayGatewayService;
        _notificationService = notificationService;
        _tourReservationService = tourReservationService;
        _tourBookingService = tourBookingService;
    }

    public async Task<CustomerOrderDetailDto> CreateOrderAsync(
        CreateCustomerOrderRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCreateRequest(request);

        var productType = ParseProductType(request.ProductType);
        var now = DateTimeOffset.UtcNow;

        await ExpireDueOrdersAsync(userId, ct);

        return productType switch
        {
            CustomerProductType.Bus => await CreateBusOrderAsync(request, userId, now, ct),
            CustomerProductType.Train => await CreateTrainOrderAsync(request, userId, now, ct),
            CustomerProductType.Flight => await CreateFlightOrderAsync(request, userId, now, ct),
            CustomerProductType.Hotel => await CreateHotelOrderAsync(request, userId, now, ct),
            CustomerProductType.Tour => await CreateTourOrderAsync(request, userId, now, ct),
            _ => throw new InvalidOperationException("Loại sản phẩm chưa được hỗ trợ."),
        };
    }

    public async Task<CustomerOrderListResponse> ListOrdersAsync(
        Guid userId,
        string? q = null,
        CustomerProductType? productType = null,
        CustomerOrderStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        await ExpireDueOrdersAsync(userId, ct);

        IQueryable<CustomerOrder> query = _db.CustomerOrders
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted);

        if (productType.HasValue)
            query = query.Where(x => x.ProductType == productType.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.OrderCode.Contains(keyword) ||
                x.ContactFullName.Contains(keyword) ||
                x.ContactEmail.Contains(keyword));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomerOrderSummaryDto
            {
                Id = x.Id,
                OrderCode = x.OrderCode,
                CurrencyCode = x.CurrencyCode,
                ProductType = x.ProductType,
                Status = x.Status,
                PaymentStatus = x.PaymentStatus,
                TicketStatus = x.TicketStatus,
                RefundStatus = x.RefundStatus,
                PayableAmount = x.PayableAmount,
                RefundedAmount = x.RefundedAmount,
                ExpiresAt = x.ExpiresAt,
                CreatedAt = x.CreatedAt,
                PaidAt = x.PaidAt,
                Snapshot = ParseJsonElement(x.SnapshotJson),
            })
            .ToListAsync(ct);

        return new CustomerOrderListResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items,
        };
    }

    public async Task<CustomerOrderDetailDto> GetOrderAsync(
        string orderCode,
        Guid userId,
        CancellationToken ct = default)
    {
        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);
        return await MapOrderDetailAsync(order.Id, ct);
    }

    public async Task<CustomerTicketDto> GetTicketAsync(
        string orderCode,
        Guid userId,
        CancellationToken ct = default)
    {
        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);

        var ticket = await _db.CustomerTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

        if (ticket is null)
            throw new KeyNotFoundException("Vé điện tử chưa được phát hành cho đơn hàng này.");

        return new CustomerTicketDto
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            Status = ticket.Status,
            Title = ticket.Title,
            Subtitle = ticket.Subtitle,
            IssuedAt = ticket.IssuedAt,
            Snapshot = ParseJsonElement(ticket.SnapshotJson),
        };
    }

    public Task<CustomerPaymentMethodResponse> GetSupportedPaymentMethodsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new CustomerPaymentMethodResponse
        {
            Methods =
            {
                new CustomerSupportedPaymentMethodDto
                {
                    Code = "sepay",
                    Name = "Chuyển khoản SePay",
                    Description = "Thanh toán qua cổng SePay, tiền về tài khoản platform và được đối soát tự động.",
                    IsDefault = true,
                },
            },
            SecurityBadges =
            {
                "Theo dõi trạng thái giao dịch tự động",
                "Tiền về tài khoản platform",
                "Đối soát theo tenant và hoa hồng",
            },
        });
    }
}
