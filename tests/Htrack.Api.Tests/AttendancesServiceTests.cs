using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;
using HTrack.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Htrack.Api.Tests;

public class AttendancesServiceTests
{
    [Fact]
    public async Task HandleAttendanceWithResultAsync_IgnoresDuplicateScanWithinWindowAfterCheckIn()
    {
        var employee = BuildEmployee();
        var repository = new FakeAttendancesRepository(employee)
        {
            LastAttendance = new Attendance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Employee = employee,
                CheckIn = DateTime.UtcNow.AddSeconds(-5),
                CheckInSource = AttendanceEntrySource.Device
            }
        };
        var service = new AttendancesService(repository, NullLogger<AttendancesService>.Instance);

        var result = await service.HandleAttendanceWithResultAsync(
            employee.CompanyId!.Value,
            employee.RFIDCardUID!,
            AttendanceEntrySource.Device);

        Assert.Equal(HTrack.Api.Abstractions.ServicesAbstractions.AttendanceActionType.IgnoredDuplicate, result.ActionType);
        Assert.Equal(0, repository.CheckOutCalls);
    }

    [Fact]
    public async Task HandleAttendanceWithResultAsync_PreservesManualSourceOnManualCheckIn()
    {
        var employee = BuildEmployee();
        var repository = new FakeAttendancesRepository(employee);
        var service = new AttendancesService(repository, NullLogger<AttendancesService>.Instance);

        var result = await service.HandleAttendanceWithResultAsync(
            employee.CompanyId!.Value,
            employee.RFIDCardUID!,
            AttendanceEntrySource.Manual);

        Assert.Equal(HTrack.Api.Abstractions.ServicesAbstractions.AttendanceActionType.CheckedIn, result.ActionType);
        Assert.Equal(AttendanceEntrySource.Manual, result.Attendance.CheckInSource);
        Assert.Equal(AttendanceEntrySource.Manual, repository.LastCheckInSource);
    }

    private static Employee BuildEmployee()
    {
        var companyId = Guid.NewGuid();
        return new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Ali Valiyev",
            RFIDCardUID = "ABC123",
            CompanyId = companyId,
            Company = new Company
            {
                Id = companyId,
                Name = "Test Co",
                TgChatID = 1
            }
        };
    }

    private sealed class FakeAttendancesRepository(Employee employee) : IAttendancesRepository
    {
        public Attendance? LastAttendance { get; set; }
        public int CheckInCalls { get; private set; }
        public int CheckOutCalls { get; private set; }
        public AttendanceEntrySource? LastCheckInSource { get; private set; }
        public AttendanceEntrySource? LastCheckOutSource { get; private set; }

        public ValueTask<Employee?> GetEmployeeByRfidAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
        {
            if (employee.CompanyId == companyId && employee.RFIDCardUID == rfidCardUID)
                return ValueTask.FromResult<Employee?>(employee);

            throw new EmployeeWithUIDNotFoundException(rfidCardUID);
        }

        public ValueTask<Attendance?> GetLastAttendanceAsync(Guid employeeId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(LastAttendance);

        public ValueTask<IEnumerable<Attendance?>> GetLast30OfEmployeeAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<Attendance?>>([]);

        public ValueTask<IEnumerable<Attendance?>> GetAllCheckInAsync(Guid companyId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<Attendance?>>([]);

        public ValueTask<IEnumerable<Attendance?>> GetAllCheckOutAsync(Guid companyId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<Attendance?>>([]);

        public ValueTask<Attendance?> CheckInAsync(Guid employeeId, AttendanceEntrySource source = AttendanceEntrySource.Device, CancellationToken cancellationToken = default)
        {
            CheckInCalls++;
            LastCheckInSource = source;
            LastAttendance = new Attendance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Employee = employee,
                CheckIn = DateTime.UtcNow,
                CheckInSource = source
            };
            return ValueTask.FromResult<Attendance?>(LastAttendance);
        }

        public ValueTask<Attendance?> CheckOutAsync(Attendance attendance, AttendanceEntrySource source = AttendanceEntrySource.Device, CancellationToken cancellationToken = default)
        {
            CheckOutCalls++;
            LastCheckOutSource = source;
            attendance.CheckOut = DateTime.UtcNow;
            attendance.CheckOutSource = source;
            attendance.Duration = attendance.CheckOut.Value - attendance.CheckIn;
            LastAttendance = attendance;
            return ValueTask.FromResult<Attendance?>(attendance);
        }
    }
}
