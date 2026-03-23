using HTrack.Api.Utilities;

namespace Htrack.Api.Tests;

public class AttendanceBusinessRulesTests
{
    [Theory]
    [InlineData("2026-03-14", "2026-03-01", "2026-03-15")]
    [InlineData("2026-03-16", "2026-03-16", "2026-03-31")]
    [InlineData("2026-02-28", "2026-02-16", "2026-02-28")]
    [InlineData("2024-02-29", "2024-02-16", "2024-02-29")]
    public void GetCurrentHalfMonthRange_ReturnsExpectedBounds(string todayRaw, string expectedStartRaw, string expectedEndRaw)
    {
        var today = DateOnly.Parse(todayRaw);
        var expectedStart = DateOnly.Parse(expectedStartRaw);
        var expectedEnd = DateOnly.Parse(expectedEndRaw);

        var actual = AttendanceBusinessRules.GetCurrentHalfMonthRange(today);

        Assert.Equal(expectedStart, actual.Start);
        Assert.Equal(expectedEnd, actual.End);
    }

    [Fact]
    public void GetMonthToDateRange_StartsAtFirstDayOfMonth()
    {
        var today = new DateOnly(2026, 3, 23);

        var actual = AttendanceBusinessRules.GetMonthToDateRange(today);

        Assert.Equal(new DateOnly(2026, 3, 1), actual.Start);
        Assert.Equal(today, actual.End);
    }

    [Fact]
    public void GetUzbekistanUtcRange_UsesExclusiveUpperBound()
    {
        var (startUtc, endUtcExclusive) = TimeHelper.GetUzbekistanUtcRange(
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 15));

        Assert.Equal(new DateTime(2026, 2, 28, 19, 0, 0, DateTimeKind.Utc), startUtc);
        Assert.Equal(new DateTime(2026, 3, 15, 19, 0, 0, DateTimeKind.Utc), endUtcExclusive);
    }
}
