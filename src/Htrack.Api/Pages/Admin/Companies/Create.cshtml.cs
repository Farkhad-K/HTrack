using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Companies;

public class CreateModel(ICompaniesService companiesService) : PageModel
{
    public string? Error { get; private set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync(string name, long tgChatId, string? managerIds)
    {
        var company = new Company
        {
            Name = name,
            TgChatID = tgChatId,
            ManagerTgUserIDs = CompanyFormHelper.ParseManagerIds(managerIds)
        };

        try
        {
            await companiesService.AddCompanyAsync(company);
            TempData["Success"] = $"'{name}' kompaniyasi qo'shildi";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}
