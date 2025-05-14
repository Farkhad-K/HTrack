using HTrack.Api.Dtos.CompanyDtos;
using HTrack.Api.Entities;

namespace HTrack.Api.Mappers.CompanyMappers;

public static class EntityToDtoMappers
{
    public static Company ToEntity(this UpdateCompany dto)
        => new()
        {
            Name = dto.Name,
            TgChatID = dto.TgChatID
        };
    
    public static Company ToEntity(this Dtos.CompanyDtos.CreateCompany dto)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            TgChatID = dto.TgChatID,
            ManagerTgUserIDs = dto.ManagerTgUserIDs
        };

    public static CompanyDto ToDto(this Company entity)
        => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            TgChatID = entity.TgChatID,
            ManagerTgUserIDs = entity.ManagerTgUserIDs,
            Employees = entity.Employees.Select(e => new EmployeeOfCompany
            {
                employeeId = e.Id,
                Name = e.Name,
                RFIDCardUID = e.RFIDCardUID
            }).ToList()
        };
}