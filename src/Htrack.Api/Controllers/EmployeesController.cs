using ClosedXML.Excel;
using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Dtos.EmployeeDtos;
using HTrack.Api.Entities;
using HTrack.Api.Mappers.EmployeeMappers;
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

    [HttpPost("bulk-import")]
    public async ValueTask<IActionResult> BulkImportEmployees(
        [FromQuery] Guid companyId, IFormFile file, CancellationToken abortionToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Fayl tanlanmagan.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, abortionToken);
        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var employees = new List<Employee>();
        for (int row = 2; row <= lastRow; row++)
        {
            var name = ws.Cell(row, 1).GetValue<string>().Trim();
            var rfid = ws.Cell(row, 2).GetValue<string>().Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(rfid))
                continue;
            employees.Add(new Employee { Name = name, RFIDCardUID = rfid, CompanyId = companyId });
        }

        var (created, errors) = await employeesService.BulkAddAsync(employees, abortionToken);
        return Ok(new { created, errors });
    }

    [HttpGet("bulk-import-template")]
    public IActionResult GetBulkImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Xodimlar");
        ws.Cell(1, 1).Value = "Ism";
        ws.Cell(1, 2).Value = "RFID Card UID";
        ws.Cell(2, 1).Value = "Ali Valiyev";
        ws.Cell(2, 2).Value = "AABBCCDD";
        ws.Cell(3, 1).Value = "Vali Aliyev";
        ws.Cell(3, 2).Value = "11223344";
        ws.Range(1, 1, 1, 2).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xodimlar_shablon.xlsx");
    }
}