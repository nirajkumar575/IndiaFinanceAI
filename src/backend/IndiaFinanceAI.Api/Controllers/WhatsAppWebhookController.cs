using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IndiaFinanceAI.Application.WhatsApp;
using IndiaFinanceAI.Infrastructure.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace IndiaFinanceAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("whatsapp-webhook")]
[Route("api/webhooks/whatsapp")]
public sealed class WhatsAppWebhookController(
    IOptions<WhatsAppCloudOptions> options,
    IWhatsAppService whatsAppService) : ControllerBase
{
    private const int MaximumBodyBytes = 65536;

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var config = options.Value;
        if (!config.CloudApiEnabled) return NotFound();
        if (mode != "subscribe" || !FixedTimeEquals(verifyToken, config.WebhookVerifyToken)) return Unauthorized();
        return Content(challenge ?? string.Empty, "text/plain");
    }

    [HttpPost]
    [RequestSizeLimit(MaximumBodyBytes)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.CloudApiEnabled) return NotFound();
        if (Request.ContentLength is > MaximumBodyBytes) return StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (Encoding.UTF8.GetByteCount(body) > MaximumBodyBytes) return StatusCode(StatusCodes.Status413PayloadTooLarge);

        var suppliedSignature = Request.Headers["X-Hub-Signature-256"].ToString();
        if (!VerifySignature(body, suppliedSignature, config.AppSecret!)) return Unauthorized();

        try
        {
            await whatsAppService.ProcessWebhookAsync(body, cancellationToken);
        }
        catch (JsonException)
        {
            return BadRequest(new { code = "InvalidWebhookPayload" });
        }

        return Ok();
    }

    private static bool VerifySignature(string body, string suppliedSignature, string appSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var digest = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return FixedTimeEquals(suppliedSignature, $"sha256={digest}");
    }

    private static bool FixedTimeEquals(string? provided, string? expected)
    {
        if (provided is null || expected is null) return false;
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
