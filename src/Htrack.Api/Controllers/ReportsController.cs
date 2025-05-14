using HTrack.Api.Abstractions.ServicesAbstractions;
using Microsoft.AspNetCore.Mvc;
using HTrack.Api.Services;

namespace HTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController(IExcelReportService excelReportService) : ControllerBase
{
    [HttpGet("generate")]
    public async Task<IActionResult> GenerateMonthlyReport(CancellationToken abortionToken)
    {
        await excelReportService.GenerateMonthlyAttendanceReportsAsync(abortionToken);
        return Ok("Monthly reports generated successfully.");
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadLastMonthReport([FromQuery] Guid companyId, CancellationToken abortionToken)
    {
        var result = await excelReportService.GetLastMonthReportAsync(companyId);
        return result is not null ? result : NotFound("Report not found.");
    }

    [HttpGet("generate-for-15")]
    public async Task<IActionResult> Generate15DayReport(CancellationToken ct)
    {
        await excelReportService.Generate15DayAttendanceReportsAsync(ct);
        return Ok("15-day reports generated.");
    }

    [HttpGet("download-for-15")]
    public async Task<IActionResult> Download15DayReport([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await excelReportService.Get15DayReportAsync(companyId);
        return result is not null ? result : NotFound("15-day report not found.");
    }
}
