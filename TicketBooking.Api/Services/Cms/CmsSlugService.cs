// FILE #300: TicketBooking.Api/Services/Cms/CmsSlugService.cs
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Cms;

public interface ICmsSlugService
{
    string Normalize(string? input);
    string NormalizeOrFallback(string? input, string fallbackPrefix);
    Task<string> EnsureUniquePostSlugAsync(Guid tenantId, string desiredSlug, Guid? excludePostId = null, CancellationToken ct = default);
    Task<string> EnsureUniqueCategorySlugAsync(Guid tenantId, string desiredSlug, Guid? excludeCategoryId = null, CancellationToken ct = default);
    Task<string> EnsureUniqueTagSlugAsync(Guid tenantId, string desiredSlug, Guid? excludeTagId = null, CancellationToken ct = default);
    string BuildPostPath(string slug);
    string BuildCategoryPath(string slug);
    string BuildTagPath(string slug);
}

public sealed class CmsSlugService : ICmsSlugService
{
    private readonly AppDbContext _db;

    public CmsSlugService(AppDbContext db)
    {
        _db = db;
    }

    public string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var source = RemoveDiacritics(input.Trim().ToLowerInvariant());

        var sb = new StringBuilder(source.Length);
        var previousWasDash = false;

        foreach (var ch in source)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                previousWasDash = false;
                continue;
            }

            if (IsDashLikeSeparator(ch))
            {
                if (!previousWasDash && sb.Length > 0)
                {
                    sb.Append('-');
                    previousWasDash = true;
                }

                continue;
            }

            // Ignore everything else: punctuation, emoji, symbols...
        }

        var slug = sb.ToString().Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return slug;
    }

    public string NormalizeOrFallback(string? input, string fallbackPrefix)
    {
        var slug = Normalize(input);
        if (!string.IsNullOrWhiteSpace(slug))
            return slug;

        fallbackPrefix = Normalize(fallbackPrefix);
        if (string.IsNullOrWhiteSpace(fallbackPrefix))
            fallbackPrefix = "post";

        return $"{fallbackPrefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
    }

    public async Task<string> EnsureUniquePostSlugAsync(
        Guid tenantId,
        string desiredSlug,
        Guid? excludePostId = null,
        CancellationToken ct = default)
    {
        return await EnsureUniqueAsync(
            desiredSlug: desiredSlug,
            fallbackPrefix: "post",
            existsAsync: async slug =>
            {
                var query = _db.Set<NewsPost>()
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.Slug == slug);

                if (excludePostId.HasValue && excludePostId.Value != Guid.Empty)
                    query = query.Where(x => x.Id != excludePostId.Value);

                return await query.AnyAsync(ct);
            });
    }

    public async Task<string> EnsureUniqueCategorySlugAsync(
        Guid tenantId,
        string desiredSlug,
        Guid? excludeCategoryId = null,
        CancellationToken ct = default)
    {
        return await EnsureUniqueAsync(
            desiredSlug: desiredSlug,
            fallbackPrefix: "category",
            existsAsync: async slug =>
            {
                var query = _db.Set<NewsCategory>()
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.Slug == slug);

                if (excludeCategoryId.HasValue && excludeCategoryId.Value != Guid.Empty)
                    query = query.Where(x => x.Id != excludeCategoryId.Value);

                return await query.AnyAsync(ct);
            });
    }

    public async Task<string> EnsureUniqueTagSlugAsync(
        Guid tenantId,
        string desiredSlug,
        Guid? excludeTagId = null,
        CancellationToken ct = default)
    {
        return await EnsureUniqueAsync(
            desiredSlug: desiredSlug,
            fallbackPrefix: "tag",
            existsAsync: async slug =>
            {
                var query = _db.Set<NewsTag>()
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.Slug == slug);

                if (excludeTagId.HasValue && excludeTagId.Value != Guid.Empty)
                    query = query.Where(x => x.Id != excludeTagId.Value);

                return await query.AnyAsync(ct);
            });
    }

    public string BuildPostPath(string slug)
    {
        var normalized = NormalizeOrFallback(slug, "post");
        return $"/tin-tuc/{normalized}";
    }

    public string BuildCategoryPath(string slug)
    {
        var normalized = NormalizeOrFallback(slug, "category");
        return $"/tin-tuc/chuyen-muc/{normalized}";
    }

    public string BuildTagPath(string slug)
    {
        var normalized = NormalizeOrFallback(slug, "tag");
        return $"/tin-tuc/the/{normalized}";
    }

    private async Task<string> EnsureUniqueAsync(
        string desiredSlug,
        string fallbackPrefix,
        Func<string, Task<bool>> existsAsync)
    {
        var baseSlug = NormalizeOrFallback(desiredSlug, fallbackPrefix);

        if (!await existsAsync(baseSlug))
            return baseSlug;

        for (var i = 2; i <= 10000; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!await existsAsync(candidate))
                return candidate;
        }

        return $"{baseSlug}-{Guid.NewGuid():N}";
    }

    private static bool IsDashLikeSeparator(char ch)
    {
        return char.IsWhiteSpace(ch)
               || ch == '-'
               || ch == '_'
               || ch == '.'
               || ch == '/'
               || ch == '\\'
               || ch == '+';
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Vietnamese specific normalization first
        text = text.Replace('đ', 'd').Replace('Đ', 'D');

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
