namespace HTrack.Api.Dtos.AttendanceDtos;

public class AttendanceDto
{
    public Guid Id { get; set; }
    public DateTime CkeckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public TimeSpan Duration { get; set; }

    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? RFIDCardUID { get; set; }
}