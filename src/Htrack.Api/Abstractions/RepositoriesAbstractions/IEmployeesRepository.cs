using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions.RepositoriesAbstractions;

public interface IEmployeesRepository
{
    ValueTask<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default);
    ValueTask<Employee> GetByIdByAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<Employee> GetByRfidAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default);
    ValueTask<Employee> UpdateAsync(Guid companyId, string rfidCardUID, Employee update, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Employee>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}