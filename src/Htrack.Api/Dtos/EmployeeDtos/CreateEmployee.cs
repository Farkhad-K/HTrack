namespace HTrack.Api.Dtos.EmployeeDtos;

public class CreateEmployee
{
    public string? Name { get; set; }
    public string? RFIDCardUID { get; set; }
    public Guid CompanyId { get; set; }
}