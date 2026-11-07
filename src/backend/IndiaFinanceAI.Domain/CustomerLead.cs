namespace IndiaFinanceAI.Domain;

public sealed class CustomerLead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string LoanType { get; set; } = string.Empty;
    public decimal RequiredLoanAmount { get; set; }
    public decimal MonthlyIncome { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public bool ConsentGiven { get; set; }
    public bool CallbackRequested { get; set; }
    public string? PreferredContactTime { get; set; }
    public string LeadSource { get; set; } = "Website";
    public DateTime CreatedAt { get; set; }
    public string LeadStatus { get; set; } = "New";
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNotes { get; set; }
    public bool WhatsAppConsentGiven { get; set; }
    public DateTime? WhatsAppConsentAt { get; set; }
    public DateTime? WhatsAppOptedOutAt { get; set; }
    public DateTime? LastWhatsAppActivityAt { get; set; }
    public string? LastWhatsAppStatus { get; set; }
}
