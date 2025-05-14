using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;

namespace HTrack.Api.Services;

public class EmployeesService(
    IEmployeesRepository employeesRepository,
    ILogger<EmployeesService> logger): IEmployeesService
{
    public async ValueTask<Employee> AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
        => await employeesRepository.AddAsync(employee, cancellationToken); 

    public async ValueTask<bool> DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await employeesRepository.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            logger.LogWarning("Failed to delete employee with id {Id}", id);
            throw new EmployeeNotFoundException(id);
        }

        return true;
    }

    public async ValueTask<IEnumerable<Employee>> GetAllEmployeesOfCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        => await employeesRepository.GetAllAsync(companyId, cancellationToken);
    public async ValueTask<Employee> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await employeesRepository.GetByIdByAsync(id, cancellationToken);
    public async ValueTask<Employee> GetEmployeeByRfidAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
        => await employeesRepository.GetByRfidAsync(companyId, rfidCardUID, cancellationToken);

    public ValueTask<Employee> UpdateEmployeeAsync(Guid companyId, string rfidCardUID, Employee update, CancellationToken cancellationToken = default)
    {
        try
        {
            return employeesRepository.UpdateAsync(companyId, rfidCardUID, update, cancellationToken);
        }
        catch (CompanyNotFoundException e)
        {
            logger.LogError(e, "Company with id {Id} not found", companyId);
            throw new CompanyNotFoundException(companyId);
        }
        catch (EmployeeWithUIDNotFoundException e)
        {
            logger.LogError(e, "Failed to update employee with uid {rfidCardUID}", rfidCardUID);
            throw new EmployeeWithUIDNotFoundException(rfidCardUID);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Something went wrong while attempting to update employee with id {Id}", update.Id);
            throw new Exception();
        }
    }
}
