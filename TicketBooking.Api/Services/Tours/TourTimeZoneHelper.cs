namespace TicketBooking.Api.Services.Tours;

public static class TourTimeZoneHelper
{
    public static DateTimeOffset ConvertUtcNowToLocationTime(string? timeZoneId)
    {
        var utcNow = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return utcNow;

        var timeZone = TryResolveTimeZone(timeZoneId.Trim());
        return timeZone is null
            ? utcNow
            : TimeZoneInfo.ConvertTime(utcNow, timeZone);
    }

    public static DateTime ConvertToTourLocalDateTime(DateTimeOffset value, TimeZoneInfo? timeZone)
        => timeZone is null ? value.UtcDateTime : TimeZoneInfo.ConvertTime(value, timeZone).DateTime;

    public static TimeZoneInfo? TryResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            var windowsId = timeZoneId.Trim() switch
            {
                "Asia/Ho_Chi_Minh" or "Asia/Bangkok" => "SE Asia Standard Time",
                "Asia/Singapore" => "Singapore Standard Time",
                "Asia/Tokyo" => "Tokyo Standard Time",
                _ => null
            };

            if (windowsId is null)
                return null;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            }
            catch
            {
                return null;
            }
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
