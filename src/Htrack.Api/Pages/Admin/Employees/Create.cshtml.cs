using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Employees;

public class CreateModel(
    IEmployeesService employeesService,
    ICompaniesService companiesService) : PageModel
{
    public IReadOnlyList<Company> Companies { get; private set; } = [];
    public string? Error { get; private set; }
    public string? SelectedCompanyIdStr { get; private set; }
    public string? NameValue { get; private set; }
    public string? RfidValue { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid companyId)
    {
        Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
        SelectedCompanyIdStr = companyId == Guid.Empty ? null : companyId.ToString();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid companyId, string name, string rfid)
    {
        var employee = new Employee
        {
            Name = name,
            RFIDCardUID = rfid,
            CompanyId = companyId
        };

        try
        {
            await employeesService.AddEmployeeAsync(employee);
            TempData["Success"] = $"'{name}' xodimi qo'shildi";
            return RedirectToPage("Index", new { companyId });
        }
        catch (Exception ex)
        {
            Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
            SelectedCompanyIdStr = companyId.ToString();
            NameValue = name;
            RfidValue = rfid;
            Error = ex.Message;
            return Page();
        }
    }
}
