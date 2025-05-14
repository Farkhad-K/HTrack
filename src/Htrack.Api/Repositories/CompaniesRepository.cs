using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Data;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Repositories;

public class CompaniesRepository(
    IHTrackDbContext context,
    ILogger<CompaniesRepository> logger) : ICompaniesRepository
{
    public async ValueTask<Company> AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        var entry = context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async ValueTask<bool> AddManagerAsync(Guid companyId, long tgUserId, CancellationToken cancellationToken = default)
    {
        var existingCompany = await context.Companies
            .FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken)
            ?? throw new CompanyNotFoundException(companyId);

        if (!existingCompany.ManagerTgUserIDs.Contains(tgUserId))
            existingCompany.ManagerTgUserIDs.Add(tgUserId);

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            context.Companies.Remove(new Company { Id = id });
            return await context.SaveChangesAsync(cancellationToken) is 1;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete company with id {Id}", id);
            throw new CompanyNotFoundException(id);
        }
    }

    public async ValueTask<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Companies.AsNoTracking().Include(c => c.Employees).ToListAsync(cancellationToken);

    public async ValueTask<Company> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Companies.Include(c => c.Employees).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
           ?? throw new CompanyNotFoundException(id);

    public async ValueTask<Company> UpdateAsync(Guid companyId, Company company, CancellationToken cancellationToken = default)
    {
        var existingCompany = await context.Companies.FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken)
            ?? throw new CompanyNotFoundException(company.Id);

        existingCompany.Name = company.Name;
        if (company.TgChatID != 0)
            existingCompany.TgChatID = company.TgChatID;

        var entry = context.Companies.Update(existingCompany);
        await context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }


}