using HTrack.Api.Abstractions;
using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Data;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Repositories;

public class AttendancesRepository(
    IHTrackDbContext context,
    IAttendanceNotifier notifier) : IAttendancesRepository
{
    public async ValueTask<Employee?> GetEmployeeByRfidAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
        => await context.Employees.Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.RFIDCardUID == rfidCardUID, cancellationToken)
            ?? throw new EmployeeWithUIDNotFoundException(rfidCardUID);

    public async ValueTask<Attendance?> GetLastAttendanceAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => await context.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .Include(a => a.Employee)
            .OrderByDescending(a => a.CheckIn)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<Attendance?> CheckInAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CheckIn = DateTime.UtcNow
        };

        var entry = context.Attendances.Add(attendance);
        await context.SaveChangesAsync(cancellationToken);

        await notifier.NotifyAttendanceAsync(attendance.Employee!, attendance, isCheckIn: true, cancellationToken);

        return entry.Entity;
    }

    public async ValueTask<Attendance?> CheckOutAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        attendance.CheckOut = DateTime.UtcNow;
        attendance.Duration = attendance.CheckOut.Value - attendance.CheckIn;

        context.Attendances.Update(attendance);
        await context.SaveChangesAsync(cancellationToken);

        await notifier.NotifyAttendanceAsync(attendance.Employee!, attendance, isCheckIn: false, cancellationToken);

        return attendance;
    }

    public async ValueTask<IEnumerable<Attendance?>> GetLast30OfEmployeeAsync(Guid companyId, string rfidCardUID, CancellationToken cancellationToken = default)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new CompanyNotFoundException(companyId);
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.CompanyId == company.Id && e.RFIDCardUID == rfidCardUID, cancellationToken)
            ?? throw new EmployeeWithUIDNotFoundException(rfidCardUID);

        return await context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employee.Id)
            .OrderByDescending(a => a.CheckIn) // use the timestamp field here
            .Take(30)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<IEnumerable<Attendance?>> GetAllCheckInAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new CompanyNotFoundException(companyId);

        return await context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.CheckOut == null && a.Employee!.CompanyId == company.Id)
            // .OrderByDescending(a => a.CheckIn) // Ensure latest check-ins come first
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<IEnumerable<Attendance?>> GetAllCheckOutAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
        ?? throw new CompanyNotFoundException(companyId);

        var today = DateTime.UtcNow.Date; // Use DateTime.Now.Date

        return await context.Attendances
            .Include(a => a.Employee)
            .Where(a =>
                a.CheckOut != null &&
                a.CheckOut.Value.Date == today &&
                a.Employee!.CompanyId == company.Id)
            .OrderByDescending(a => a.CheckOut)
            .ToListAsync(cancellationToken);
    }
}
