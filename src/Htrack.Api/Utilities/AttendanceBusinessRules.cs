namespace HTrack.Api.Utilities;

public static class AttendanceBusinessRules
{
    public static (DateOnly Start, DateOnly End) GetMonthToDateRange(DateOnly today)
        => (new DateOnly(today.Year, today.Month, 1), today);

    public static (DateOnly Start, DateOnly End) GetCurrentHalfMonthRange(DateOnly today)
    {
        var startDay = today.Day <= 15 ? 1 : 16;
        var endDay = today.Day <= 15 ? 15 : DateTime.DaysInMonth(today.Year, today.Month);
        return (new DateOnly(today.Year, today.Month, startDay), new DateOnly(today.Year, today.Month, endDay));
    }

    public static (DateOnly Start, DateOnly End) GetFullMonthRange(DateOnly dateInMonth)
    {
        var start = new DateOnly(dateInMonth.Year, dateInMonth.Month, 1);
        var end = new DateOnly(dateInMonth.Year, dateInMonth.Month, DateTime.DaysInMonth(dateInMonth.Year, dateInMonth.Month));
        return (start, end);
    }
}
