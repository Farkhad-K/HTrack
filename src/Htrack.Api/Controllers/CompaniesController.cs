using HTrack.Api.Mappers.CompanyMappers;
using HTrack.Api.Abstractions.ServicesAbstractions;
using Microsoft.AspNetCore.Mvc;
using HTrack.Api.Dtos.CompanyDtos;

namespace HTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController(
    ICompaniesService companiesService) : ControllerBase
{
    [HttpGet]
    public async ValueTask<IActionResult> GetAllCompanies(CancellationToken abortionToken = default)
    {
        var companies = await companiesService.GetAllCompaniesAsync(abortionToken);
        return Ok(companies.Select(c => c.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async ValueTask<IActionResult> GetCompanyById([FromRoute] Guid id, CancellationToken abortionToken = default)
    {
        var company = await companiesService.GetCompanyByIdAsync(id, abortionToken);
        return Ok(company.ToDto());
    }

    [HttpPost("create-company")]
    public async ValueTask<IActionResult> AddCompany([FromBody] CreateCompany dto, CancellationToken abortionToken = default)
    {
        var company = await companiesService.AddCompanyAsync(dto.ToEntity(), abortionToken);
        return Ok(company.ToDto());
    }

    [HttpPost("addManager/{companyId:guid}")]
    public async ValueTask<IActionResult> AddManagerAsync(
        [FromRoute] Guid companyId,
        [FromBody] long tgUserId,
        CancellationToken abortionToken = default)
    {
        await companiesService.AddManagersTgUserIdToCompanyAsync(companyId, tgUserId, abortionToken);
        return Ok();
    }

    [HttpPut("update-company/{companyId:guid}")]
    public async ValueTask<IActionResult> UpdateCompany([FromRoute] Guid companyId, [FromBody] UpdateCompany dto, CancellationToken abortionToken = default)
    {
        var company = await companiesService.UpdateCompanyAsync(companyId, dto.ToEntity(), abortionToken);
        return Ok(company.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async ValueTask<IActionResult> DeleteCompany([FromRoute] Guid id, CancellationToken abortionToken = default)
    {
        await companiesService.DeleteCompanyAsync(id, abortionToken);
        return NoContent();
    }
}