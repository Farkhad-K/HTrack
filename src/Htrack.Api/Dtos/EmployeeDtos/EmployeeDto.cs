namespace HTrack.Api.Dtos.EmployeeDtos;

public class EmployeeDtos
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? RFIDCardUID { get; set; }

    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
}