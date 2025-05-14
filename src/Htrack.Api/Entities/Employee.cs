using HTrack.Api.Entities;

namespace HTrack.Api.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? RFIDCardUID { get; set; }

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}