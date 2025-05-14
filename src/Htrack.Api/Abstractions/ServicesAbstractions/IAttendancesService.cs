using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions.ServicesAbstractions;

public interface IAttendancesService
{
    ValueTask<Attendance?> HandleAttendanceAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default);
    ValueTask<Attendance?> GetLastAttendanceAsync(Guid employeeId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Attendance?>> GetLast30AttendanceOfEmployee(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Attendance?>> GetCheckedInEmployees(Guid companyId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Attendance?>> GetCheckedOutEmployees(Guid companyId, CancellationToken cancellationToken = default);
}
