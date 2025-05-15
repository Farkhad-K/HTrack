namespace HTrack.Api.Utilities;

public static class TimeHelper
{
    private static readonly TimeZoneInfo UzbekistanTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");

    public static DateTime ToUzbekistanTime(DateTime utcTime)
    {
        return DateTime.SpecifyKind(utcTime, DateTimeKind.Utc) == utcTime
            ? TimeZoneInfo.ConvertTimeFromUtc(utcTime, UzbekistanTimeZone)
            : TimeZoneInfo.ConvertTime(utcTime, UzbekistanTimeZone);
    }
}
