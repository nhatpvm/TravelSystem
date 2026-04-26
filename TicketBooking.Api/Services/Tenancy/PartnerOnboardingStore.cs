using System.Text.Json;

namespace TicketBooking.Api.Services.Tenancy;

public sealed class PartnerOnboardingStore
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PartnerOnboardingStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public PartnerOnboardingStore(
        IWebHostEnvironment environment,
        ILogger<PartnerOnboardingStore> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<PartnerOnboardingStoreResult> SaveAsync(
        PartnerOnboardingSubmission submission,
        IFormFile legalDocument,
        CancellationToken ct = default)
    {
        var trackingCode = GenerateTrackingCode();
        var rootDir = Path.Combine(GetBaseDirectory(), trackingCode);
        Directory.CreateDirectory(rootDir);

        var extension = Path.GetExtension(legalDocument.FileName)?.ToLowerInvariant() ?? "";
        var documentFileName = $"legal-document{extension}";
        var documentPath = Path.Combine(rootDir, documentFileName);

        await using (var stream = File.Create(documentPath))
        {
            await legalDocument.CopyToAsync(stream, ct);
        }

        var stored = new StoredPartnerOnboardingRequest
        {
            TrackingCode = trackingCode,
            ServiceType = submission.ServiceType,
            BusinessName = submission.BusinessName,
            TaxCode = submission.TaxCode,
            Address = submission.Address,
            ContactEmail = submission.ContactEmail,
            ContactPhone = submission.ContactPhone,
            Status = "PendingReview",
            SubmittedAt = DateTimeOffset.Now,
            LegalDocument = new StoredPartnerOnboardingDocument
            {
                OriginalFileName = Path.GetFileName(legalDocument.FileName),
                StoredFileName = documentFileName,
                ContentType = legalDocument.ContentType,
                SizeBytes = legalDocument.Length
            }
        };

        var metadataPath = Path.Combine(rootDir, "metadata.json");
        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(stored, _jsonOptions),
            ct);

        _logger.LogInformation(
            "Stored partner onboarding request {TrackingCode} for {BusinessName} ({ServiceType}).",
            trackingCode,
            stored.BusinessName,
            stored.ServiceType);

        return new PartnerOnboardingStoreResult
        {
            TrackingCode = trackingCode,
            BusinessName = stored.BusinessName,
            ServiceType = stored.ServiceType,
            Status = stored.Status,
            SubmittedAt = stored.SubmittedAt,
            ReviewEtaHours = 24
        };
    }

    public async Task<StoredPartnerOnboardingRequest?> GetAsync(
        string trackingCode,
        CancellationToken ct = default)
    {
        var safeTrackingCode = NormalizeTrackingCode(trackingCode);
        if (safeTrackingCode is null)
            return null;

        var metadataPath = Path.Combine(GetBaseDirectory(), safeTrackingCode, "metadata.json");
        if (!File.Exists(metadataPath))
            return null;

        var json = await File.ReadAllTextAsync(metadataPath, ct);
        return JsonSerializer.Deserialize<StoredPartnerOnboardingRequest>(json, _jsonOptions);
    }

    public async Task<IReadOnlyList<StoredPartnerOnboardingRequest>> ListAsync(
        string? status = null,
        string? q = null,
        CancellationToken ct = default)
    {
        var rootDir = GetBaseDirectory();
        if (!Directory.Exists(rootDir))
        {
            return Array.Empty<StoredPartnerOnboardingRequest>();
        }

        var normalizedStatus = NormalizeStatus(status);
        var normalizedQuery = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var items = new List<StoredPartnerOnboardingRequest>();

        foreach (var directory in Directory.EnumerateDirectories(rootDir))
        {
            var metadataPath = Path.Combine(directory, "metadata.json");
            if (!File.Exists(metadataPath))
            {
                continue;
            }

            StoredPartnerOnboardingRequest? item = null;
            try
            {
                var json = await File.ReadAllTextAsync(metadataPath, ct);
                item = JsonSerializer.Deserialize<StoredPartnerOnboardingRequest>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read onboarding metadata from {MetadataPath}.", metadataPath);
            }

            if (item is null)
            {
                continue;
            }

            if (normalizedStatus is not null &&
                !string.Equals(item.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (normalizedQuery is not null &&
                !ContainsIgnoreCase(item.TrackingCode, normalizedQuery) &&
                !ContainsIgnoreCase(item.BusinessName, normalizedQuery) &&
                !ContainsIgnoreCase(item.TaxCode, normalizedQuery) &&
                !ContainsIgnoreCase(item.ContactEmail, normalizedQuery))
            {
                continue;
            }

            items.Add(item);
        }

        return items
            .OrderByDescending(x => x.SubmittedAt)
            .ThenBy(x => x.BusinessName)
            .ToArray();
    }

    public async Task<StoredPartnerOnboardingRequest?> ReviewAsync(
        string trackingCode,
        string status,
        string? reviewedBy,
        string? reviewNote,
        string? rejectReason,
        string? needMoreInfoReason,
        CancellationToken ct = default)
    {
        var safeTrackingCode = NormalizeTrackingCode(trackingCode);
        var normalizedStatus = NormalizeStatus(status);
        if (safeTrackingCode is null || normalizedStatus is null)
        {
            return null;
        }

        var metadataPath = Path.Combine(GetBaseDirectory(), safeTrackingCode, "metadata.json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(metadataPath, ct);
        var current = JsonSerializer.Deserialize<StoredPartnerOnboardingRequest>(json, _jsonOptions);
        if (current is null)
        {
            return null;
        }

        var updated = current with
        {
            Status = normalizedStatus,
            ReviewedAt = DateTimeOffset.Now,
            ReviewedBy = NormalizeOptional(reviewedBy, 200),
            ReviewNote = NormalizeOptional(reviewNote, 1000),
            ReviewerNote = NormalizeOptional(reviewNote, 1000),
            RejectReason = string.Equals(normalizedStatus, "Rejected", StringComparison.OrdinalIgnoreCase)
                ? NormalizeOptional(rejectReason, 1000)
                : null,
            NeedMoreInfoReason = string.Equals(normalizedStatus, "NeedsMoreInfo", StringComparison.OrdinalIgnoreCase)
                ? NormalizeOptional(needMoreInfoReason, 1000)
                : null
        };

        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(updated, _jsonOptions),
            ct);

        _logger.LogInformation(
            "Reviewed partner onboarding request {TrackingCode} with status {Status}.",
            safeTrackingCode,
            normalizedStatus);

        return updated;
    }

    public async Task<StoredPartnerOnboardingRequest?> MarkProvisionedAsync(
        string trackingCode,
        Guid tenantId,
        string tenantCode,
        Guid ownerUserId,
        string ownerEmail,
        string? provisionedBy,
        CancellationToken ct = default)
    {
        var safeTrackingCode = NormalizeTrackingCode(trackingCode);
        if (safeTrackingCode is null)
        {
            return null;
        }

        var metadataPath = Path.Combine(GetBaseDirectory(), safeTrackingCode, "metadata.json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(metadataPath, ct);
        var current = JsonSerializer.Deserialize<StoredPartnerOnboardingRequest>(json, _jsonOptions);
        if (current is null)
        {
            return null;
        }

        var now = DateTimeOffset.Now;
        var updated = current with
        {
            Status = "Approved",
            ReviewedAt = current.ReviewedAt ?? now,
            ReviewedBy = current.ReviewedBy ?? NormalizeOptional(provisionedBy, 200),
            ProvisionedAt = now,
            ProvisionedBy = NormalizeOptional(provisionedBy, 200),
            TenantId = tenantId,
            TenantCode = tenantCode,
            OwnerUserId = ownerUserId,
            OwnerEmail = ownerEmail
        };

        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(updated, _jsonOptions),
            ct);

        _logger.LogInformation(
            "Provisioned partner onboarding request {TrackingCode} into tenant {TenantCode}.",
            safeTrackingCode,
            tenantCode);

        return updated;
    }

    public static bool IsAllowedDocument(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
            return false;

        var extension = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }

    public static string? NormalizeServiceType(string? serviceType)
    {
        var normalized = (serviceType ?? "").Trim().ToLowerInvariant();
        return normalized switch
        {
            "bus" => "bus",
            "train" => "train",
            "flight" => "flight",
            "hotel" => "hotel",
            "tour" => "tour",
            _ => null
        };
    }

    public static string? NormalizeStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        return normalized.ToUpperInvariant() switch
        {
            "PENDING" or "PENDINGREVIEW" => "PendingReview",
            "APPROVED" => "Approved",
            "REJECTED" => "Rejected",
            "NEEDSMOREINFO" or "NEEDS_MORE_INFO" => "NeedsMoreInfo",
            _ => null
        };
    }

    private string GetBaseDirectory()
    {
        var baseDir = Path.Combine(_environment.ContentRootPath, "App_Data", "partner-onboarding");
        Directory.CreateDirectory(baseDir);
        return baseDir;
    }

    private static string GenerateTrackingCode()
        => $"ONB-{DateTimeOffset.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..23].ToUpperInvariant();

    private static string? NormalizeTrackingCode(string? trackingCode)
    {
        var value = (trackingCode ?? "").Trim().ToUpperInvariant();
        if (value.Length < 8 || value.Length > 32)
            return null;

        foreach (var c in value)
        {
            if (!(char.IsLetterOrDigit(c) || c == '-'))
                return null;
        }

        return value;
    }

    private static bool ContainsIgnoreCase(string? value, string keyword)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return normalized;
    }
}

public sealed class PartnerOnboardingSubmission
{
    public string ServiceType { get; init; } = "";
    public string BusinessName { get; init; } = "";
    public string TaxCode { get; init; } = "";
    public string Address { get; init; } = "";
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
}

public sealed class PartnerOnboardingStoreResult
{
    public string TrackingCode { get; init; } = "";
    public string ServiceType { get; init; } = "";
    public string BusinessName { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset SubmittedAt { get; init; }
    public int ReviewEtaHours { get; init; }
}

public sealed record StoredPartnerOnboardingRequest
{
    public string TrackingCode { get; init; } = "";
    public string ServiceType { get; init; } = "";
    public string BusinessName { get; init; } = "";
    public string TaxCode { get; init; } = "";
    public string Address { get; init; } = "";
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string Status { get; init; } = "";
    public DateTimeOffset SubmittedAt { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public string? ReviewedBy { get; init; }
    public string? ReviewNote { get; init; }
    public string? ReviewerNote { get; init; }
    public string? RejectReason { get; init; }
    public string? NeedMoreInfoReason { get; init; }
    public Guid? TenantId { get; init; }
    public string? TenantCode { get; init; }
    public Guid? OwnerUserId { get; init; }
    public string? OwnerEmail { get; init; }
    public DateTimeOffset? ProvisionedAt { get; init; }
    public string? ProvisionedBy { get; init; }
    public StoredPartnerOnboardingDocument LegalDocument { get; init; } = new();
}

public sealed class StoredPartnerOnboardingDocument
{
    public string OriginalFileName { get; init; } = "";
    public string StoredFileName { get; init; } = "";
    public string? ContentType { get; init; }
    public long SizeBytes { get; init; }
}
