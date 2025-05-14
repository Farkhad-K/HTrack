using ClosedXML.Excel;
using System;
using System.IO;

namespace HTrack.Api.Services;

public class ExcelExportService
{
    private readonly string _reportsFolder;

    public ExcelExportService()
    {
        // Set the path to the "Reports" folder in the root directory
        _reportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Reports");

        // Ensure the "Reports" directory exists
        if (!Directory.Exists(_reportsFolder))
        {
            Directory.CreateDirectory(_reportsFolder);
        }
    }

    public string GenerateSampleExcel()
    {
        // Create a unique file name based on the current date/time
        var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var filePath = Path.Combine(_reportsFolder, fileName);

        // Create a new Excel workbook and worksheet
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sample Report");

        // Add some data to the worksheet
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Email";
        worksheet.Cell(2, 1).Value = "Alice";
        worksheet.Cell(2, 2).Value = "alice@example.com";
        worksheet.Cell(3, 1).Value = "Bob";
        worksheet.Cell(3, 2).Value = "bob@example.com";

        // Save the workbook to the file path
        workbook.SaveAs(filePath);

        return filePath;
    }
}
