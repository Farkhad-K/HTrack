using ClosedXML.Excel;
using HTrack.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Services;

public class ExcelReportService(IHTrackDbContext context, IWebHostEnvironment env) : IExcelReportService
{
    public async Task Generate15DayAttendanceReportsAsync(CancellationToken cancellationToken = default)
    {
        var companies = await context.Companies.Include(c => c.Employees).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var startDay = now.Day <= 15 ? 1 : 16;
        var endDay = now.Day <= 15 ? 15 : DateTime.DaysInMonth(now.Year, now.Month);
        var periodName = startDay == 1 ? "1to15" : "16toEnd";

        foreach (var company in companies)
        {
            var attendances = await context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Employee!.CompanyId == company.Id &&
                            a.CheckIn.Month == now.Month &&
                            a.CheckIn.Year == now.Year &&
                            a.CheckIn.Day >= startDay && a.CheckIn.Day <= endDay)
                .ToListAsync(cancellationToken);

            if (!attendances.Any()) continue;

            attendances = attendances
                .DistinctBy(a => new { a.EmployeeId, a.CheckIn, a.CheckOut })
                .OrderBy(a => a.Employee!.Name)
                .ThenBy(a => a.CheckIn)
                .ToList();

            var fileName = $"{company.Name}_{now:MMMM}_{now.Year}_attendance_{periodName}.xlsx";
            var filePath = Path.Combine(env.ContentRootPath, "Reports", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Employee";
            worksheet.Cell(1, 2).Value = "Check In";
            worksheet.Cell(1, 3).Value = "Check Out";
            worksheet.Cell(1, 4).Value = "Duration";
            worksheet.Row(1).Style.Font.Bold = true;

            int row = 2;
            var grouped = attendances.GroupBy(a => a.Employee!.Name);

            foreach (var group in grouped)
            {
                string employeeName = group.Key!;
                TimeSpan totalDuration = TimeSpan.Zero;
                bool isFirstRow = true;

                foreach (var a in group)
                {
                    worksheet.Cell(row, 1).Value = isFirstRow ? employeeName + " - " + a.Employee!.RFIDCardUID : "";
                    worksheet.Cell(row, 2).Value = a.CheckIn.ToString("d-MMMM h:mm tt");
                    worksheet.Cell(row, 3).Value = a.CheckOut?.ToString("d-MMMM h:mm tt") ?? "N/A";
                    worksheet.Cell(row, 4).Value = a.Duration.ToString(@"hh\:mm");

                    if (isFirstRow)
                    {
                        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }

                    totalDuration += a.Duration;
                    isFirstRow = false;
                    row++;
                }

                worksheet.Cell(row, 3).Value = "Total";
                worksheet.Cell(row, 4).Value = $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}";
                worksheet.Row(row).Style.Font.Bold = true;

                worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;
                worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Yellow;

                row += 2;
                // double row++; changed to row += 2
            }

            worksheet.Columns().AdjustToContents();
            var usedRange = worksheet.RangeUsed();
            usedRange!.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Medium;

            workbook.SaveAs(filePath);
        }
    }

    public async Task GenerateMonthlyAttendanceReportsAsync(CancellationToken cancellationToken = default)
    {
        var companies = await context.Companies.Include(c => c.Employees).ToListAsync(cancellationToken);
        var reportMonth = DateTime.UtcNow.AddMonths(-1);
        var monthName = reportMonth.ToString("MMMM");
        var year = reportMonth.Year;

        foreach (var company in companies)
        {
            var attendances = await context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Employee!.CompanyId == company.Id &&
                            a.CheckIn.Month == reportMonth.Month &&
                            a.CheckIn.Year == reportMonth.Year)
                .ToListAsync(cancellationToken);

            if (!attendances.Any()) continue;

            // Remove duplicates and sort
            attendances = attendances
                .DistinctBy(a => new { a.EmployeeId, a.CheckIn, a.CheckOut })
                .OrderBy(a => a.Employee!.Name)
                .ThenBy(a => a.CheckIn)
                .ToList();

            var filePath = Path.Combine(env.ContentRootPath, "Reports", $"{company.Name}_{monthName}_{year}_attendance.xlsx");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance");

            // Header row
            worksheet.Cell(1, 1).Value = "Employee";
            worksheet.Cell(1, 2).Value = "Check In";
            worksheet.Cell(1, 3).Value = "Check Out";
            worksheet.Cell(1, 4).Value = "Duration";
            worksheet.Row(1).Style.Font.Bold = true;

            int row = 2;
            var grouped = attendances.GroupBy(a => a.Employee!.Name);

            foreach (var group in grouped)
            {
                string employeeName = group.Key!;
                TimeSpan totalDuration = TimeSpan.Zero;
                bool isFirstRow = true;

                foreach (var a in group)
                {
                    worksheet.Cell(row, 1).Value = isFirstRow ? employeeName + " - " + a.Employee!.RFIDCardUID : ""; // show name only once
                    worksheet.Cell(row, 2).Value = a.CheckIn.ToString("d-MMMM h:mm tt");
                    worksheet.Cell(row, 3).Value = a.CheckOut?.ToString("d-MMMM h:mm tt") ?? "N/A";
                    worksheet.Cell(row, 4).Value = a.Duration.ToString(@"hh\:mm");

                    if (isFirstRow)
                    {
                        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }

                    totalDuration += a.Duration;
                    isFirstRow = false;
                    row++;
                }

                // Total row
                worksheet.Cell(row, 3).Value = "Total";
                worksheet.Cell(row, 4).Value = $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}";
                worksheet.Row(row).Style.Font.Bold = true;

                worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;
                worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Yellow;

                row += 2;
                // double row++; changed to row += 2
            }

            worksheet.Columns().AdjustToContents();

            var usedRange = worksheet.RangeUsed();
            usedRange!.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Medium;

            workbook.SaveAs(filePath);
        }
    }

    public async Task<FileStreamResult?> Get15DayReportAsync(Guid companyId)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null) return null;

        var now = DateTime.UtcNow;
        var periodName = now.Day <= 15 ? "1to15" : "16toEnd";
        var fileName = $"{company.Name}_{now:MMMM}_{now.Year}_attendance_{periodName}.xlsx";
        var filePath = Path.Combine(env.ContentRootPath, "Reports", fileName);

        if (!File.Exists(filePath)) return null;

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = Path.GetFileName(filePath)
        };
    }

    public async Task<FileStreamResult?> GetLastMonthReportAsync(Guid companyId)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null) return null;

        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var filePath = Path.Combine(env.ContentRootPath, "Reports", $"{company.Name}_{lastMonth:MMMM}_{lastMonth.Year}_attendance.xlsx");

        if (!File.Exists(filePath)) return null;

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = Path.GetFileName(filePath)
        };
    }
}
