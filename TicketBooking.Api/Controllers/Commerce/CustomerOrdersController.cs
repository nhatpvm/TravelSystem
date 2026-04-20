using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Controllers.Commerce;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer/orders")]
[Authorize]
public sealed class CustomerOrdersController : ControllerBase
{
    private readonly CustomerOrderService _customerOrderService;

    public CustomerOrdersController(CustomerOrderService customerOrderService)
    {
        _customerOrderService = customerOrderService;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerOrderDetailDto>> Create(
        [FromBody] CreateCustomerOrderRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem đơn hàng." });

        var result = await _customerOrderService.CreateOrderAsync(request, userId.Value, ct);
        return CreatedAtAction(
            nameof(GetByCode),
            new { version = "1.0", orderCode = result.OrderCode },
            result);
    }

    [HttpGet]
    public async Task<ActionResult<CustomerOrderListResponse>> List(
        [FromQuery] string? q = null,
        [FromQuery] string? productType = null,
        [FromQuery] CustomerOrderStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để tạo đơn hàng." });

        CustomerProductType? parsedProductType = null;
        if (!string.IsNullOrWhiteSpace(productType))
        {
            if (!Enum.TryParse<CustomerProductType>(productType, true, out var value))
            return BadRequest(new { message = "Loại dịch vụ không hợp lệ." });

            parsedProductType = value;
        }

        var result = await _customerOrderService.ListOrdersAsync(
            userId.Value,
            q,
            parsedProductType,
            status,
            page,
            pageSize,
            ct);

        return Ok(result);
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<CustomerPaymentMethodResponse>> GetPaymentMethods(CancellationToken ct = default)
    {
        return Ok(await _customerOrderService.GetSupportedPaymentMethodsAsync(ct));
    }

    [HttpGet("{orderCode}")]
    public async Task<ActionResult<CustomerOrderDetailDto>> GetByCode(
        string orderCode,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem chi tiết đơn hàng." });

        return Ok(await _customerOrderService.GetOrderAsync(orderCode, userId.Value, ct));
    }

    [HttpGet("{orderCode}/timeline")]
    public async Task<ActionResult<List<CustomerOrderTimelineEventDto>>> GetTimeline(
        string orderCode,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để theo dõi tiến trình đơn hàng." });

        return Ok(await _customerOrderService.GetOrderTimelineAsync(orderCode, userId.Value, ct));
    }

    [HttpGet("{orderCode}/refund-estimate")]
    public async Task<ActionResult<CustomerRefundEstimateDto>> GetRefundEstimate(
        string orderCode,
        [FromQuery] decimal? requestedAmount = null,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem ước tính hoàn tiền." });

        return Ok(await _customerOrderService.GetRefundEstimateAsync(orderCode, userId.Value, requestedAmount, ct));
    }

    [HttpPost("{orderCode}/payment-init")]
    public async Task<ActionResult<CustomerOrderDetailDto>> StartPayment(
        string orderCode,
        [FromBody] StartPaymentRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để khởi tạo thanh toán." });

        request ??= new StartPaymentRequest();
        return Ok(await _customerOrderService.StartPaymentAsync(orderCode, userId.Value, request, ct));
    }

    [HttpPost("{orderCode}/payment-sync")]
    public async Task<ActionResult<CustomerOrderDetailDto>> SyncPayment(
        string orderCode,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để đồng bộ trạng thái thanh toán." });

        return Ok(await _customerOrderService.SyncPaymentAsync(orderCode, userId.Value, ct));
    }

    [HttpGet("{orderCode}/ticket")]
    public async Task<ActionResult<CustomerTicketDto>> GetTicket(
        string orderCode,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để xem vé của mình." });

        return Ok(await _customerOrderService.GetTicketAsync(orderCode, userId.Value, ct));
    }

    [HttpPost("{orderCode}/refunds")]
    public async Task<ActionResult<CustomerRefundDto>> CreateRefund(
        string orderCode,
        [FromBody] CreateRefundRequestInput request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để gửi yêu cầu hoàn tiền." });

        return Ok(await _customerOrderService.CreateRefundRequestAsync(orderCode, userId.Value, request, ct));
    }

    [HttpPost("{orderCode}/cancel")]
    public async Task<ActionResult<CustomerOrderDetailDto>> Cancel(
        string orderCode,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Bạn cần đăng nhập để hủy đơn hàng." });

        return Ok(await _customerOrderService.CancelOrderAsync(orderCode, userId.Value, ct));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
