namespace HTrack.Api.Dtos.CompanyDtos;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public long TgChatID { get; set; }
    public List<long> ManagerTgUserIDs { get; set; } = [];

    public List<EmployeeOfCompany> Employees { get; set; } = [];
}

public class EmployeeOfCompany
{
    public Guid employeeId { get; set; }
    public string? Name { get; set; }
    public string? RFIDCardUID { get; set; }
}