using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions.RepositoriesAbstractions;

public interface IAttendancesRepository
{
    ValueTask<Attendance?> GetLastAttendanceAsync(Guid employeeId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Attendance?>> GetLast30OfEmployeeAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Attendance?>> GetAllCheckInAsync(Guid companyId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Attendance?>> GetAllCheckOutAsync(Guid companyId, CancellationToken cancellationToken = default);
    ValueTask<Attendance?> CheckInAsync(Guid employeeId, AttendanceEntrySource source = AttendanceEntrySource.Device, CancellationToken cancellationToken = default);
    ValueTask<Attendance?> CheckOutAsync(Attendance attendance, AttendanceEntrySource source = AttendanceEntrySource.Device, CancellationToken cancellationToken = default);
    ValueTask<Employee?> GetEmployeeByRfidAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default);
}
