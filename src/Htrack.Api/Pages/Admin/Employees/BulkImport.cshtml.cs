using ClosedXML.Excel;
using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Employees;

public class BulkImportModel(
    IEmployeesService employeesService,
    ICompaniesService companiesService) : PageModel
{
    public IReadOnlyList<Company> Companies { get; private set; } = [];
    public string? Error { get; private set; }
    public int? Created { get; private set; }
    public List<string> ImportErrors { get; private set; } = [];
    public Guid SelectedCompanyId { get; private set; }

    public async Task OnGetAsync(Guid companyId)
    {
        Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
        SelectedCompanyId = companyId;
    }

    public async Task<IActionResult> OnPostAsync(Guid companyId, IFormFile? file)
    {
        Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
        SelectedCompanyId = companyId;

        if (companyId == Guid.Empty)
        {
            Error = "Kompaniya tanlanmagan.";
            return Page();
        }

        if (file is null || file.Length == 0)
        {
            Error = "Fayl tanlanmagan.";
            return Page();
        }

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            var employees = new List<Employee>();
            for (int row = 2; row <= lastRow; row++)
            {
                var name = ws.Cell(row, 1).GetValue<string>().Trim();
                var rfid = ws.Cell(row, 2).GetValue<string>().Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(rfid))
                    continue;
                employees.Add(new Employee { Name = name, RFIDCardUID = rfid, CompanyId = companyId });
            }

            var (created, errors) = await employeesService.BulkAddAsync(employees);
            Created = created;
            ImportErrors = errors;

            if (created > 0)
                TempData["Success"] = $"{created} ta xodim muvaffaqiyatli qo'shildi.";
        }
        catch (Exception ex)
        {
            Error = $"Fayl o'qishda xatolik: {ex.Message}";
        }

        return Page();
    }
}
