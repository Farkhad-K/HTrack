using HTrack.Api.Data;
using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions.RepositoriesAbstractions;

public interface ICompaniesRepository
{
    ValueTask<Company> AddAsync(Company company, CancellationToken cancellationToken = default);
    ValueTask<Company> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default);
    ValueTask<Company> UpdateAsync(Guid companyId, Company company, CancellationToken cancellationToken = default);
    ValueTask<bool> AddManagerAsync(Guid companyId, long tgUserId, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}