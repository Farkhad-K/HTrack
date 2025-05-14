namespace HTrack.Api.Dtos.AttendanceDtos;

public class CreateAttendance
{
    public Guid CompanyId { get; set; }
    public string? RFIDCardUID { get; set; }
}