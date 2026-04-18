// FILE #259: TicketBooking.Api/Controllers/Tours/TourSchedulesAdminController.cs
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/tour-schedules")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class TourSchedulesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly TourLocalTimeService _tourLocalTimeService;

    public TourSchedulesAdminController(
        AppDbContext db,
        ITenantContext tenant,
        TourLocalTimeService tourLocalTimeService)
    {
        _db = db;
        _tenant = tenant;
        _tourLocalTimeService = tourLocalTimeService;
    }

    [HttpGet]
    public async Task<ActionResult<TourSchedulesAdminPagedResponse>> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? tourId = null,
        [FromQuery] string? q = null,
        [FromQuery] TourScheduleStatus? status = null,
        [FromQuery] DateOnly? fromDepartureDate = null,
        [FromQuery] DateOnly? toDepartureDate = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool? featured = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<TourSchedule> query = includeDeleted
            ? _db.TourSchedules.IgnoreQueryFilters()
            : _db.TourSchedules.Where(x => !x.IsDeleted);

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            query = query.Where(x => x.TenantId == tenantId.Value);

        if (tourId.HasValue && tourId.Value != Guid.Empty)
            query = query.Where(x => x.TourId == tourId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (fromDepartureDate.HasValue)
            query = query.Where(x => x.DepartureDate >= fromDepartureDate.Value);

        if (toDepartureDate.HasValue)
            query = query.Where(x => x.DepartureDate <= toDepartureDate.Value);

        if (active.HasValue)
            query = query.Where(x => x.IsActive == active.Value);

        if (featured.HasValue)
            query = query.Where(x => x.IsFeatured == featured.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                (x.Name != null && x.Name.Contains(qq)) ||
                (x.MeetingPointSummary != null && x.MeetingPointSummary.Contains(qq)) ||
                (x.PickupSummary != null && x.PickupSummary.Contains(qq)) ||
                (x.DropoffSummary != null && x.DropoffSummary.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var schedules = await query
            .AsNoTracking()
            .OrderBy(x => x.TenantId)
            .ThenBy(x => x.TourId)
            .ThenBy(x => x.DepartureDate)
            .ThenBy(x => x.DepartureTime)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var scheduleIds = schedules.Select(x => x.Id).ToList();

        var capacities = await _db.TourScheduleCapacities.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => scheduleIds.Contains(x.TourScheduleId) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.TourScheduleId, ct);

        var adultPrices = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                scheduleIds.Contains(x.TourScheduleId) &&
                !x.IsDeleted &&
                x.IsActive &&
                x.PriceType == TourPriceType.Adult)
            .GroupBy(x => x.TourScheduleId)
            .Select(g => g.OrderByDescending(x => x.IsDefault).ThenBy(x => x.Price).First())
            .ToDictionaryAsync(x => x.TourScheduleId, ct);

        var items = schedules.Select(x =>
        {
            capacities.TryGetValue(x.Id, out var cap);
            adultPrices.TryGetValue(x.Id, out var adultPrice);

            return new TourSchedulesAdminListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                TourId = x.TourId,
                Code = x.Code,
                Name = x.Name,
                DepartureDate = x.DepartureDate,
                ReturnDate = x.ReturnDate,
                DepartureTime = x.DepartureTime,
                ReturnTime = x.ReturnTime,
                BookingOpenAt = x.BookingOpenAt,
                BookingCutoffAt = x.BookingCutoffAt,
                Status = x.Status,
                IsGuaranteedDeparture = x.IsGuaranteedDeparture,
                IsInstantConfirm = x.IsInstantConfirm,
                IsFeatured = x.IsFeatured,
                MinGuestsToOperate = x.MinGuestsToOperate,
                MaxGuests = x.MaxGuests,
                TotalSlots = cap?.TotalSlots,
                SoldSlots = cap?.SoldSlots,
                HeldSlots = cap?.HeldSlots,
                BlockedSlots = cap?.BlockedSlots,
                AvailableSlots = cap?.AvailableSlots,
                AdultPrice = adultPrice?.Price,
                CurrencyCode = adultPrice?.CurrencyCode,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            };
        }).ToList();

        return Ok(new TourSchedulesAdminPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourSchedulesAdminDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        IQueryable<TourSchedule> query = includeDeleted
            ? _db.TourSchedules.IgnoreQueryFilters()
            : _db.TourSchedules.Where(x => !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule not found." });

        var capacity = await _db.TourScheduleCapacities.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TourScheduleId == id, ct);

        var prices = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TourScheduleId == id)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.PriceType)
            .ToListAsync(ct);

        return Ok(MapDetail(entity, capacity, prices));
    }

    [HttpPost]
    public async Task<ActionResult<TourSchedulesAdminCreateResponse>> Create(
        [FromBody] TourSchedulesAdminCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        if (req.TourId == Guid.Empty)
            return BadRequest(new { message = "TourId is required." });

        await EnsureTourExistsAsync(tenantId, req.TourId, ct);
        await ValidateCreateAsync(tenantId, req.TourId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = req.TourId,
            Code = req.Code.Trim(),
            Name = NullIfWhiteSpace(req.Name),
            DepartureDate = req.DepartureDate,
            ReturnDate = req.ReturnDate,
            DepartureTime = req.DepartureTime,
            ReturnTime = req.ReturnTime,
            BookingOpenAt = req.BookingOpenAt,
            BookingCutoffAt = req.BookingCutoffAt,
            MeetingPointSummary = NullIfWhiteSpace(req.MeetingPointSummary),
            PickupSummary = NullIfWhiteSpace(req.PickupSummary),
            DropoffSummary = NullIfWhiteSpace(req.DropoffSummary),
            Notes = NullIfWhiteSpace(req.Notes),
            InternalNotes = NullIfWhiteSpace(req.InternalNotes),
            CancellationNotes = NullIfWhiteSpace(req.CancellationNotes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            Status = req.Status,
            IsGuaranteedDeparture = req.IsGuaranteedDeparture ?? false,
            IsInstantConfirm = req.IsInstantConfirm ?? false,
            IsFeatured = req.IsFeatured ?? false,
            MinGuestsToOperate = req.MinGuestsToOperate,
            MaxGuests = req.MaxGuests,
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourSchedules.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new TourSchedulesAdminCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TourSchedulesAdminUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule not found in current switched tenant." });

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(req.RowVersionBase64);
                _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = bytes;
            }
            catch
            {
                return BadRequest(new { message = "RowVersionBase64 is invalid." });
            }
        }

        await ValidateUpdateAsync(tenantId, entity.TourId, id, req, ct);

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = NullIfWhiteSpace(req.Name);

        if (req.DepartureDate.HasValue) entity.DepartureDate = req.DepartureDate.Value;
        if (req.ReturnDate.HasValue) entity.ReturnDate = req.ReturnDate.Value;
        if (req.DepartureTime.HasValue) entity.DepartureTime = req.DepartureTime;
        if (req.ReturnTime.HasValue) entity.ReturnTime = req.ReturnTime;
        if (req.BookingOpenAt.HasValue) entity.BookingOpenAt = req.BookingOpenAt;
        if (req.BookingCutoffAt.HasValue) entity.BookingCutoffAt = req.BookingCutoffAt;

        if (req.MeetingPointSummary is not null) entity.MeetingPointSummary = NullIfWhiteSpace(req.MeetingPointSummary);
        if (req.PickupSummary is not null) entity.PickupSummary = NullIfWhiteSpace(req.PickupSummary);
        if (req.DropoffSummary is not null) entity.DropoffSummary = NullIfWhiteSpace(req.DropoffSummary);
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.InternalNotes is not null) entity.InternalNotes = NullIfWhiteSpace(req.InternalNotes);
        if (req.CancellationNotes is not null) entity.CancellationNotes = NullIfWhiteSpace(req.CancellationNotes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);

        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.IsGuaranteedDeparture.HasValue) entity.IsGuaranteedDeparture = req.IsGuaranteedDeparture.Value;
        if (req.IsInstantConfirm.HasValue) entity.IsInstantConfirm = req.IsInstantConfirm.Value;
        if (req.IsFeatured.HasValue) entity.IsFeatured = req.IsFeatured.Value;
        if (req.MinGuestsToOperate.HasValue) entity.MinGuestsToOperate = req.MinGuestsToOperate;
        if (req.MaxGuests.HasValue) entity.MaxGuests = req.MaxGuests;
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour schedule was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
        => await ToggleActive(id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
        => await ToggleActive(id, false, ct);

    [HttpPost("{id:guid}/open")]
    public async Task<IActionResult> Open(Guid id, CancellationToken ct = default)
        => await ChangeStatus(id, TourScheduleStatus.Open, ct);

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct = default)
        => await ChangeStatus(id, TourScheduleStatus.Closed, ct);

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct = default)
        => await ChangeStatus(id, TourScheduleStatus.Cancelled, ct);

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct = default)
        => await ChangeStatus(id, TourScheduleStatus.Completed, ct);

    [HttpPost("{id:guid}/feature")]
    public async Task<IActionResult> Feature(Guid id, CancellationToken ct = default)
        => await ToggleFeatured(id, true, ct);

    [HttpPost("{id:guid}/unfeature")]
    public async Task<IActionResult> Unfeature(Guid id, CancellationToken ct = default)
        => await ToggleFeatured(id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule not found in current switched tenant." });

        entity.IsActive = isActive;

        if (!isActive && entity.Status == TourScheduleStatus.Open)
            entity.Status = TourScheduleStatus.Closed;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ChangeStatus(Guid id, TourScheduleStatus status, CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule not found in current switched tenant." });

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        if (status == TourScheduleStatus.Open)
            entity.IsActive = true;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleFeatured(Guid id, bool isFeatured, CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule not found in current switched tenant." });

        entity.IsFeatured = isFeatured;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task EnsureTourExistsAsync(Guid tenantId, Guid tourId, CancellationToken ct)
    {
        var exists = await _db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == tourId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour not found in current switched tenant.");
    }

    private async Task ValidateCreateAsync(Guid tenantId, Guid tourId, TourSchedulesAdminCreateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        var timeZone = await _tourLocalTimeService.ResolveTourTimeZoneAsync(tenantId, tourId, ct);

        ValidateBusinessRules(
            req.DepartureDate,
            req.ReturnDate,
            req.DepartureTime,
            req.ReturnTime,
            req.BookingOpenAt,
            req.BookingCutoffAt,
            req.MinGuestsToOperate,
            req.MaxGuests,
            timeZone);

        var duplicatedCode = await _db.TourSchedules.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == req.Code.Trim(), ct);

        if (duplicatedCode)
            throw new ArgumentException("Code already exists in current switched tenant tour.");
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid tourId,
        Guid currentId,
        TourSchedulesAdminUpdateRequest req,
        CancellationToken ct)
    {
        if (req.Code is not null && string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code cannot be empty.");

        if (req.Code is not null && req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        var current = await _db.TourSchedules.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == currentId, ct);

        var timeZone = await _tourLocalTimeService.ResolveTourTimeZoneAsync(tenantId, tourId, ct);

        ValidateBusinessRules(
            req.DepartureDate ?? current.DepartureDate,
            req.ReturnDate ?? current.ReturnDate,
            req.DepartureTime ?? current.DepartureTime,
            req.ReturnTime ?? current.ReturnTime,
            req.BookingOpenAt ?? current.BookingOpenAt,
            req.BookingCutoffAt ?? current.BookingCutoffAt,
            req.MinGuestsToOperate ?? current.MinGuestsToOperate,
            req.MaxGuests ?? current.MaxGuests,
            timeZone);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var duplicatedCode = await _db.TourSchedules.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == req.Code.Trim() && x.Id != currentId, ct);

            if (duplicatedCode)
                throw new ArgumentException("Code already exists in current switched tenant tour.");
        }
    }

    private void ValidateBusinessRules(
        DateOnly departureDate,
        DateOnly returnDate,
        TimeOnly? departureTime,
        TimeOnly? returnTime,
        DateTimeOffset? bookingOpenAt,
        DateTimeOffset? bookingCutoffAt,
        int? minGuestsToOperate,
        int? maxGuests,
        TimeZoneInfo? timeZone)
    {
        if (returnDate < departureDate)
            throw new ArgumentException("ReturnDate cannot be earlier than DepartureDate.");

        if (departureDate == returnDate && departureTime.HasValue && returnTime.HasValue && returnTime < departureTime)
            throw new ArgumentException("ReturnTime cannot be earlier than DepartureTime on the same day.");

        var bookingOpenLocal = bookingOpenAt.HasValue
            ? _tourLocalTimeService.ConvertToTourLocalDateTime(bookingOpenAt.Value, timeZone)
            : (DateTime?)null;

        var bookingCutoffLocal = bookingCutoffAt.HasValue
            ? _tourLocalTimeService.ConvertToTourLocalDateTime(bookingCutoffAt.Value, timeZone)
            : (DateTime?)null;

        if (bookingOpenLocal.HasValue && bookingCutoffLocal.HasValue && bookingCutoffLocal.Value < bookingOpenLocal.Value)
            throw new ArgumentException("BookingCutoffAt cannot be earlier than BookingOpenAt.");

        var departureAt = departureTime.HasValue
            ? departureDate.ToDateTime(departureTime.Value)
            : departureDate.ToDateTime(new TimeOnly(0, 0));

        if (bookingCutoffLocal.HasValue && bookingCutoffLocal.Value > departureAt)
            throw new ArgumentException("BookingCutoffAt cannot be later than departure date/time.");

        if (minGuestsToOperate.HasValue && minGuestsToOperate <= 0)
            throw new ArgumentException("MinGuestsToOperate must be greater than 0.");

        if (maxGuests.HasValue && maxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than 0.");

        if (minGuestsToOperate.HasValue && maxGuests.HasValue && minGuestsToOperate > maxGuests)
            throw new ArgumentException("MinGuestsToOperate cannot be greater than MaxGuests.");
    }

    private Guid RequireWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue || _tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Admin write operations require switched tenant context.");

        return _tenant.TenantId.Value;
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TourSchedulesAdminDetailDto MapDetail(
        TourSchedule x,
        TourScheduleCapacity? capacity,
        List<TourSchedulePrice> prices)
    {
        return new TourSchedulesAdminDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Code = x.Code,
            Name = x.Name,
            DepartureDate = x.DepartureDate,
            ReturnDate = x.ReturnDate,
            DepartureTime = x.DepartureTime,
            ReturnTime = x.ReturnTime,
            BookingOpenAt = x.BookingOpenAt,
            BookingCutoffAt = x.BookingCutoffAt,
            MeetingPointSummary = x.MeetingPointSummary,
            PickupSummary = x.PickupSummary,
            DropoffSummary = x.DropoffSummary,
            Notes = x.Notes,
            InternalNotes = x.InternalNotes,
            CancellationNotes = x.CancellationNotes,
            MetadataJson = x.MetadataJson,
            Status = x.Status,
            IsGuaranteedDeparture = x.IsGuaranteedDeparture,
            IsInstantConfirm = x.IsInstantConfirm,
            IsFeatured = x.IsFeatured,
            MinGuestsToOperate = x.MinGuestsToOperate,
            MaxGuests = x.MaxGuests,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null,
            Capacity = capacity is null
                ? null
                : new TourSchedulesAdminCapacitySummaryDto
                {
                    Id = capacity.Id,
                    TotalSlots = capacity.TotalSlots,
                    SoldSlots = capacity.SoldSlots,
                    HeldSlots = capacity.HeldSlots,
                    BlockedSlots = capacity.BlockedSlots,
                    AvailableSlots = capacity.AvailableSlots,
                    MinGuestsToOperate = capacity.MinGuestsToOperate,
                    MaxGuestsPerBooking = capacity.MaxGuestsPerBooking,
                    WarningThreshold = capacity.WarningThreshold,
                    Status = capacity.Status,
                    AllowWaitlist = capacity.AllowWaitlist,
                    AutoCloseWhenFull = capacity.AutoCloseWhenFull
                },
            Prices = prices.Select(p => new TourSchedulesAdminPriceSummaryDto
            {
                Id = p.Id,
                PriceType = p.PriceType,
                CurrencyCode = p.CurrencyCode,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                Taxes = p.Taxes,
                Fees = p.Fees,
                MinAge = p.MinAge,
                MaxAge = p.MaxAge,
                MinQuantity = p.MinQuantity,
                MaxQuantity = p.MaxQuantity,
                IsDefault = p.IsDefault,
                IsIncludedTax = p.IsIncludedTax,
                IsIncludedFee = p.IsIncludedFee,
                Label = p.Label,
                Notes = p.Notes,
                IsActive = p.IsActive,
                IsDeleted = p.IsDeleted
            }).ToList()
        };
    }
}

public sealed class TourSchedulesAdminPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourSchedulesAdminListItemDto> Items { get; set; } = new();
}

public sealed class TourSchedulesAdminListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public TourScheduleStatus Status { get; set; }
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public bool IsFeatured { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }
    public int? TotalSlots { get; set; }
    public int? SoldSlots { get; set; }
    public int? HeldSlots { get; set; }
    public int? BlockedSlots { get; set; }
    public int? AvailableSlots { get; set; }
    public decimal? AdultPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourSchedulesAdminDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public string? MeetingPointSummary { get; set; }
    public string? PickupSummary { get; set; }
    public string? DropoffSummary { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? CancellationNotes { get; set; }
    public string? MetadataJson { get; set; }
    public TourScheduleStatus Status { get; set; }
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public bool IsFeatured { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
    public TourSchedulesAdminCapacitySummaryDto? Capacity { get; set; }
    public List<TourSchedulesAdminPriceSummaryDto> Prices { get; set; } = new();
}

public sealed class TourSchedulesAdminCapacitySummaryDto
{
    public Guid Id { get; set; }
    public int TotalSlots { get; set; }
    public int SoldSlots { get; set; }
    public int HeldSlots { get; set; }
    public int BlockedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuestsPerBooking { get; set; }
    public int? WarningThreshold { get; set; }
    public TourCapacityStatus Status { get; set; }
    public bool AllowWaitlist { get; set; }
    public bool AutoCloseWhenFull { get; set; }
}

public sealed class TourSchedulesAdminPriceSummaryDto
{
    public Guid Id { get; set; }
    public TourPriceType PriceType { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefault { get; set; }
    public bool IsIncludedTax { get; set; }
    public bool IsIncludedFee { get; set; }
    public string? Label { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class TourSchedulesAdminCreateRequest
{
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public string? MeetingPointSummary { get; set; }
    public string? PickupSummary { get; set; }
    public string? DropoffSummary { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? CancellationNotes { get; set; }
    public string? MetadataJson { get; set; }
    public TourScheduleStatus Status { get; set; } = TourScheduleStatus.Draft;
    public bool? IsGuaranteedDeparture { get; set; }
    public bool? IsInstantConfirm { get; set; }
    public bool? IsFeatured { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class TourSchedulesAdminUpdateRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public string? MeetingPointSummary { get; set; }
    public string? PickupSummary { get; set; }
    public string? DropoffSummary { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? CancellationNotes { get; set; }
    public string? MetadataJson { get; set; }
    public TourScheduleStatus? Status { get; set; }
    public bool? IsGuaranteedDeparture { get; set; }
    public bool? IsInstantConfirm { get; set; }
    public bool? IsFeatured { get; set; }
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class TourSchedulesAdminCreateResponse
{
    public Guid Id { get; set; }
}
