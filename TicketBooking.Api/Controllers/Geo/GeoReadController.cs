// FILE #122 (FIX INT CODE): TicketBooking.Api/Controllers/Geo/GeoReadController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Geo;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Geo
{
    /// <summary>
    /// Read-only Geo endpoints for dropdown/search:
    /// - Provinces
    /// - Districts by provinceCode (int)
    /// - Wards by districtCode (int)
    ///
    /// FIX:
    /// - Code is INT (provinces.open-api.vn)
    /// - Use db.Set<TEntity>() so it doesn't depend on DbSet property names.
    /// - Search uses EF.Functions.Like on Name only (because Code is int).
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/geo")]
    [AllowAnonymous]
    public sealed class GeoReadController : ControllerBase
    {
        private readonly AppDbContext _db;

        public GeoReadController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> Provinces([FromQuery] string? q, CancellationToken ct = default)
        {
            IQueryable<Province> query = _db.Set<Province>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = $"%{q.Trim()}%";
                query = query.Where(x => EF.Functions.Like(x.Name, like));
            }

            var items = await query
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Code,   // int
                    x.Name,
                    x.Type
                })
                .ToListAsync(ct);

            return Ok(new { count = items.Count, items });
        }

        [HttpGet("districts")]
        public async Task<IActionResult> Districts(
            [FromQuery] int provinceCode,
            [FromQuery] string? q,
            CancellationToken ct = default)
        {
            if (provinceCode <= 0)
                return BadRequest(new { message = "provinceCode phải lớn hơn 0." });

            var provinceId = await _db.Set<Province>().AsNoTracking()
                .Where(x => x.Code == provinceCode)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (provinceId == Guid.Empty)
                return NotFound(new { message = "Không tìm thấy tỉnh/thành." });

            IQueryable<District> query = _db.Set<District>().AsNoTracking()
                .Where(x => x.ProvinceId == provinceId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = $"%{q.Trim()}%";
                query = query.Where(x => EF.Functions.Like(x.Name, like));
            }

            var items = await query
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.ProvinceId,
                    x.Code,   // int
                    x.Name,
                    x.Type
                })
                .ToListAsync(ct);

            return Ok(new { count = items.Count, items });
        }

        [HttpGet("wards")]
        public async Task<IActionResult> Wards(
            [FromQuery] int districtCode,
            [FromQuery] string? q,
            CancellationToken ct = default)
        {
            if (districtCode <= 0)
                return BadRequest(new { message = "districtCode phải lớn hơn 0." });

            var districtId = await _db.Set<District>().AsNoTracking()
                .Where(x => x.Code == districtCode)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (districtId == Guid.Empty)
                return NotFound(new { message = "Không tìm thấy quận/huyện." });

            IQueryable<Ward> query = _db.Set<Ward>().AsNoTracking()
                .Where(x => x.DistrictId == districtId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = $"%{q.Trim()}%";
                query = query.Where(x => EF.Functions.Like(x.Name, like));
            }

            var items = await query
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.DistrictId,
                    x.Code,   // int
                    x.Name,
                    x.Type
                })
                .ToListAsync(ct);

            return Ok(new { count = items.Count, items });
        }

        /// <summary>
        /// Convenience lookup: ward details by wardCode (int), including district & province.
        /// </summary>
        [HttpGet("ward-lookup")]
        public async Task<IActionResult> WardLookup([FromQuery] int wardCode, CancellationToken ct = default)
        {
            if (wardCode <= 0)
                return BadRequest(new { message = "wardCode phải lớn hơn 0." });

            var ward = await _db.Set<Ward>().AsNoTracking()
                .Where(w => w.Code == wardCode)
                .Select(w => new
                {
                    w.Id,
                    w.Code,
                    w.Name,
                    w.Type,
                    w.DistrictId
                })
                .FirstOrDefaultAsync(ct);

            if (ward is null)
                return NotFound(new { message = "Không tìm thấy phường/xã." });

            var district = await _db.Set<District>().AsNoTracking()
                .Where(d => d.Id == ward.DistrictId)
                .Select(d => new
                {
                    d.Id,
                    d.Code,
                    d.Name,
                    d.Type,
                    d.ProvinceId
                })
                .FirstOrDefaultAsync(ct);

            if (district is null)
                return Ok(new { ward, district = (object?)null, province = (object?)null });

            var province = await _db.Set<Province>().AsNoTracking()
                .Where(p => p.Id == district.ProvinceId)
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Type
                })
                .FirstOrDefaultAsync(ct);

            return Ok(new { ward, district, province });
        }
    }
}
