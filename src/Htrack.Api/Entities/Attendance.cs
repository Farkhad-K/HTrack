using HTrack.Api.Entities;

namespace HTrack.Api.Entities;

public class Attendance
{
    public Guid Id { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public TimeSpan Duration { get; set; }
    
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}