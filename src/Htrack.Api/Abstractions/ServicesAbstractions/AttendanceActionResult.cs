using HTrack.Api.Entities;

namespace HTrack.Api.Abstractions.ServicesAbstractions;

public sealed record AttendanceActionResult(
    Attendance Attendance,
    Employee Employee,
    AttendanceActionType ActionType,
    AttendanceEntrySource Source,
    string? Message = null);
