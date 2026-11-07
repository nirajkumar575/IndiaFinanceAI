using IndiaFinanceAI.Application.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndiaFinanceAI.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/leads")]
public sealed class AdminLeadsController(ICustomerLeadService customerLeadService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminLeadListItem>>> Search(
        [FromQuery] string? loanType,
        [FromQuery] string? leadStatus,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (loanType is { Length: > 40 } || leadStatus is { Length: > 20 } || search is { Length: > 100 })
        {
            return BadRequest(new { message = "Filter values exceed the allowed length." });
        }

        var leads = await customerLeadService.SearchAsync(loanType, leadStatus, search, cancellationToken);
        return Ok(leads);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminLeadDetails>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lead = await customerLeadService.GetByIdAsync(id, cancellationToken);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPut("{id:guid}/management")]
    public async Task<IActionResult> UpdateManagement(
        Guid id,
        [FromBody] UpdateLeadManagementRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await customerLeadService.UpdateManagementAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
