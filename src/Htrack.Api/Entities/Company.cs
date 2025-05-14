namespace HTrack.Api.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public long TgChatID { get; set; }
    public List<long> ManagerTgUserIDs { get; set; } = [];
    
    public ICollection<Employee> Employees { get; set; } = [];
}