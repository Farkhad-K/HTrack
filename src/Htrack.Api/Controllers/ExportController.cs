using HTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly ExcelExportService _excelExportService;

    public ExportController(ExcelExportService excelExportService)
    {
        _excelExportService = excelExportService;
    }

    // Endpoint to generate and save the Excel file, and return the file path
    [HttpPost("generate")]
    public IActionResult GenerateReport()
    {
        // Generate and save the Excel file
        var filePath = _excelExportService.GenerateSampleExcel();

        // Return a success response with the file path
        return Ok(new { filePath });
    }

    // Endpoint to download the generated report
    [HttpGet("download/{fileName}")]
    public IActionResult DownloadReport(string fileName)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Reports", fileName);

        // Check if the file exists
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found.");
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(fileBytes, contentType, fileName);
    }
}
