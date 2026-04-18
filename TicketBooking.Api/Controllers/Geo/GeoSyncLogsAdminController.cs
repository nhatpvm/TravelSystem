// FILE: TicketBooking.Api/Controllers/GeoSyncLogsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Geo;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers;

/// <summary>
/// Ops/Admin: read-only endpoints for geo.GeoSyncLogs
/// - Admin only
/// - No tenant header needed (logs are operational)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/geo/sync-logs")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class GeoSyncLogsAdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public GeoSyncLogsAdminController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool? isSuccess = null,
        [FromQuery] string? source = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Set<GeoSyncLog>().AsNoTracking();

        if (isSuccess.HasValue)
            q = q.Where(x => x.IsSuccess == isSuccess.Value);

        if (!string.IsNullOrWhiteSpace(source))
        {
            var s = source.Trim();
            q = q.Where(x => x.Source.Contains(s));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Source,
                x.Url,
                x.Depth,
                x.HttpStatus,
                x.IsSuccess,
                x.ProvincesInserted,
                x.ProvincesUpdated,
                x.DistrictsInserted,
                x.DistrictsUpdated,
                x.WardsInserted,
                x.WardsUpdated,
                x.ErrorMessage,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var x = await _db.Set<GeoSyncLog>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (x is null) return NotFound(new { message = "Không tìm thấy nhật ký đồng bộ." });

        return Ok(new
        {
            x.Id,
            x.Source,
            x.Url,
            x.Depth,
            x.HttpStatus,
            x.IsSuccess,
            x.ProvincesInserted,
            x.ProvincesUpdated,
            x.DistrictsInserted,
            x.DistrictsUpdated,
            x.WardsInserted,
            x.WardsUpdated,
            x.ErrorMessage,
            x.ErrorDetail,
            x.CreatedAt
        });
    }
}

