using HTrack.Api.Dtos.EmployeeDtos;
using HTrack.Api.Mappers.EmployeeMappers;
using HTrack.Api.Abstractions.ServicesAbstractions;
using Microsoft.AspNetCore.Mvc;

namespace HTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(
    IEmployeesService employeesService) : ControllerBase
{
    [HttpGet("get-all-employees/{companyId:guid}")]
    public async ValueTask<IActionResult> GetAllEmployees([FromRoute] Guid companyId, CancellationToken abortionToken = default)
    {
        var employees = await employeesService.GetAllEmployeesOfCompanyAsync(companyId,abortionToken);
        return Ok(employees.Select(e => e.ToDto()));
    }

    [HttpGet("get-employee-by-id/{id:guid}")]
    public async ValueTask<IActionResult> GetEmployeeById([FromRoute] Guid id, CancellationToken abortionToken = default)
    {
        var employee = await employeesService.GetEmployeeByIdAsync(id, abortionToken);
        return Ok(employee.ToDto());
    }

    [HttpGet("get-employee-by-rfidUid/{companyId:guid}/{rfidUid}")]
    public async ValueTask<IActionResult> GetEmployeeByRfidUid([FromRoute] Guid companyId, [FromRoute] string rfidUid, CancellationToken abortionToken = default)
    {
        var employee = await employeesService.GetEmployeeByRfidAsync(companyId, rfidUid, abortionToken);
        return Ok(employee.ToDto());
    }

    [HttpPost("create-employee")]
    public async ValueTask<IActionResult> AddEmployee([FromBody] CreateEmployee dto, CancellationToken abortionToken = default)
    {
        var employee = await employeesService.AddEmployeeAsync(dto.ToEntity(), abortionToken);
        return Ok(employee.ToDto());
    }

    [HttpDelete("delete-employee/{id:guid}")]
    public async ValueTask<IActionResult> DeleteEmployee([FromRoute] Guid id, CancellationToken abortionToken = default)
    {
        await employeesService.DeleteEmployeeAsync(id, abortionToken);
        return NoContent();
    }

    [HttpPut("update-employee/{companyId:guid}/{rfidUid}")] 
    public async ValueTask<IActionResult> UpdateEmployee([FromRoute] Guid companyId, [FromRoute] string rfidUid, [FromBody] UpdateEmployee dto, CancellationToken abortionToken = default)
    {
        var employee = await employeesService.UpdateEmployeeAsync(companyId, rfidUid, dto.ToEntity(), abortionToken);
        return Ok(employee.ToDto());
    }
}