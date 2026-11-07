namespace IndiaFinanceAI.Application.WhatsApp;

public sealed record WhatsAppIntegrationStatus(
    bool CloudApiEnabled,
    bool WebhookEnabled,
    IReadOnlyList<string> ApprovedTemplates);

public sealed record WhatsAppLinkResult(
    bool Success,
    string? ChatLink,
    string Status,
    string? ErrorCode,
    DateTime? ActivityAt);

public sealed record WhatsAppSendResult(
    bool Success,
    string Status,
    string? MessageId,
    string? ErrorCode);

public sealed record WhatsAppCommunicationEventDto(
    Guid Id,
    string EventType,
    string Status,
    string? TemplateName,
    string? ErrorCode,
    DateTime CreatedAt);

public interface IWhatsAppService
{
    WhatsAppIntegrationStatus GetIntegrationStatus();
    Task<WhatsAppLinkResult> CreateManualLinkAsync(Guid leadId, CancellationToken cancellationToken);
    Task<WhatsAppSendResult> SendApprovedTemplateAsync(Guid leadId, string templateName, string languageCode, CancellationToken cancellationToken);
    Task<bool> RecordOptOutAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WhatsAppCommunicationEventDto>> GetEventsAsync(Guid leadId, CancellationToken cancellationToken);
    Task ProcessWebhookAsync(string rawBody, CancellationToken cancellationToken);
}
