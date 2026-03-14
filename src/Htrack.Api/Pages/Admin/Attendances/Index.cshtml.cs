using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HTrack.Api.Pages.Admin.Attendances;

public class IndexModel(
    IAttendancesService attendancesService,
    ICompaniesService companiesService) : PageModel
{
    public IReadOnlyList<Company> Companies { get; private set; } = [];
    public IReadOnlyList<Attendance?> CheckedIn { get; private set; } = [];
    public IReadOnlyList<Attendance?> CheckedOut { get; private set; } = [];
    public Guid SelectedCompanyId { get; private set; }

    public async Task OnGetAsync(Guid companyId)
    {
        Companies = (await companiesService.GetAllCompaniesAsync()).ToList();
        SelectedCompanyId = companyId;
        if (companyId != Guid.Empty)
        {
            CheckedIn = (await attendancesService.GetCheckedInEmployees(companyId)).ToList();
            CheckedOut = (await attendancesService.GetCheckedOutEmployees(companyId)).ToList();
        }
    }
}
