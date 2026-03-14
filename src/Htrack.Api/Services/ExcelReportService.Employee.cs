using ClosedXML.Excel;
using HTrack.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Services;

public partial class ExcelReportService
{
    // ── Per-employee reports ────────────────────────────────────────────────

    public async Task<(MemoryStream Stream, string FileName)> GetEmployeeMonthToDateReportAsync(
        Guid companyId, string rfidCardUID, CancellationToken ct = default)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.RFIDCardUID == rfidCardUID, ct)
            ?? throw new InvalidOperationException($"Employee with RFID '{rfidCardUID}' not found.");

        var now = DateTime.UtcNow;
        var rows = await QueryAttendancesForEmployee(employee.Id,
            a => a.CheckIn.Month == now.Month && a.CheckIn.Year == now.Year
              && a.CheckIn.Day >= 1 && a.CheckIn.Day <= now.Day, ct);

        var monthName = now.ToString("MMMM", _uzCulture);
        var fileName = $"{employee.Name!}_{monthName}_{now.Year}_1dan{now.Day}.xlsx";

        return BuildEmployeeReport(employee.Name!, rows, fileName);
    }

    public async Task<(MemoryStream Stream, string FileName)> GetEmployee15DayReportAsync(
        Guid companyId, string rfidCardUID, CancellationToken ct = default)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.RFIDCardUID == rfidCardUID, ct)
            ?? throw new InvalidOperationException($"Employee with RFID '{rfidCardUID}' not found.");

        var now = DateTime.UtcNow;
        var startDay = now.Day <= 15 ? 1 : 16;
        var endDay = now.Day <= 15 ? 15 : DateTime.DaysInMonth(now.Year, now.Month);

        var rows = await QueryAttendancesForEmployee(employee.Id,
            a => a.CheckIn.Month == now.Month && a.CheckIn.Year == now.Year
              && a.CheckIn.Day >= startDay && a.CheckIn.Day <= endDay, ct);

        var monthName = now.ToString("MMMM", _uzCulture);
        var fileName = $"{employee.Name!}_{monthName}_{now.Year}_{startDay}dan{endDay}.xlsx";

        return BuildEmployeeReport(employee.Name!, rows, fileName);
    }

    public async Task<(MemoryStream Stream, string FileName)> GetEmployeeCustomRangeReportAsync(
        Guid companyId, string rfidCardUID, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.RFIDCardUID == rfidCardUID, ct)
            ?? throw new InvalidOperationException($"Employee with RFID '{rfidCardUID}' not found.");

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var rows = await QueryAttendancesForEmployee(employee.Id,
            a => a.CheckIn >= fromUtc && a.CheckIn <= toUtc, ct);

        var fileName = $"{employee.Name!}_davomat_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";

        return BuildEmployeeReport(employee.Name!, rows, fileName);
    }

    // ── Private employee helpers ─────────────────────────────────────────────

    private (MemoryStream Stream, string FileName) BuildEmployeeReport(
        string employeeName, List<AttendanceRow> rows, string fileName)
    {
        using var workbook = new XLWorkbook();
        BuildEmployeeSummarySheet(workbook, employeeName, rows);
        BuildEmployeeDetailSheet(workbook, employeeName, rows);
        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return (ms, fileName);
    }

    private void BuildEmployeeSummarySheet(XLWorkbook workbook, string employeeName, List<AttendanceRow> rows)
    {
        var ws = workbook.Worksheets.Add("Xulosa");

        ws.Cell(1, 1).Value = "Xodim";
        ws.Cell(1, 2).Value = "Kunlar soni";
        ws.Cell(1, 3).Value = "Jami soat";
        ws.Cell(1, 4).Value = "O'rtacha soat/kun";

        var headerRange = ws.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        var totalDuration = rows.Aggregate(TimeSpan.Zero, (acc, r) => acc + r.Duration);
        var distinctDays = rows.Select(r => TimeHelper.ToUzbekistanTime(r.CheckIn).Date).Distinct().Count();
        var avgHours = distinctDays > 0 ? totalDuration.TotalHours / distinctDays : 0;

        ws.Cell(2, 1).Value = employeeName;
        ws.Cell(2, 2).Value = distinctDays;
        ws.Cell(2, 3).Value = $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}";
        ws.Cell(2, 4).Value = $"{avgHours:F1}";

        ws.Columns().AdjustToContents();
        ws.Range(1, 1, 2, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(1, 1, 2, 4).Style.Border.InsideBorder = XLBorderStyleValues.Medium;
    }

    private void BuildEmployeeDetailSheet(XLWorkbook workbook, string employeeName, List<AttendanceRow> rows)
    {
        var ws = workbook.Worksheets.Add("Batafsil");

        // Row 1: employee name title
        ws.Cell(1, 1).Value = employeeName;
        ws.Range(1, 1, 1, 4).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Row 2: headers
        ws.Cell(2, 1).Value = "Kelish";
        ws.Cell(2, 2).Value = "Ketish";
        ws.Cell(2, 3).Value = "Smena soati";
        ws.Cell(2, 4).Value = "Kun jami";

        var headerRange = ws.Range(2, 1, 2, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        ws.SheetView.FreezeRows(2);

        int row = 3;
        TimeSpan grandTotal = TimeSpan.Zero;

        // Group by UZ-local date, order by date then check-in time
        var byDay = rows
            .OrderBy(a => TimeHelper.ToUzbekistanTime(a.CheckIn))
            .GroupBy(a => TimeHelper.ToUzbekistanTime(a.CheckIn).Date)
            .OrderBy(g => g.Key);

        foreach (var dayGroup in byDay)
        {
            TimeSpan dayTotal = TimeSpan.Zero;

            foreach (var a in dayGroup)
            {
                var checkIn = TimeHelper.ToUzbekistanTime(a.CheckIn);
                var checkOut = a.CheckOut.HasValue ? TimeHelper.ToUzbekistanTime(a.CheckOut.Value) : (DateTime?)null;

                ws.Cell(row, 1).Value = checkIn.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 2).Value = checkOut?.ToString("dd.MM.yyyy HH:mm") ?? "—";
                ws.Cell(row, 3).Value = $"{(int)a.Duration.TotalHours:D2}:{a.Duration.Minutes:D2}";
                ws.Cell(row, 4).Value = "";

                // Duration coloring — only when checkout is present
                if (a.CheckOut.HasValue)
                {
                    if (a.Duration < TimeSpan.FromHours(4))
                        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE0E0");
                    else if (a.Duration >= TimeSpan.FromHours(8))
                        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0FFE0");
                }

                dayTotal += a.Duration;
                grandTotal += a.Duration;
                row++;
            }

            // Day-subtotal row (LightYellow, bold)
            ws.Range(row, 1, row, 3).Merge();
            ws.Cell(row, 1).Value = $"{dayGroup.Key:dd.MM.yyyy} — Kun jami";
            ws.Cell(row, 4).Value = $"{(int)dayTotal.TotalHours:D2}:{dayTotal.Minutes:D2}";
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightYellow;
            row++;
        }

        // Grand total row (Yellow, bold)
        ws.Range(row, 1, row, 3).Merge();
        ws.Cell(row, 1).Value = "Jami";
        ws.Cell(row, 4).Value = $"{(int)grandTotal.TotalHours:D2}:{grandTotal.Minutes:D2}";
        ws.Range(row, 1, row, 4).Style.Font.Bold = true;
        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.Yellow;

        ws.Columns().AdjustToContents();
        ws.Range(1, 1, row, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(1, 1, row, 4).Style.Border.InsideBorder = XLBorderStyleValues.Medium;
    }
}
