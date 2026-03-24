using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions.ServicesAbstractions;

public interface IEmployeesService
{
    ValueTask<Employee> AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
    ValueTask<Employee> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Employee>> GetAllEmployeesOfCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<Employee> GetEmployeeByRfidAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default);
    ValueTask<Employee> UpdateEmployeeAsync(Guid companyId, string rfidCardUID, Employee update, CancellationToken cancellationToken = default);
    ValueTask<Employee> UpdateEmployeeRfidAsync(Guid companyId, string currentRfidCardUID, string newRfidCardUID, CancellationToken cancellationToken = default);
    ValueTask<(int Created, List<string> Errors)> BulkAddAsync(IEnumerable<Employee> employees, CancellationToken cancellationToken = default);
}
