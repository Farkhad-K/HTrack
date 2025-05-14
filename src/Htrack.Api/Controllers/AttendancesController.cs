using HTrack.Api.Mappers.AttendanceMappers;
using HTrack.Api.Abstractions.ServicesAbstractions;
using Microsoft.AspNetCore.Mvc;

namespace HTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendancesController(
    IAttendancesService attendancesService) : ControllerBase
{
    [HttpPost("create-attendance/{companyId:guid}/{rfidCardUID}")]
    public async ValueTask<IActionResult> AddAttendanceAsync([FromRoute] Guid companyId, [FromRoute] string rfidCardUID, CancellationToken abortionToken = default)
    {
        var attendance = await attendancesService.HandleAttendanceAsync(companyId, rfidCardUID, abortionToken);
        return Ok(attendance!.ToDto());
    }

    [HttpGet("get-last-attendance/{employeeId:guid}")]
    public async ValueTask<IActionResult> GetLastAttendanceAsync([FromRoute] Guid employeeId, CancellationToken abortionToken = default)
    {
        var attendance = await attendancesService.GetLastAttendanceAsync(employeeId, abortionToken);
        return Ok(attendance!.ToDto());
    }

    [HttpGet("get-last-30attendances/{companyId:guid}/{rfidCardUID}")]
    public async ValueTask<IActionResult> GetLastAttendanceAsync([FromRoute] Guid companyId, [FromRoute] string rfidCardUID, CancellationToken abortionToken = default)
    {
        var attendance = await attendancesService.GetLast30AttendanceOfEmployee(companyId, rfidCardUID, abortionToken);
        return Ok(attendance!.Select(a => a!.ToDto()));
    }

    [HttpGet("get-checked-in-employees/{companyId:guid}/")]
    public async ValueTask<IActionResult> GetCheckedInEmployees([FromRoute] Guid companyId, CancellationToken abortionToken = default)
    {
        var attendance = await attendancesService.GetCheckedInEmployees(companyId, abortionToken);
        return Ok(attendance!.Select(a => a!.ToDto()));
    }

    [HttpGet("get-checked-out-employees/{companyId:guid}/")]
    public async ValueTask<IActionResult> GetCheckedOutEmployees([FromRoute] Guid companyId, CancellationToken abortionToken = default)
    {
        var attendance = await attendancesService.GetCheckedInEmployees(companyId, abortionToken);
        return Ok(attendance!.Select(a => a!.ToDto()));
    }
}