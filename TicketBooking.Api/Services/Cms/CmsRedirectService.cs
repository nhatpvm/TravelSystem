// FILE #304: TicketBooking.Api/Services/Cms/CmsRedirectService.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Cms;

public interface ICmsRedirectService
{
    string NormalizePath(string? path);

    Task<NewsRedirect?> GetByFromPathAsync(
        Guid tenantId,
        string fromPath,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task<NewsRedirect> UpsertAsync(
        Guid tenantId,
        string fromPath,
        string toPath,
        Guid? actorUserId = null,
        RedirectStatusCode statusCode = RedirectStatusCode.MovedPermanently301,
        bool isRegex = false,
        string? reason = null,
        bool isActive = true,
        CancellationToken ct = default);

    Task<NewsRedirect?> UpsertPostSlugRedirectAsync(
        Guid tenantId,
        string? oldSlug,
        string? newSlug,
        Guid? actorUserId = null,
        string? reason = null,
        CancellationToken ct = default);

    Task<int> SoftDeleteByFromPathAsync(
        Guid tenantId,
        string fromPath,
        Guid? actorUserId = null,
        CancellationToken ct = default);
}

public sealed class CmsRedirectService : ICmsRedirectService
{
    private readonly AppDbContext _db;
    private readonly ICmsSlugService _slugService;

    public CmsRedirectService(AppDbContext db, ICmsSlugService slugService)
    {
        _db = db;
        _slugService = slugService;
    }

    public string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var value = path.Trim();

        if (!value.StartsWith('/'))
            value = "/" + value;

        while (value.Contains("//", StringComparison.Ordinal))
            value = value.Replace("//", "/", StringComparison.Ordinal);

        if (value.Length > 1 && value.EndsWith('/'))
            value = value.TrimEnd('/');

        return value;
    }

    public async Task<NewsRedirect?> GetByFromPathAsync(
        Guid tenantId,
        string fromPath,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("tenantId is required.");

        var normalizedFrom = NormalizePath(fromPath);

        IQueryable<NewsRedirect> query = _db.Set<NewsRedirect>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.FromPath == normalizedFrom,
            ct);
    }

    public async Task<NewsRedirect> UpsertAsync(
        Guid tenantId,
        string fromPath,
        string toPath,
        Guid? actorUserId = null,
        RedirectStatusCode statusCode = RedirectStatusCode.MovedPermanently301,
        bool isRegex = false,
        string? reason = null,
        bool isActive = true,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("tenantId is required.");

        var normalizedFrom = NormalizePath(fromPath);
        var normalizedTo = NormalizePath(toPath);

        if (normalizedFrom == "/" && normalizedTo == "/")
            throw new InvalidOperationException("Redirect source and target cannot both be root path.");

        if (!isRegex && string.Equals(normalizedFrom, normalizedTo, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Redirect source and target cannot be the same path.");

        var now = DateTimeOffset.Now;

        var entity = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.FromPath == normalizedFrom, ct);

        if (entity is null)
        {
            entity = new NewsRedirect
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FromPath = normalizedFrom,
                ToPath = normalizedTo,
                StatusCode = statusCode,
                IsRegex = isRegex,
                Reason = NullIfWhite(reason),
                IsActive = isActive,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };

            _db.Set<NewsRedirect>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        entity.ToPath = normalizedTo;
        entity.StatusCode = statusCode;
        entity.IsRegex = isRegex;
        entity.Reason = NullIfWhite(reason);
        entity.IsActive = isActive;

        if (entity.IsDeleted)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<NewsRedirect?> UpsertPostSlugRedirectAsync(
        Guid tenantId,
        string? oldSlug,
        string? newSlug,
        Guid? actorUserId = null,
        string? reason = null,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("tenantId is required.");

        var oldNormalized = _slugService.Normalize(oldSlug);
        var newNormalized = _slugService.Normalize(newSlug);

        if (string.IsNullOrWhiteSpace(oldNormalized) || string.IsNullOrWhiteSpace(newNormalized))
            return null;

        if (string.Equals(oldNormalized, newNormalized, StringComparison.OrdinalIgnoreCase))
            return null;

        var fromPath = _slugService.BuildPostPath(oldNormalized);
        var toPath = _slugService.BuildPostPath(newNormalized);

        return await UpsertAsync(
            tenantId: tenantId,
            fromPath: fromPath,
            toPath: toPath,
            actorUserId: actorUserId,
            statusCode: RedirectStatusCode.MovedPermanently301,
            isRegex: false,
            reason: string.IsNullOrWhiteSpace(reason) ? "Auto redirect after post slug change." : reason,
            isActive: true,
            ct: ct);
    }

    public async Task<int> SoftDeleteByFromPathAsync(
        Guid tenantId,
        string fromPath,
        Guid? actorUserId = null,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("tenantId is required.");

        var normalizedFrom = NormalizePath(fromPath);
        var now = DateTimeOffset.Now;

        var items = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.FromPath == normalizedFrom && !x.IsDeleted)
            .ToListAsync(ct);

        if (items.Count == 0)
            return 0;

        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.IsActive = false;
            item.UpdatedAt = now;
            item.UpdatedByUserId = actorUserId;
        }

        await _db.SaveChangesAsync(ct);
        return items.Count;
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
