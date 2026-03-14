using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Companies;

public class EditModel(ICompaniesService companiesService) : PageModel
{
    public Guid CompanyId { get; private set; }
    public string? Name { get; private set; }
    public long TgChatID { get; private set; }
    public string ManagerIdsText { get; private set; } = string.Empty;
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var company = await companiesService.GetCompanyByIdAsync(id);
            CompanyId = company.Id;
            Name = company.Name;
            TgChatID = company.TgChatID;
            ManagerIdsText = string.Join(", ", company.ManagerTgUserIDs);
            return Page();
        }
        catch
        {
            return RedirectToPage("Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string name, long tgChatId, string? managerIds)
    {
        var update = new Company
        {
            Name = name,
            TgChatID = tgChatId,
            ManagerTgUserIDs = CompanyFormHelper.ParseManagerIds(managerIds)
        };

        try
        {
            await companiesService.UpdateCompanyAsync(id, update);
            TempData["Success"] = $"'{name}' kompaniyasi yangilandi";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            CompanyId = id;
            Name = name;
            TgChatID = tgChatId;
            ManagerIdsText = managerIds ?? string.Empty;
            Error = ex.Message;
            return Page();
        }
    }
}
