namespace HTrack.Api.Dtos.CompanyDtos;

public class CreateCompany
{
    public string? Name { get; set; }
    public long TgChatID { get; set; }
    public List<long> ManagerTgUserIDs { get; set; } = [];
}