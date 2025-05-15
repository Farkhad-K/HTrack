using System.Globalization;
using ClosedXML.Excel;
using HTrack.Api.Data;
using HTrack.Api.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Services;

public class ExcelReportService(IHTrackDbContext context, IWebHostEnvironment env) : IExcelReportService
{
    private readonly CultureInfo _uzCulture = new("uz-UZ");

    public async Task Generate15DayAttendanceReportsAsync(CancellationToken cancellationToken = default)
    {
        var companies = await context.Companies.Include(c => c.Employees).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var startDay = now.Day <= 15 ? 1 : 16;
        var endDay = now.Day <= 15 ? 15 : DateTime.DaysInMonth(now.Year, now.Month);
        var periodName = startDay == 1 ? "1dan15" : "16dan31";

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

            var fileName = $"{company.Name}_{now.ToString("MMMM", _uzCulture)}_{now.Year}_davomat_{periodName}.xlsx";
            var filePath = Path.Combine(env.ContentRootPath, "Reports", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Davomat");

            // Sarlavhalar
            worksheet.Cell(1, 1).Value = "Ishchi";
            worksheet.Cell(1, 2).Value = "Kelgan vaqti";
            worksheet.Cell(1, 3).Value = "Ketgan vaqti";
            worksheet.Cell(1, 4).Value = "Ishlagan soati";
            worksheet.Row(1).Style.Font.Bold = true;

            int row = 2;
            var grouped = attendances.GroupBy(a => a.Employee!.Name);

            foreach (var group in grouped)
            {
                var employeeName = group.Key!;
                TimeSpan totalDuration = TimeSpan.Zero;
                bool isFirstRow = true;

                foreach (var a in group)
                {
                    var checkIn = TimeHelper.ToUzbekistanTime(a.CheckIn);
                    var checkOut = a.CheckOut.HasValue ? TimeHelper.ToUzbekistanTime(a.CheckOut.Value) : (DateTime?)null;

                    worksheet.Cell(row, 1).Value = isFirstRow ? $"{employeeName} - {a.Employee!.RFIDCardUID}" : "";
                    worksheet.Cell(row, 2).Value = checkIn.ToString("d-MMMM yyyy HH:mm", _uzCulture);
                    worksheet.Cell(row, 3).Value = checkOut?.ToString("d-MMMM yyyy HH:mm", _uzCulture) ?? "Yo'q";
                    worksheet.Cell(row, 4).Value = a.Duration.ToString(@"hh\:mm");

                    if (isFirstRow)
                        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;

                    totalDuration += a.Duration;
                    isFirstRow = false;
                    row++;
                }

                // Jami
                worksheet.Cell(row, 3).Value = "Jami";
                worksheet.Cell(row, 4).Value = $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}";
                worksheet.Row(row).Style.Font.Bold = true;
                worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;
                worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Yellow;

                row += 2;
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
        var reportDate = DateTime.UtcNow.AddMonths(-1);
        var monthName = reportDate.ToString("MMMM", _uzCulture);
        var year = reportDate.Year;

        foreach (var company in companies)
        {
            var attendances = await context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Employee!.CompanyId == company.Id &&
                            a.CheckIn.Month == reportDate.Month &&
                            a.CheckIn.Year == reportDate.Year)
                .ToListAsync(cancellationToken);

            if (!attendances.Any()) continue;

            attendances = attendances
                .DistinctBy(a => new { a.EmployeeId, a.CheckIn, a.CheckOut })
                .OrderBy(a => a.Employee!.Name)
                .ThenBy(a => a.CheckIn)
                .ToList();

            var filePath = Path.Combine(env.ContentRootPath, "Reports", $"{company.Name}_{monthName}_{year}_davomat.xlsx");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Davomat");

            worksheet.Cell(1, 1).Value = "Ishchi";
            worksheet.Cell(1, 2).Value = "Kelgan vaqti";
            worksheet.Cell(1, 3).Value = "Ketgan vaqti";
            worksheet.Cell(1, 4).Value = "Ishlagan soati";
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
                    var checkIn = TimeHelper.ToUzbekistanTime(a.CheckIn);
                    var checkOut = a.CheckOut.HasValue ? TimeHelper.ToUzbekistanTime(a.CheckOut.Value) : (DateTime?)null;

                    worksheet.Cell(row, 1).Value = isFirstRow ? $"{employeeName} - {a.Employee!.RFIDCardUID}" : "";
                    worksheet.Cell(row, 2).Value = checkIn.ToString("d-MMMM yyyy HH:mm", _uzCulture);
                    worksheet.Cell(row, 3).Value = checkOut?.ToString("d-MMMM yyyy HH:mm", _uzCulture) ?? "Yo'q";
                    worksheet.Cell(row, 4).Value = a.Duration.ToString(@"hh\:mm");

                    if (isFirstRow)
                        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;

                    totalDuration += a.Duration;
                    isFirstRow = false;
                    row++;
                }

                worksheet.Cell(row, 3).Value = "Jami";
                worksheet.Cell(row, 4).Value = $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}";
                worksheet.Row(row).Style.Font.Bold = true;
                worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;
                worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Yellow;

                row += 2;
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
        var periodName = now.Day <= 15 ? "1dan15" : "16dan31";
        var fileName = $"{company.Name}_{now.ToString("MMMM", _uzCulture)}_{now.Year}_davomat_{periodName}.xlsx";
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
        var fileName = $"{company.Name}_{lastMonth.ToString("MMMM", _uzCulture)}_{lastMonth.Year}_davomat.xlsx";
        var filePath = Path.Combine(env.ContentRootPath, "Reports", fileName);

        if (!File.Exists(filePath)) return null;

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = Path.GetFileName(filePath)
        };
    }
}
