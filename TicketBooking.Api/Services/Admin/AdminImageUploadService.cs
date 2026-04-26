using System.Text.RegularExpressions;

namespace TicketBooking.Api.Services.Admin;

public sealed class AdminImageUploadService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "hotel-image",
        "room-type-image",
        "tour-cover",
        "user-avatar"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AdminImageUploadService> _logger;

    public AdminImageUploadService(
        IWebHostEnvironment environment,
        ILogger<AdminImageUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<AdminStoredImageResult> SaveImageAsync(
        IFormFile file,
        string scope,
        string? tenantId,
        CancellationToken ct = default)
    {
        var normalizedScope = NormalizeScope(scope)
            ?? throw new InvalidOperationException("Upload scope is invalid.");

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

        var targetSegments = new List<string> { rootDirectory, "uploads", "admin", normalizedScope };
        var urlSegments = new List<string> { "uploads", "admin", normalizedScope };

        var normalizedTenantId = NormalizePathSegment(tenantId);
        if (!string.IsNullOrWhiteSpace(normalizedTenantId))
        {
            targetSegments.Add(normalizedTenantId);
            urlSegments.Add(normalizedTenantId);
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

        _logger.LogInformation(
            "Stored admin upload {FileName} for scope {Scope} (tenant {TenantId}).",
            fileName,
            normalizedScope,
            normalizedTenantId ?? "global");

        return new AdminStoredImageResult(relativeUrl, fileName, file.ContentType, file.Length);
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

    public static string? NormalizeScope(string? scope)
    {
        var normalized = NormalizePathSegment(scope);
        return normalized is not null && AllowedScopes.Contains(normalized)
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

public sealed record AdminStoredImageResult(
    string RelativeUrl,
    string FileName,
    string ContentType,
    long SizeBytes);
