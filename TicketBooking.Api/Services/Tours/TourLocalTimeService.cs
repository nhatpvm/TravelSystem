using Microsoft.EntityFrameworkCore;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourLocalTimeService
{
    private readonly AppDbContext _db;

    public TourLocalTimeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DateTimeOffset> ResolveCurrentTimeAsync(Guid? locationId, CancellationToken ct)
    {
        if (!locationId.HasValue || locationId.Value == Guid.Empty)
            return DateTimeOffset.UtcNow;

        var timeZoneId = await _db.Locations
            .AsNoTracking()
            .Where(x => x.Id == locationId.Value && x.IsActive && !x.IsDeleted)
            .Select(x => x.TimeZone)
            .FirstOrDefaultAsync(ct);

        return TourTimeZoneHelper.ConvertUtcNowToLocationTime(timeZoneId);
    }

    public async Task<TimeZoneInfo?> ResolveTourTimeZoneAsync(Guid tenantId, Guid tourId, CancellationToken ct)
    {
        var locationId = await _db.Tours.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == tourId && !x.IsDeleted)
            .Select(x => x.PrimaryLocationId)
            .FirstOrDefaultAsync(ct);

        if (!locationId.HasValue || locationId.Value == Guid.Empty)
            return null;

        var timeZoneId = await _db.Locations
            .AsNoTracking()
            .Where(x => x.Id == locationId.Value && x.IsActive && !x.IsDeleted)
            .Select(x => x.TimeZone)
            .FirstOrDefaultAsync(ct);

        return TourTimeZoneHelper.TryResolveTimeZone(timeZoneId);
    }

    public DateTime ConvertToTourLocalDateTime(DateTimeOffset value, TimeZoneInfo? timeZone)
        => TourTimeZoneHelper.ConvertToTourLocalDateTime(value, timeZone);
}
