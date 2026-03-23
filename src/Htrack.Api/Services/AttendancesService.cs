using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;
using Microsoft.Extensions.Logging;

namespace HTrack.Api.Services;

public class AttendancesService(
    IAttendancesRepository attendancesRepository,
    ILogger<AttendancesService> logger) : IAttendancesService
{
    private static readonly TimeSpan DuplicateScanWindow = TimeSpan.FromSeconds(10);

    public async ValueTask<Attendance?> HandleAttendanceAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
        => (await HandleAttendanceWithResultAsync(companyId, rfidCardUID, AttendanceEntrySource.Device, cancellationToken)).Attendance;

    public async ValueTask<Attendance?> HandleAttendanceAsync(Guid companyId, string rfidCardUID, AttendanceEntrySource source, CancellationToken cancellationToken = default)
        => (await HandleAttendanceWithResultAsync(companyId, rfidCardUID, source, cancellationToken)).Attendance;

    public async ValueTask<AttendanceActionResult> HandleAttendanceWithResultAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
        => await HandleAttendanceWithResultAsync(companyId, rfidCardUID, AttendanceEntrySource.Device, cancellationToken);

    public async ValueTask<AttendanceActionResult> HandleAttendanceWithResultAsync(Guid companyId, string rfidCardUID, AttendanceEntrySource source, CancellationToken cancellationToken = default)
    {
        var employee = await attendancesRepository.GetEmployeeByRfidAsync(companyId, rfidCardUID, cancellationToken);
        var lastAttendance = await attendancesRepository.GetLastAttendanceAsync(employee!.Id, cancellationToken);
        var nowUtc = DateTime.UtcNow;

        if (lastAttendance == null || lastAttendance.CheckOut != null)
        {
            if (lastAttendance?.CheckOut is DateTime lastCheckOut
                && nowUtc - lastCheckOut < DuplicateScanWindow)
            {
                logger.LogWarning(
                    "Duplicate scan ignored for employee {EmployeeId} within {WindowSeconds}s after check-out",
                    employee.Id,
                    DuplicateScanWindow.TotalSeconds);
                return new AttendanceActionResult(
                    lastAttendance,
                    employee,
                    AttendanceActionType.IgnoredDuplicate,
                    source,
                    $"Takroriy scan {DuplicateScanWindow.TotalSeconds:0} soniya ichida e'tiborga olinmadi.");
            }

            logger.LogInformation("Check-in for employee {EmployeeId}", employee.Id);
            var attendance = await attendancesRepository.CheckInAsync(employee.Id, source, cancellationToken);
            return new AttendanceActionResult(attendance!, employee, AttendanceActionType.CheckedIn, source);
        }
        else
        {
            if (nowUtc - lastAttendance.CheckIn < DuplicateScanWindow)
            {
                logger.LogWarning(
                    "Duplicate scan ignored for employee {EmployeeId} within {WindowSeconds}s after check-in",
                    employee.Id,
                    DuplicateScanWindow.TotalSeconds);
                return new AttendanceActionResult(
                    lastAttendance,
                    employee,
                    AttendanceActionType.IgnoredDuplicate,
                    source,
                    $"Takroriy scan {DuplicateScanWindow.TotalSeconds:0} soniya ichida e'tiborga olinmadi.");
            }

            logger.LogInformation("Check-out for employee {EmployeeId}", employee.Id);
            var attendance = await attendancesRepository.CheckOutAsync(lastAttendance, source, cancellationToken);
            return new AttendanceActionResult(attendance!, employee, AttendanceActionType.CheckedOut, source);
        }
    }

    public async ValueTask<Attendance?> GetLastAttendanceAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => await attendancesRepository.GetLastAttendanceAsync(employeeId, cancellationToken);

    public async ValueTask<IEnumerable<Attendance?>> GetLast30AttendanceOfEmployee(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
    {
        try
        {
            return await attendancesRepository.GetLast30OfEmployeeAsync(companyId, rfidCardUID, cancellationToken);
        }
        catch (EmployeeWithUIDNotFoundException e)
        {
            logger.LogError(e, "Employee with uid {rfidCardUID} not found", rfidCardUID);
            throw new EmployeeWithUIDNotFoundException(rfidCardUID);
        }
        catch (CompanyNotFoundException e)
        {
            logger.LogError(e, "Company with id {Id} not found", companyId);
            throw new CompanyNotFoundException(companyId);
        }
    }

    public ValueTask<IEnumerable<Attendance?>> GetCheckedInEmployees(Guid companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return attendancesRepository.GetAllCheckInAsync(companyId, cancellationToken);
        }
        catch (CompanyNotFoundException e)
        {
            logger.LogError(e, "Company with id {Id} not found", companyId);
            throw new CompanyNotFoundException(companyId);
        }
        catch(Exception e)
        {
            logger.LogError(e, "Something went wrong while attempting to get checked in employees for company with id {Id}", companyId);
            throw new Exception();
        }
    }

    public ValueTask<IEnumerable<Attendance?>> GetCheckedOutEmployees(Guid companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return attendancesRepository.GetAllCheckOutAsync(companyId, cancellationToken);
        }
        catch (CompanyNotFoundException e)
        {
            logger.LogError(e, "Company with id {Id} not found", companyId);
            throw new CompanyNotFoundException(companyId);
        }
        catch(Exception e)
        {
            logger.LogError(e, "Something went wrong while attempting to get checked out employees for company with id {Id}", companyId);
            throw new Exception();
        }
    }
}
