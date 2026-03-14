using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Employees;

public class IndexModel(
    IEmployeesService employeesService,
    ICompaniesService companiesService) : PageModel
{
    public IReadOnlyList<Company> Companies { get; private set; } = [];
    public IReadOnlyList<Employee> Employees { get; private set; } = [];
    public Guid SelectedCompanyId { get; private set; }

    public async Task OnGetAsync(Guid companyId)
    {
        Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
        SelectedCompanyId = companyId;
        if (companyId != Guid.Empty)
            Employees = (await employeesService.GetAllEmployeesOfCompanyAsync(companyId)).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid companyId)
    {
        var deleted = await employeesService.DeleteEmployeeAsync(id);
        if (deleted)
            TempData["Success"] = "Xodim o'chirildi";
        else
            TempData["Error"] = "O'chirishda xato yuz berdi";
        return RedirectToPage(new { companyId });
    }
}
