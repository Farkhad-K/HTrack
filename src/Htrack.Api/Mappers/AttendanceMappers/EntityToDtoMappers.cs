using HTrack.Api.Dtos.AttendanceDtos;
using HTrack.Api.Entities;

namespace HTrack.Api.Mappers.AttendanceMappers;

public static class EntityToDtoMappers
{
    public static AttendanceDto ToDto(this Attendance entity)
    => new()
    {
        Id = entity.Id,
        CkeckIn = entity.CheckIn,
        CheckOut = entity.CheckOut,
        Duration = entity.Duration,
        EmployeeId = entity.EmployeeId,
        EmployeeName = entity?.Employee?.Name,
        RFIDCardUID = entity?.Employee?.RFIDCardUID
    };
}