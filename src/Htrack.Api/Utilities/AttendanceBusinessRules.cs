namespace HTrack.Api.Utilities;

public static class AttendanceBusinessRules
{
    public static (DateOnly Start, DateOnly End) GetMonthToDateRange(DateOnly today)
        => (new DateOnly(today.Year, today.Month, 1), today);

    public static (DateOnly Start, DateOnly End) GetCurrentHalfMonthRange(DateOnly today)
    {
        if (today.Day == 16)
        {
            // 16-sana: Oyning birinchi yarmi (1-15) yopilgan hisobot
            return (new DateOnly(today.Year, today.Month, 1), new DateOnly(today.Year, today.Month, 15));
        }

        if (today.Day == 1)
        {
            // 1-sana: O'tgan oyning ikkinchi yarmi (16-...) yopilgan hisobot
            var prevMonth = today.AddMonths(-1);
            return (new DateOnly(prevMonth.Year, prevMonth.Month, 16),
                    new DateOnly(prevMonth.Year, prevMonth.Month, DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month)));
        }

        // Oddiy holat: joriy yarim oylikni qaytarish (oyning 1-15 yoki 16-oxirgi kunlari)
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
