namespace IndiaFinanceAI.Infrastructure.WhatsApp;

public sealed class WhatsAppCloudOptions
{
    public bool Enabled { get; set; }
    public string? AccessToken { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? ApiVersion { get; set; }
    public string? AppSecret { get; set; }
    public string? WebhookVerifyToken { get; set; }
    public string[] ApprovedTemplates { get; set; } = [];

    public bool CloudApiEnabled => Enabled
        && !string.IsNullOrWhiteSpace(AccessToken)
        && !string.IsNullOrWhiteSpace(PhoneNumberId)
        && !string.IsNullOrWhiteSpace(ApiVersion)
        && !string.IsNullOrWhiteSpace(AppSecret)
        && !string.IsNullOrWhiteSpace(WebhookVerifyToken)
        && ApprovedTemplates.Length > 0;
}
