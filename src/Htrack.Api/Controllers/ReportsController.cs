using HTrack.Api.Abstractions.ServicesAbstractions;
using Microsoft.AspNetCore.Mvc;

namespace HTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController(IExcelReportService excelReportService) : ControllerBase
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("download")]
    public async Task<IActionResult> DownloadLastMonthReport([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await excelReportService.GetLastMonthReportAsync(companyId, ct);
        return File(result.Stream, XlsxMime, result.FileName);
    }

    [HttpGet("download-for-15")]
    public async Task<IActionResult> Download15DayReport([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await excelReportService.Get15DayReportAsync(companyId, ct);
        return File(result.Stream, XlsxMime, result.FileName);
    }

    [HttpGet("download-till-today")]
    public async Task<IActionResult> DownloadTillTodayReport([FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await excelReportService.GetFromStartToTodayAsync(companyId, ct);
        return File(result.Stream, XlsxMime, result.FileName);
    }

    [HttpGet("custom")]
    public async Task<IActionResult> DownloadCustomReport(
        [FromQuery] Guid companyId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        var result = await excelReportService.GetCustomRangeReportAsync(companyId, from, to, ct);
        return File(result.Stream, XlsxMime, result.FileName);
    }
}
