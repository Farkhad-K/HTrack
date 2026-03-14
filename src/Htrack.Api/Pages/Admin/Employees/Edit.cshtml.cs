using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Employees;

public class EditModel(IEmployeesService employeesService) : PageModel
{
    public Guid CompanyId { get; private set; }
    public string? Rfid { get; private set; }
    public string? Name { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid companyId, string rfid)
    {
        try
        {
            var employee = await employeesService.GetEmployeeByRfidAsync(companyId, rfid);
            CompanyId = companyId;
            Rfid = rfid;
            Name = employee.Name;
            return Page();
        }
        catch
        {
            return RedirectToPage("Index", new { companyId });
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid companyId, string rfid, string name)
    {
        var update = new Employee { Name = name };
        try
        {
            await employeesService.UpdateEmployeeAsync(companyId, rfid, update);
            TempData["Success"] = $"'{name}' xodimi yangilandi";
            return RedirectToPage("Index", new { companyId });
        }
        catch (Exception ex)
        {
            CompanyId = companyId;
            Rfid = rfid;
            Name = name;
            Error = ex.Message;
            return Page();
        }
    }
}
