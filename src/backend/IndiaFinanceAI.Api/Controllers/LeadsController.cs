using IndiaFinanceAI.Application.Leads;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace IndiaFinanceAI.Api.Controllers;

[ApiController]
[Route("api/leads")]
public sealed class LeadsController(ICustomerLeadService customerLeadService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("public-leads")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerLeadRequest request,
        CancellationToken cancellationToken)
    {
        var id = await customerLeadService.CreateAsync(request, cancellationToken);
        return Created($"/api/leads/{id}", new { id, status = "New" });
    }
}
