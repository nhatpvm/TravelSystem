using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/fare-classes")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainFareClassesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainFareClassesController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class UpsertFareClassRequest
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public TrainSeatType SeatType { get; set; } = TrainSeatType.Seat;
        public string? Description { get; set; }
        public decimal DefaultModifier { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<TrainFareClass> query = _db.TrainFareClasses;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var items = await query
            .Where(x => x.TenantId == _tenantContext.TenantId)
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.SeatType,
                x.Description,
                x.DefaultModifier,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertFareClassRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        var tenantId = _tenantContext.TenantId!.Value;
        var code = req.Code.Trim().ToUpperInvariant();

        var exists = await _db.TrainFareClasses.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (exists)
            return Conflict(new { message = "Fare class code already exists in this tenant." });

        var entity = new TrainFareClass
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = req.Name.Trim(),
            SeatType = req.SeatType,
            Description = TrimOrNull(req.Description, 300),
            DefaultModifier = req.DefaultModifier,
            IsActive = req.IsActive,
            CreatedAt = DateTimeOffset.Now
        };

        _db.TrainFareClasses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertFareClassRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainFareClasses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Fare class not found in this tenant." });

        var code = req.Code.Trim().ToUpperInvariant();
        var exists = await _db.TrainFareClasses.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "Fare class code already exists in this tenant." });

        entity.Code = code;
        entity.Name = req.Name.Trim();
        entity.SeatType = req.SeatType;
        entity.Description = TrimOrNull(req.Description, 300);
        entity.DefaultModifier = req.DefaultModifier;
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.TrainFareClasses
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Fare class not found." });

        _db.TrainFareClasses.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.TrainFareClasses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Fare class not found." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private static void Validate(UpsertFareClassRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Code)) throw new InvalidOperationException("Code is required.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Name is required.");
        if (req.Code.Trim().Length > 50) throw new InvalidOperationException("Code max length is 50.");
        if (req.Name.Trim().Length > 120) throw new InvalidOperationException("Name max length is 120.");
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
