using HTrack.Api.Dtos.EmployeeDtos;
using HTrack.Api.Entities;

namespace HTrack.Api.Mappers.EmployeeMappers;

public static class EntityToDtoMappers
{
    public static EmployeeDtos ToDto(this Employee entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        RFIDCardUID = entity.RFIDCardUID,
        CompanyId = entity.CompanyId,
        CompanyName = entity.Company?.Name
    };

    public static Employee ToEntity(this CreateEmployee dto) 
    => new()
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        RFIDCardUID = dto.RFIDCardUID,
        CompanyId = dto.CompanyId
    };
    public static Employee ToEntity(this UpdateEmployee dto) 
    => new()
    {
        Name = dto.Name
    };
}