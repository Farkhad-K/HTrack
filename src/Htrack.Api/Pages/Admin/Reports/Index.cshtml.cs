using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Reports;

public class IndexModel(
    IExcelReportService reportService,
    ICompaniesService companiesService) : PageModel
{
    public IReadOnlyList<Company> Companies { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostDownloadAsync(
        string type, Guid companyId, string? from, string? to)
    {
        if (companyId == Guid.Empty)
        {
            Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
            Error = "Kompaniya tanlanmadi";
            return Page();
        }

        try
        {
            (MemoryStream stream, string fileName) = type switch
            {
                "last15" => await reportService.Get15DayReportAsync(companyId),
                "monthToDate" => await reportService.GetFromStartToTodayAsync(companyId),
                "custom" when !string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to)
                    => await reportService.GetCustomRangeReportAsync(
                        companyId,
                        DateOnly.Parse(from),
                        DateOnly.Parse(to)),
                _ => await reportService.GetLastMonthReportAsync(companyId)
            };

            stream.Position = 0;
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
            Error = ex.Message;
            return Page();
        }
    }
}
