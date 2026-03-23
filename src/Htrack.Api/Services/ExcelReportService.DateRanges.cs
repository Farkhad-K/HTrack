using HTrack.Api.Utilities;

namespace HTrack.Api.Services;

public partial class ExcelReportService
{
    private static DateOnly GetBusinessToday()
        => TimeHelper.GetUzbekistanToday();

    private static DateOnly GetBusinessDate(DateTime utcDateTime)
        => DateOnly.FromDateTime(TimeHelper.ToUzbekistanTime(utcDateTime));

    private static (DateOnly Start, DateOnly End) GetMonthToDateRange(DateOnly today)
        => AttendanceBusinessRules.GetMonthToDateRange(today);

    private static (DateOnly Start, DateOnly End) GetCurrentHalfMonthRange(DateOnly today)
        => AttendanceBusinessRules.GetCurrentHalfMonthRange(today);

    private static (DateOnly Start, DateOnly End) GetFullMonthRange(DateOnly dateInMonth)
        => AttendanceBusinessRules.GetFullMonthRange(dateInMonth);

    private static string FormatDuration(TimeSpan duration)
        => $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";

    private async Task<List<AttendanceRow>> QueryAttendances(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (startUtc, endUtcExclusive) = TimeHelper.GetUzbekistanUtcRange(from, to);
        return await QueryAttendances(companyId, a => a.CheckIn >= startUtc && a.CheckIn < endUtcExclusive, ct);
    }

    private async Task<List<AttendanceRow>> QueryAttendancesForEmployee(Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (startUtc, endUtcExclusive) = TimeHelper.GetUzbekistanUtcRange(from, to);
        return await QueryAttendancesForEmployee(employeeId, a => a.CheckIn >= startUtc && a.CheckIn < endUtcExclusive, ct);
    }
}
