using System.Text.RegularExpressions;

namespace TicketBooking.Api.Services.Uploads;

public sealed class PortalImageUploadService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedManagerScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "tour-cover",
        "tour-image",
        "room-type-image",
        "airline-logo",
        "cms-post-cover",
        "cms-site-og-image",
        "cms-site-brand-logo",
        "cms-media"
    };

    private static readonly HashSet<string> AllowedCustomerScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "customer-avatar"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PortalImageUploadService> _logger;

    public PortalImageUploadService(
        IWebHostEnvironment environment,
        ILogger<PortalImageUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<PortalStoredImageResult> SaveManagerImageAsync(
        IFormFile file,
        string scope,
        string? tenantId,
        CancellationToken ct = default)
    {
        var normalizedScope = NormalizeManagerScope(scope)
            ?? throw new InvalidOperationException("Upload scope is invalid.");

        return await SaveImageAsync(
            file,
            area: "manager",
            normalizedScope,
            tenantId,
            ct);
    }

    public async Task<PortalStoredImageResult> SaveCustomerImageAsync(
        IFormFile file,
        string scope,
        string? userId,
        CancellationToken ct = default)
    {
        var normalizedScope = NormalizeCustomerScope(scope)
            ?? throw new InvalidOperationException("Upload scope is invalid.");

        return await SaveImageAsync(
            file,
            area: "customer",
            normalizedScope,
            userId,
            ct);
    }

    private async Task<PortalStoredImageResult> SaveImageAsync(
        IFormFile file,
        string area,
        string scope,
        string? ownerSegment,
        CancellationToken ct)
    {
        if (!IsAllowedImage(file))
        {
            throw new InvalidOperationException("Only .jpg, .jpeg, .png, .webp images up to 10MB are supported.");
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        var rootDirectory = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            rootDirectory = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var targetSegments = new List<string> { rootDirectory, "uploads", area, scope };
        var urlSegments = new List<string> { "uploads", area, scope };

        var normalizedOwnerSegment = NormalizePathSegment(ownerSegment);
        if (!string.IsNullOrWhiteSpace(normalizedOwnerSegment))
        {
            targetSegments.Add(normalizedOwnerSegment);
            urlSegments.Add(normalizedOwnerSegment);
        }

        var folderPath = Path.Combine(targetSegments.ToArray());
        Directory.CreateDirectory(folderPath);

        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(folderPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var relativeUrl = "/" + string.Join("/", urlSegments.Append(fileName));
        var storageKey = string.Join("/", urlSegments.Append(fileName));

        _logger.LogInformation(
            "Stored portal upload {FileName} for area {Area}, scope {Scope}, owner {OwnerSegment}.",
            fileName,
            area,
            scope,
            normalizedOwnerSegment ?? "global");

        return new PortalStoredImageResult(relativeUrl, storageKey, fileName, file.ContentType, file.Length);
    }

    public static bool IsAllowedImage(IFormFile? file)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxFileSizeBytes)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }

    public static string? NormalizeManagerScope(string? scope)
    {
        var normalized = NormalizePathSegment(scope);
        return normalized is not null && AllowedManagerScopes.Contains(normalized)
            ? normalized
            : null;
    }

    public static string? NormalizeCustomerScope(string? scope)
    {
        var normalized = NormalizePathSegment(scope);
        return normalized is not null && AllowedCustomerScopes.Contains(normalized)
            ? normalized
            : null;
    }

    private static string? NormalizePathSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9-]+", "-");
        normalized = normalized.Trim('-');
        return normalized.Length == 0 ? null : normalized;
    }
}

public sealed record PortalStoredImageResult(
    string RelativeUrl,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes);
