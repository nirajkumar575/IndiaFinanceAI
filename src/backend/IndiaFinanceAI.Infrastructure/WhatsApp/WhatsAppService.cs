using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IndiaFinanceAI.Application.WhatsApp;
using IndiaFinanceAI.Domain;
using IndiaFinanceAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IndiaFinanceAI.Infrastructure.WhatsApp;

public sealed class WhatsAppService(
    FinanceDbContext dbContext,
    HttpClient httpClient,
    IOptions<WhatsAppCloudOptions> options) : IWhatsAppService
{
    private readonly WhatsAppCloudOptions _options = options.Value;

    public WhatsAppIntegrationStatus GetIntegrationStatus() => new(
        _options.CloudApiEnabled,
        _options.CloudApiEnabled,
        _options.CloudApiEnabled ? _options.ApprovedTemplates : []);

    public async Task<WhatsAppLinkResult> CreateManualLinkAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await dbContext.CustomerLeads.FirstOrDefaultAsync(item => item.Id == leadId, cancellationToken);
        if (lead is null) return Failure("NotFound");
        if (!lead.WhatsAppConsentGiven) return Failure("WhatsAppConsentRequired");
        if (lead.WhatsAppOptedOutAt is not null) return Failure("WhatsAppOptedOut");

        var link = WhatsAppLinkBuilder.CreateManualChatLink(lead.MobileNumber, lead.FullName, lead.LoanType);
        if (link is null) return Failure("InvalidIndianMobile");

        var createdAt = DateTime.UtcNow;
        lead.LastWhatsAppActivityAt = createdAt;
        lead.LastWhatsAppStatus = "LinkReadyNotSent";
        dbContext.WhatsAppCommunicationEvents.Add(new WhatsAppCommunicationEvent
        {
            CustomerLeadId = lead.Id,
            EventType = "ManualLinkPrepared",
            Status = "LinkReadyNotSent",
            CreatedAt = createdAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new WhatsAppLinkResult(true, link, "LinkReadyNotSent", null, createdAt);
    }

    public async Task<WhatsAppSendResult> SendApprovedTemplateAsync(
        Guid leadId,
        string templateName,
        string languageCode,
        CancellationToken cancellationToken)
    {
        if (!_options.CloudApiEnabled) return SendFailure("CloudApiDisabled");
        if (!_options.ApprovedTemplates.Contains(templateName, StringComparer.Ordinal)) return SendFailure("TemplateNotApproved");
        if (!System.Text.RegularExpressions.Regex.IsMatch(languageCode, "^[a-z]{2}(?:_[A-Z]{2})?$")) return SendFailure("InvalidLanguageCode");

        var lead = await dbContext.CustomerLeads.FirstOrDefaultAsync(item => item.Id == leadId, cancellationToken);
        if (lead is null) return SendFailure("NotFound");
        if (!lead.WhatsAppConsentGiven) return SendFailure("WhatsAppConsentRequired");
        if (lead.WhatsAppOptedOutAt is not null) return SendFailure("WhatsAppOptedOut");

        var number = WhatsAppLinkBuilder.NormalizeIndianMobile(lead.MobileNumber);
        if (number is null) return SendFailure("InvalidIndianMobile");

        var communicationEvent = new WhatsAppCommunicationEvent
        {
            CustomerLeadId = lead.Id,
            EventType = "CloudTemplate",
            Status = "Pending",
            TemplateName = templateName,
            CreatedAt = DateTime.UtcNow,
        };
        lead.LastWhatsAppActivityAt = communicationEvent.CreatedAt;
        lead.LastWhatsAppStatus = "Pending";
        dbContext.WhatsAppCommunicationEvents.Add(communicationEvent);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            request.Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                to = number,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = languageCode },
                },
            });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                communicationEvent.Status = "Failed";
                communicationEvent.ErrorCode = ExtractProviderErrorCode(responseBody) ?? $"HTTP_{(int)response.StatusCode}";
                lead.LastWhatsAppStatus = "Failed";
                await dbContext.SaveChangesAsync(cancellationToken);
                return new WhatsAppSendResult(false, "Failed", null, communicationEvent.ErrorCode);
            }

            communicationEvent.MessageId = ExtractMessageId(responseBody);
            communicationEvent.Status = "Accepted";
            lead.LastWhatsAppStatus = "Accepted";
            await dbContext.SaveChangesAsync(cancellationToken);
            return new WhatsAppSendResult(true, "Accepted", communicationEvent.MessageId, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            communicationEvent.Status = "Failed";
            communicationEvent.ErrorCode = "Timeout";
            lead.LastWhatsAppStatus = "Failed";
            await dbContext.SaveChangesAsync(cancellationToken);
            return new WhatsAppSendResult(false, "Failed", null, "Timeout");
        }
        catch (HttpRequestException)
        {
            communicationEvent.Status = "Failed";
            communicationEvent.ErrorCode = "NetworkError";
            lead.LastWhatsAppStatus = "Failed";
            await dbContext.SaveChangesAsync(cancellationToken);
            return new WhatsAppSendResult(false, "Failed", null, "NetworkError");
        }
    }

    public async Task<bool> RecordOptOutAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await dbContext.CustomerLeads.FirstOrDefaultAsync(item => item.Id == leadId, cancellationToken);
        if (lead is null) return false;

        var now = DateTime.UtcNow;
        lead.WhatsAppOptedOutAt = now;
        lead.LastWhatsAppActivityAt = now;
        lead.LastWhatsAppStatus = "OptedOut";
        dbContext.WhatsAppCommunicationEvents.Add(new WhatsAppCommunicationEvent
        {
            CustomerLeadId = lead.Id,
            EventType = "AdminRecordedOptOut",
            Status = "OptedOut",
            CreatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<WhatsAppCommunicationEventDto>> GetEventsAsync(Guid leadId, CancellationToken cancellationToken) =>
        await dbContext.WhatsAppCommunicationEvents.AsNoTracking()
            .Where(item => item.CustomerLeadId == leadId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(20)
            .Select(item => new WhatsAppCommunicationEventDto(
                item.Id,
                item.EventType,
                item.Status,
                item.TemplateName,
                item.ErrorCode,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task ProcessWebhookAsync(string rawBody, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(rawBody);
        if (!document.RootElement.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array) return;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array) continue;
            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;
                await ProcessDeliveryStatusesAsync(value, cancellationToken);
                await ProcessStopMessagesAsync(value, cancellationToken);
            }
        }
    }

    private async Task ProcessDeliveryStatusesAsync(JsonElement value, CancellationToken cancellationToken)
    {
        if (!value.TryGetProperty("statuses", out var statuses) || statuses.ValueKind != JsonValueKind.Array) return;
        foreach (var status in statuses.EnumerateArray())
        {
            var messageId = ReadString(status, "id");
            var deliveryStatus = ReadString(status, "status");
            if (string.IsNullOrWhiteSpace(messageId) || deliveryStatus is not ("sent" or "delivered" or "read" or "failed")) continue;

            var communicationEvent = await dbContext.WhatsAppCommunicationEvents
                .Include(item => item.CustomerLead)
                .FirstOrDefaultAsync(item => item.MessageId == messageId, cancellationToken);
            if (communicationEvent is null) continue;

            communicationEvent.Status = char.ToUpperInvariant(deliveryStatus[0]) + deliveryStatus[1..];
            if (deliveryStatus == "failed" && status.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            {
                communicationEvent.ErrorCode = ReadString(errors[0], "code");
            }
            communicationEvent.CustomerLead.LastWhatsAppActivityAt = DateTime.UtcNow;
            communicationEvent.CustomerLead.LastWhatsAppStatus = communicationEvent.Status;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessStopMessagesAsync(JsonElement value, CancellationToken cancellationToken)
    {
        if (!value.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array) return;
        foreach (var message in messages.EnumerateArray())
        {
            if (!message.TryGetProperty("text", out var textElement)) continue;
            var text = ReadString(textElement, "body");
            if (text is null || !IsOptOutCommand(text.Trim())) continue;

            var normalizedNumber = WhatsAppLinkBuilder.NormalizeIndianMobile(ReadString(message, "from"));
            if (normalizedNumber is null) continue;
            var localNumber = normalizedNumber[^10..];
            var lead = await dbContext.CustomerLeads.FirstOrDefaultAsync(item => item.MobileNumber == localNumber, cancellationToken);
            if (lead is null || lead.WhatsAppOptedOutAt is not null) continue;

            var now = DateTime.UtcNow;
            lead.WhatsAppOptedOutAt = now;
            lead.LastWhatsAppActivityAt = now;
            lead.LastWhatsAppStatus = "OptedOut";
            dbContext.WhatsAppCommunicationEvents.Add(new WhatsAppCommunicationEvent
            {
                CustomerLeadId = lead.Id,
                EventType = "CustomerOptOut",
                Status = "OptedOut",
                CreatedAt = now,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static WhatsAppLinkResult Failure(string errorCode) => new(false, null, "NotSent", errorCode, null);
    private static WhatsAppSendResult SendFailure(string errorCode) => new(false, "NotSent", null, errorCode);

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.ToString() : null;

    private static bool IsOptOutCommand(string text) =>
        string.Equals(text, "STOP", StringComparison.OrdinalIgnoreCase)
        || string.Equals(text, "UNSUBSCRIBE", StringComparison.OrdinalIgnoreCase)
        || string.Equals(text, "CANCEL", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractMessageId(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.TryGetProperty("messages", out var messages)
                && messages.ValueKind == JsonValueKind.Array
                && messages.GetArrayLength() > 0
                ? ReadString(messages[0], "id")
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractProviderErrorCode(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.TryGetProperty("error", out var error)
                ? ReadString(error, "code")
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
