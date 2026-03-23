using ClosedXML.Excel;
using HTrack.Api.Data;
using HTrack.Api.Entities;
using HTrack.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Htrack.Api.Tests;

public class ExcelReportServiceTests
{
    [Fact]
    public async Task CustomEmployeeReport_ShowsManualMarkerAndAnomalyWithoutChangingHours()
    {
        var companyId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Co",
            TgChatID = 1
        });
        db.Employees.Add(new Employee
        {
            Id = employeeId,
            CompanyId = companyId,
            Name = "Ali Valiyev",
            RFIDCardUID = "ABC123"
        });
        db.Attendances.Add(new Attendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CheckIn = new DateTime(2026, 3, 10, 4, 0, 0, DateTimeKind.Utc),
            CheckOut = new DateTime(2026, 3, 10, 22, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(18),
            CheckInSource = AttendanceEntrySource.Manual,
            CheckOutSource = AttendanceEntrySource.Device
        });
        await ((IHTrackDbContext)db).SaveChangesAsync();

        var service = new ExcelReportService(db);
        var (stream, _) = await service.GetEmployeeCustomRangeReportAsync(companyId, "ABC123", new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 10));

        using var workbook = new XLWorkbook(stream);
        var summary = workbook.Worksheet("Xulosa");
        var detail = workbook.Worksheet("Batafsil");

        Assert.Equal(1d, summary.Cell(2, 6).GetDouble());
        Assert.Equal("Qo'lda kelish", detail.Cell(3, 6).GetString());
        Assert.Contains("Uzoq smena", detail.Cell(3, 5).GetString());
        Assert.Equal("18:00", detail.Cell(3, 3).GetString());
    }

    [Fact]
    public async Task CustomEmployeeReport_KeepsOpenShiftVisibleWithoutFalseAnomaly()
    {
        var companyId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Co",
            TgChatID = 1
        });
        db.Employees.Add(new Employee
        {
            Id = employeeId,
            CompanyId = companyId,
            Name = "Vali Aliyev",
            RFIDCardUID = "XYZ789"
        });
        db.Attendances.Add(new Attendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CheckIn = new DateTime(2026, 3, 11, 3, 30, 0, DateTimeKind.Utc),
            Duration = TimeSpan.Zero,
            CheckInSource = AttendanceEntrySource.Manual
        });
        await ((IHTrackDbContext)db).SaveChangesAsync();

        var service = new ExcelReportService(db);
        var (stream, _) = await service.GetEmployeeCustomRangeReportAsync(companyId, "XYZ789", new DateOnly(2026, 3, 11), new DateOnly(2026, 3, 11));

        using var workbook = new XLWorkbook(stream);
        var detail = workbook.Worksheet("Batafsil");

        Assert.Equal("—", detail.Cell(3, 2).GetString());
        Assert.Equal(string.Empty, detail.Cell(3, 5).GetString());
        Assert.Equal("Qo'lda kelish", detail.Cell(3, 6).GetString());
    }

    private static HTrackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HTrackDbContext(options);
    }
}
