using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Data;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;
using HTrack.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Repositories;

public class EmployeesRepository(
    IHTrackDbContext context,
    ILogger<CompaniesRepository> logger) : IEmployeesRepository
{
    public async ValueTask<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        var entry = context.Employees.Add(employee);
        await context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            context.Employees.Remove(new Employee { Id = id });
            return await context.SaveChangesAsync(cancellationToken) is 1;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete employee with id {Id}", id);
            throw new EmployeeNotFoundException(id);
        }
    }

    public async ValueTask<IEnumerable<Employee>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default)
        => await context.Employees.AsNoTracking().Include(e => e.Company).Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);

    public async ValueTask<Employee> GetByIdByAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Employees.Include(e => e.Company).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
           ?? throw new EmployeeNotFoundException(id);

    public async ValueTask<Employee> GetByRfidAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
        => await context.Employees.Include(e => e.Company).FirstOrDefaultAsync(e => e.CompanyId == companyId && e.RFIDCardUID == rfidCardUID, cancellationToken)
           ?? throw new EmployeeWithUIDNotFoundException(rfidCardUID);

    public async ValueTask<Employee> UpdateAsync(Guid companyId, string rfidCardUID, Employee update, CancellationToken cancellationToken = default)
    {
        var companyExists = await context.Companies.AnyAsync(c => c.Id == companyId, cancellationToken);
        if (!companyExists)
            throw new CompanyNotFoundException(companyId);

        var existingEmployee = await context.Employees
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.RFIDCardUID == rfidCardUID, cancellationToken)
            ?? throw new EmployeeWithUIDNotFoundException(rfidCardUID);

        existingEmployee.Name = update.Name;

        context.Employees.Update(existingEmployee);
        await context.SaveChangesAsync(cancellationToken);

        return existingEmployee;
    }
}