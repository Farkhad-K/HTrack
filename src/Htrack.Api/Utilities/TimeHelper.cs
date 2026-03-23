namespace HTrack.Api.Utilities;

public static class TimeHelper
{
    private static readonly TimeZoneInfo UzbekistanTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");

    public static DateTime GetUzbekistanNow()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UzbekistanTimeZone);

    public static DateOnly GetUzbekistanToday()
        => DateOnly.FromDateTime(GetUzbekistanNow());

    public static DateTime ToUzbekistanTime(DateTime utcTime)
    {
        return DateTime.SpecifyKind(utcTime, DateTimeKind.Utc) == utcTime
            ? TimeZoneInfo.ConvertTimeFromUtc(utcTime, UzbekistanTimeZone)
            : TimeZoneInfo.ConvertTime(utcTime, UzbekistanTimeZone);
    }

    public static DateTime ToUtcFromUzbekistanLocal(DateOnly date, TimeOnly time)
    {
        var localDateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, UzbekistanTimeZone);
    }

    public static (DateTime StartUtc, DateTime EndUtcExclusive) GetUzbekistanUtcRange(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new ArgumentOutOfRangeException(nameof(to), "`to` date must be on or after `from` date.");

        var startUtc = ToUtcFromUzbekistanLocal(from, TimeOnly.MinValue);
        var endUtcExclusive = ToUtcFromUzbekistanLocal(to.AddDays(1), TimeOnly.MinValue);
        return (startUtc, endUtcExclusive);
    }
}
