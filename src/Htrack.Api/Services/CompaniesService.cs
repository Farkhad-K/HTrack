using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;

namespace HTrack.Api.Services;

public class CompaniesService(
    ICompaniesRepository companiesRepository,
    ILogger<CompaniesService> logger) : ICompaniesService
{
    public async ValueTask<Company> AddCompanyAsync(Company company, CancellationToken cancellationToken = default)
        => await companiesRepository.AddAsync(company, cancellationToken);

    public ValueTask<bool> AddManagersTgUserIdToCompanyAsync(Guid companyId, long tgUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            return companiesRepository.AddManagerAsync(companyId, tgUserId, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add manager to company with id {Id}", companyId);
            throw new CompanyNotFoundException(companyId);
        }
    }

    public async ValueTask<bool> DeleteCompanyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await companiesRepository.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            logger.LogWarning("Failed to delete academy with id {Id}", id);
            throw new CompanyNotFoundException(id);
        }

        return true;
    }

    public async ValueTask<IEnumerable<Company>> GetAllCompaniesAsync(CancellationToken cancellationToken = default)
        => await companiesRepository.GetAllAsync(cancellationToken);

    public ValueTask<Company> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => companiesRepository.GetByIdAsync(id, cancellationToken);

    public ValueTask<Company> UpdateCompanyAsync(Guid companyId, Company company, CancellationToken cancellationToken = default)
    {
        try
        {
            return companiesRepository.UpdateAsync(companyId, company, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update company with id {Id}", company.Id);
            throw new CompanyNotFoundException(companyId);
        }
    }
}
