namespace IndiaFinanceAI.Domain;

public sealed class WhatsAppCommunicationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerLeadId { get; set; }
    public CustomerLead CustomerLead { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? TemplateName { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
