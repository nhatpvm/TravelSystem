using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed class SePayGatewayService
{
    private static readonly string[] SignatureFieldOrder =
    {
        "order_amount",
        "merchant",
        "currency",
        "operation",
        "order_description",
        "order_invoice_number",
        "customer_id",
        "payment_method",
        "success_url",
        "error_url",
        "cancel_url",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly SePayGatewayOptions _options;

    public SePayGatewayService(
        IHttpClientFactory httpClientFactory,
        IHostEnvironment hostEnvironment,
        IOptions<SePayGatewayOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.MerchantId) &&
        !string.IsNullOrWhiteSpace(_options.SecretKey);

    public string? WebhookSecret => _options.WebhookSecret;

    public SePayCheckoutFormDto BuildCheckoutForm(
        CustomerOrder order,
        CustomerPayment payment,
        string? appBaseUrl,
        string orderDescription,
        string customerId)
    {
        EnsureConfigured();

        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["order_amount"] = decimal.Round(payment.Amount, 0, MidpointRounding.AwayFromZero).ToString("0"),
            ["merchant"] = _options.MerchantId.Trim(),
            ["currency"] = payment.CurrencyCode,
            ["operation"] = "PURCHASE",
            ["order_description"] = orderDescription,
            ["order_invoice_number"] = payment.ProviderInvoiceNumber,
            ["customer_id"] = customerId,
            ["payment_method"] = string.IsNullOrWhiteSpace(_options.DefaultPaymentMethod)
                ? "BANK_TRANSFER"
                : _options.DefaultPaymentMethod.Trim(),
        };

        var callbackBaseUrl = ResolveCallbackBaseUrl(appBaseUrl);
        fields["success_url"] = $"{callbackBaseUrl}/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}&result=success";
        fields["error_url"] = $"{callbackBaseUrl}/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}&result=error";
        fields["cancel_url"] = $"{callbackBaseUrl}/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}&result=cancel";

        fields["signature"] = CreateSignature(fields);

        return new SePayCheckoutFormDto
        {
            ActionUrl = $"{_options.PayBaseUrl.TrimEnd('/')}/v1/checkout/init",
            Fields = fields.Select(x => new SePayCheckoutFieldDto
            {
                Name = x.Key,
                Value = x.Value,
            }).ToList(),
        };
    }

    public async Task<SePayOrderSyncResult?> FindOrderByInvoiceAsync(string invoiceNumber, CancellationToken ct = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return null;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_options.ApiBaseUrl.TrimEnd('/')}/v1/order?per_page=20&page=1&q={Uri.EscapeDataString(invoiceNumber)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicAuthToken());

        var client = _httpClientFactory.CreateClient(nameof(SePayGatewayService));
        using var response = await client.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(content))
            return null;

        using var document = JsonDocument.Parse(content);
        if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in dataElement.EnumerateArray())
        {
            var currentInvoice = item.TryGetProperty("order_invoice_number", out var invoiceElement)
                ? invoiceElement.GetString()
                : null;

            if (!string.Equals(currentInvoice, invoiceNumber, StringComparison.OrdinalIgnoreCase))
                continue;

            var orderStatus = item.TryGetProperty("order_status", out var statusElement)
                ? statusElement.GetString() ?? string.Empty
                : string.Empty;

            var orderId = item.TryGetProperty("order_id", out var orderIdElement)
                ? orderIdElement.GetString()
                : null;

            var orderAmount = item.TryGetProperty("order_amount", out var amountElement)
                ? ParseDecimal(amountElement.GetString())
                : null;

            return new SePayOrderSyncResult
            {
                ProviderOrderId = orderId,
                ProviderInvoiceNumber = currentInvoice ?? invoiceNumber,
                RawStatus = orderStatus,
                PaymentStatus = MapPaymentStatus(orderStatus),
                OrderStatus = MapOrderStatus(orderStatus),
                PaidAmount = orderAmount,
                RawPayloadJson = item.GetRawText(),
            };
        }

        return null;
    }

    public static CustomerPaymentStatus MapPaymentStatus(string? status)
    {
        return status?.Trim().ToUpperInvariant() switch
        {
            "CAPTURED" => CustomerPaymentStatus.Paid,
            "CANCELLED" => CustomerPaymentStatus.Cancelled,
            "CANCELED" => CustomerPaymentStatus.Cancelled,
            "EXPIRED" => CustomerPaymentStatus.Expired,
            "FAILED" => CustomerPaymentStatus.Failed,
            _ => CustomerPaymentStatus.Pending,
        };
    }

    public static CustomerOrderStatus MapOrderStatus(string? status)
    {
        return MapPaymentStatus(status) switch
        {
            CustomerPaymentStatus.Paid => CustomerOrderStatus.Paid,
            CustomerPaymentStatus.Cancelled => CustomerOrderStatus.Cancelled,
            CustomerPaymentStatus.Expired => CustomerOrderStatus.Expired,
            CustomerPaymentStatus.Failed => CustomerOrderStatus.Failed,
            _ => CustomerOrderStatus.PendingPayment,
        };
    }

    private static decimal? ParseDecimal(string? raw)
    {
        if (decimal.TryParse(raw, out var value))
            return value;

        return null;
    }

    private string CreateSignature(IReadOnlyDictionary<string, string> fields)
    {
        var signedPairs = new List<string>();
        foreach (var field in SignatureFieldOrder)
        {
            if (!fields.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                continue;

            signedPairs.Add($"{field}={value}");
        }

        var payload = string.Join(",", signedPairs);
        var secretKey = Encoding.UTF8.GetBytes(_options.SecretKey.Trim());
        using var hmac = new HMACSHA256(secretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private string BuildBasicAuthToken()
    {
        var raw = $"{_options.MerchantId.Trim()}:{_options.SecretKey.Trim()}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static string? NormalizeBaseUrl(string? appBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(appBaseUrl))
            return null;

        return appBaseUrl.Trim().TrimEnd('/');
    }

    private string ResolveCallbackBaseUrl(string? clientAppBaseUrl)
    {
        var configuredBaseUrl = NormalizeBaseUrl(_options.FrontendBaseUrl);
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            return configuredBaseUrl;

        var clientBaseUrl = NormalizeBaseUrl(clientAppBaseUrl);
        if (_hostEnvironment.IsDevelopment() &&
            _options.AllowClientAppBaseUrlInDevelopment &&
            !string.IsNullOrWhiteSpace(clientBaseUrl))
        {
            return clientBaseUrl;
        }

        throw new InvalidOperationException(
            "SePay frontend callback URL is not configured. Set SePayGateway:FrontendBaseUrl for public environments.");
    }

    private void EnsureConfigured()
    {
        if (IsConfigured)
            return;

        throw new InvalidOperationException("SePay gateway is not configured.");
    }
}

public sealed class SePayOrderSyncResult
{
    public string ProviderInvoiceNumber { get; set; } = "";
    public string? ProviderOrderId { get; set; }
    public string RawStatus { get; set; } = "";
    public CustomerPaymentStatus PaymentStatus { get; set; }
    public CustomerOrderStatus OrderStatus { get; set; }
    public decimal? PaidAmount { get; set; }
    public string RawPayloadJson { get; set; } = "{}";
}
