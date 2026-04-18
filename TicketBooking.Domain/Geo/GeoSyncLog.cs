// FILE #048: TicketBooking.Domain/Geo/GeoSyncLog.cs
using System;

namespace TicketBooking.Domain.Geo
{
    /// <summary>
    /// geo.GeoSyncLogs
    /// Lưu lịch sử sync từ provinces.open-api.vn để audit khi demo/bảo vệ.
    /// </summary>
    public sealed class GeoSyncLog
    {
        public Guid Id { get; set; }                 // NEWSEQUENTIALID (Phase 5 defaults)
        public string Source { get; set; } = "provinces.open-api.vn";
        public string Url { get; set; } = "";
        public int Depth { get; set; }               // 1..3

        public bool IsSuccess { get; set; }
        public int HttpStatus { get; set; }

        public int ProvincesInserted { get; set; }
        public int ProvincesUpdated { get; set; }
        public int DistrictsInserted { get; set; }
        public int DistrictsUpdated { get; set; }
        public int WardsInserted { get; set; }
        public int WardsUpdated { get; set; }

        public string? ErrorMessage { get; set; }    // ngắn gọn
        public string? ErrorDetail { get; set; }     // có thể dài (stack/body)

        // Standard columns (CreatedAt default +07 via Phase 5 defaults)
        public DateTimeOffset CreatedAt { get; set; }
    }
}