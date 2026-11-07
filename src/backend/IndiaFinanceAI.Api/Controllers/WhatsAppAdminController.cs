using System.ComponentModel.DataAnnotations;
using IndiaFinanceAI.Application.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IndiaFinanceAI.Api.Controllers;

public sealed class SendWhatsAppTemplateRequest
{
    [Required, StringLength(100)]
    public string TemplateName { get; init; } = string.Empty;

    [Required, RegularExpression("^[a-z]{2}(?:_[A-Z]{2})?$")]
    public string LanguageCode { get; init; } = string.Empty;
}

[ApiController]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("whatsapp-admin")]
[Route("api/admin")]
public sealed class WhatsAppAdminController(IWhatsAppService whatsAppService) : ControllerBase
{
    [HttpGet("whatsapp/status")]
    public ActionResult<WhatsAppIntegrationStatus> GetStatus() => Ok(whatsAppService.GetIntegrationStatus());

    [HttpGet("leads/{leadId:guid}/whatsapp/events")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppCommunicationEventDto>>> GetEvents(
        Guid leadId,
        CancellationToken cancellationToken)
    {
        var events = await whatsAppService.GetEventsAsync(leadId, cancellationToken);
        return Ok(events);
    }

    [HttpPost("leads/{leadId:guid}/whatsapp/link")]
    public async Task<IActionResult> CreateManualLink(Guid leadId, CancellationToken cancellationToken)
    {
        var result = await whatsAppService.CreateManualLinkAsync(leadId, cancellationToken);
        if (result.Success) return Ok(result);
        return result.ErrorCode switch
        {
            "NotFound" => NotFound(),
            "WhatsAppConsentRequired" or "WhatsAppOptedOut" => Conflict(new { code = result.ErrorCode }),
            "InvalidIndianMobile" => BadRequest(new { code = result.ErrorCode }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { code = "WhatsAppLinkUnavailable" }),
        };
    }

    [HttpPost("leads/{leadId:guid}/whatsapp/template")]
    public async Task<IActionResult> SendApprovedTemplate(
        Guid leadId,
        [FromBody] SendWhatsAppTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await whatsAppService.SendApprovedTemplateAsync(leadId, request.TemplateName, request.LanguageCode, cancellationToken);
        if (result.Success) return Accepted(new { result.Status, result.MessageId });
        return result.ErrorCode switch
        {
            "NotFound" => NotFound(),
            "WhatsAppConsentRequired" or "WhatsAppOptedOut" => Conflict(new { code = result.ErrorCode }),
            "TemplateNotApproved" or "InvalidLanguageCode" or "InvalidIndianMobile" => BadRequest(new { code = result.ErrorCode }),
            "CloudApiDisabled" => StatusCode(StatusCodes.Status503ServiceUnavailable, new { code = result.ErrorCode }),
            _ => StatusCode(StatusCodes.Status502BadGateway, new { code = "WhatsAppProviderError", providerCode = result.ErrorCode }),
        };
    }

    [HttpPost("leads/{leadId:guid}/whatsapp/opt-out")]
    public async Task<IActionResult> RecordOptOut(Guid leadId, CancellationToken cancellationToken) =>
        await whatsAppService.RecordOptOutAsync(leadId, cancellationToken) ? NoContent() : NotFound();
}
