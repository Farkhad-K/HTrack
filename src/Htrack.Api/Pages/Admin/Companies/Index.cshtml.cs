using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Companies;

public class IndexModel(ICompaniesService companiesService) : PageModel
{
    public IReadOnlyList<Company> Companies { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var deleted = await companiesService.DeleteCompanyAsync(id);
        if (deleted)
            TempData["Success"] = "Kompaniya o'chirildi";
        else
            TempData["Error"] = "O'chirishda xato yuz berdi";
        return RedirectToPage();
    }
}
