using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions.ServicesAbstractions;

public interface ICompaniesService
{
    ValueTask<Company> AddCompanyAsync(Company company, CancellationToken cancellationToken = default);
    ValueTask<bool> AddManagersTgUserIdToCompanyAsync(Guid companyId, long tgUserId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Company>> GetAllCompaniesAsync(CancellationToken cancellationToken = default);
    ValueTask<Company> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<Company> UpdateCompanyAsync(Guid companyId, Company company, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteCompanyAsync(Guid id, CancellationToken cancellationToken = default);
}