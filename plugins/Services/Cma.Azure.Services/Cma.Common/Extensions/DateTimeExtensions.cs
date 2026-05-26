namespace Cma.Common.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToEasternTimeZone(this DateTime datetimeUtc)
    {
        var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(datetimeUtc, easternZone);
    }
}