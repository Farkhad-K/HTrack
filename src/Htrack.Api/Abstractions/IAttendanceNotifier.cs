using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions;

public interface IAttendanceNotifier
{
    Task NotifyAttendanceAsync(Employee employee, Attendance attendance, bool isCheckIn, CancellationToken cancellationToken = default);
}
