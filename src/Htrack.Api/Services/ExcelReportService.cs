using System.Globalization;
using ClosedXML.Excel;
using HTrack.Api.Data;
using HTrack.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Services;

public partial class ExcelReportService(IHTrackDbContext context) : IExcelReportService
{
    private readonly CultureInfo _uzCulture = new("uz-UZ");

    private record AttendanceRow(string EmployeeName, string RFID, DateTime CheckIn, DateTime? CheckOut, TimeSpan Duration);

    // ── Company-wide reports ────────────────────────────────────────────────

    public async Task<(MemoryStream Stream, string FileName)> GetLastMonthReportAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new InvalidOperationException($"Company {companyId} not found.");

        var reportDate = DateTime.UtcNow.AddMonths(-1);
        var rows = await QueryAttendances(companyId,
            a => a.CheckIn.Month == reportDate.Month && a.CheckIn.Year == reportDate.Year, ct);

        var monthName = reportDate.ToString("MMMM", _uzCulture);
        var label = $"{monthName} {reportDate.Year}";
        var fileName = $"{company.Name!}_{monthName}_{reportDate.Year}_davomat.xlsx";

        return BuildReport(rows, company.Name!, label, fileName);
    }

    public async Task<(MemoryStream Stream, string FileName)> Get15DayReportAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new InvalidOperationException($"Company {companyId} not found.");

        var now = DateTime.UtcNow;
        var startDay = now.Day <= 15 ? 1 : 16;
        var endDay = now.Day <= 15 ? 15 : DateTime.DaysInMonth(now.Year, now.Month);
        var periodName = startDay == 1 ? "1-15" : "16-oy oxiri";

        var rows = await QueryAttendances(companyId,
            a => a.CheckIn.Month == now.Month && a.CheckIn.Year == now.Year
              && a.CheckIn.Day >= startDay && a.CheckIn.Day <= endDay, ct);

        var monthName = now.ToString("MMMM", _uzCulture);
        var label = $"{monthName} {now.Year} ({periodName})";
        var fileName = $"{company.Name!}_{monthName}_{now.Year}_davomat_{startDay}dan{endDay}.xlsx";

        return BuildReport(rows, company.Name!, label, fileName);
    }

    public async Task<(MemoryStream Stream, string FileName)> GetFromStartToTodayAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new InvalidOperationException($"Company {companyId} not found.");

        var now = DateTime.UtcNow;
        var rows = await QueryAttendances(companyId,
            a => a.CheckIn.Month == now.Month && a.CheckIn.Year == now.Year
              && a.CheckIn.Day >= 1 && a.CheckIn.Day <= now.Day, ct);

        var monthName = now.ToString("MMMM", _uzCulture);
        var label = $"{monthName} {now.Year} (1-{now.Day})";
        var fileName = $"{company.Name!}_{monthName}_{now.Year}_davomat_1dan{now.Day}.xlsx";

        return BuildReport(rows, company.Name!, label, fileName);
    }

    public async Task<(MemoryStream Stream, string FileName)> GetCustomRangeReportAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new InvalidOperationException($"Company {companyId} not found.");

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var rows = await QueryAttendances(companyId,
            a => a.CheckIn >= fromUtc && a.CheckIn <= toUtc, ct);

        var label = $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}";
        var fileName = $"{company.Name!}_davomat_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";

        return BuildReport(rows, company.Name!, label, fileName);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private async Task<List<AttendanceRow>> QueryAttendances(
        Guid companyId,
        System.Linq.Expressions.Expression<Func<HTrack.Api.Entities.Attendance, bool>> filter,
        CancellationToken ct)
    {
        var raw = await context.Attendances
            .Where(a => a.Employee!.CompanyId == companyId)
            .Where(filter)
            .Select(a => new
            {
                EmployeeName = a.Employee!.Name,
                RFID = a.Employee.RFIDCardUID,
                a.CheckIn,
                a.CheckOut,
                a.Duration
            })
            .OrderBy(a => a.EmployeeName)
            .ThenBy(a => a.CheckIn)
            .ToListAsync(ct);

        return raw
            .DistinctBy(a => new { a.EmployeeName, a.CheckIn, a.CheckOut })
            .Select(a => new AttendanceRow(a.EmployeeName!, a.RFID!, a.CheckIn, a.CheckOut, a.Duration))
            .ToList();
    }

    private async Task<List<AttendanceRow>> QueryAttendancesForEmployee(
        Guid employeeId,
        System.Linq.Expressions.Expression<Func<HTrack.Api.Entities.Attendance, bool>> filter,
        CancellationToken ct)
    {
        var raw = await context.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .Where(filter)
            .Select(a => new
            {
                EmployeeName = a.Employee!.Name,
                RFID = a.Employee.RFIDCardUID,
                a.CheckIn,
                a.CheckOut,
                a.Duration
            })
            .OrderBy(a => a.CheckIn)
            .ToListAsync(ct);

        return raw
            .DistinctBy(a => new { a.CheckIn, a.CheckOut })
            .Select(a => new AttendanceRow(a.EmployeeName!, a.RFID!, a.CheckIn, a.CheckOut, a.Duration))
            .ToList();
    }

    private (MemoryStream Stream, string FileName) BuildReport(
        List<AttendanceRow> rows, string companyName, string label, string fileName)
    {
        using var workbook = BuildWorkbook(rows, companyName, label);
        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return (ms, fileName);
    }

    private XLWorkbook BuildWorkbook(List<AttendanceRow> rows, string companyName, string label)
    {
        var workbook = new XLWorkbook();
        BuildSummarySheet(workbook, rows, companyName, label);
        BuildDetailSheet(workbook, rows);
        return workbook;
    }

    private void BuildSummarySheet(XLWorkbook workbook, List<AttendanceRow> rows, string companyName, string label)
    {
        var ws = workbook.Worksheets.Add("Xulosa");

        ws.Cell(1, 1).Value = $"{companyName} — {label}";
        ws.Range(1, 1, 1, 4).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(2, 1).Value = "Xodim";
        ws.Cell(2, 2).Value = "Kunlar soni";
        ws.Cell(2, 3).Value = "Jami soat";
        ws.Cell(2, 4).Value = "O'rtacha soat/kun";

        var headerRange = ws.Range(2, 1, 2, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        ws.SheetView.FreezeRows(2);

        int row = 3;
        var grouped = rows.GroupBy(r => r.EmployeeName);

        foreach (var group in grouped)
        {
            var totalDuration = group.Aggregate(TimeSpan.Zero, (acc, r) => acc + r.Duration);
            var distinctDays = group.Select(r => r.CheckIn.Date).Distinct().Count();
            var avgHours = distinctDays > 0 ? totalDuration.TotalHours / distinctDays : 0;

            ws.Cell(row, 1).Value = group.Key;
            ws.Cell(row, 2).Value = distinctDays;
            ws.Cell(row, 3).Value = $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}";
            ws.Cell(row, 4).Value = $"{avgHours:F1}";
            row++;
        }

        ws.Columns().AdjustToContents();
        int lastRow = row - 1; // last data row (or row 2 if no groups)
        ws.Range(1, 1, lastRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(1, 1, lastRow, 4).Style.Border.InsideBorder = XLBorderStyleValues.Medium;
    }

    private void BuildDetailSheet(XLWorkbook workbook, List<AttendanceRow> rows)
    {
        var ws = workbook.Worksheets.Add("Batafsil");

        ws.Cell(1, 1).Value = "Ishchi";
        ws.Cell(1, 2).Value = "Kelgan vaqti";
        ws.Cell(1, 3).Value = "Ketgan vaqti";
        ws.Cell(1, 4).Value = "Ishlagan soati";

        var headerRange = ws.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        ws.SheetView.FreezeRows(1);

        int row = 2;
        int lastRow = 1;
        var grouped = rows.GroupBy(r => r.EmployeeName);

        foreach (var group in grouped)
        {
            TimeSpan totalDuration = TimeSpan.Zero;
            bool isFirstRow = true;

            foreach (var a in group)
            {
                var checkIn = TimeHelper.ToUzbekistanTime(a.CheckIn);
                var checkOut = a.CheckOut.HasValue ? TimeHelper.ToUzbekistanTime(a.CheckOut.Value) : (DateTime?)null;

                ws.Cell(row, 1).Value = isFirstRow ? $"{a.EmployeeName} - {a.RFID}" : "";
                ws.Cell(row, 2).Value = checkIn.ToString("d-MMMM yyyy HH:mm", _uzCulture);
                ws.Cell(row, 3).Value = checkOut?.ToString("d-MMMM yyyy HH:mm", _uzCulture) ?? "Yo'q";
                ws.Cell(row, 4).Value = a.Duration.ToString(@"hh\:mm");

                // Conditional row color based on duration
                if (a.Duration < TimeSpan.FromHours(4))
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE0E0");
                else if (a.Duration >= TimeSpan.FromHours(8))
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0FFE0");

                // First row of each employee group: green separator across all 4 columns
                if (isFirstRow)
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGreen;

                totalDuration += a.Duration;
                isFirstRow = false;
                row++;
            }

            ws.Cell(row, 3).Value = "Jami";
            ws.Cell(row, 4).Value = $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}";
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;
            ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Yellow;

            lastRow = row;
            row += 2;
        }

        ws.Columns().AdjustToContents();
        ws.Range(1, 1, lastRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(1, 1, lastRow, 4).Style.Border.InsideBorder = XLBorderStyleValues.Medium;
    }

}
