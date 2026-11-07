using System.ComponentModel.DataAnnotations;

namespace IndiaFinanceAI.Application.Leads;

public sealed record AdminLeadListItem(
    Guid Id,
    string FullName,
    string City,
    string State,
    string LoanType,
    decimal RequiredLoanAmount,
    DateTime CreatedAt,
    string LeadStatus,
    string LeadSource,
    DateOnly? FollowUpDate,
    int Score,
    string Priority);

public sealed record AdminLeadDetails(
    Guid Id,
    string FullName,
    string MobileNumber,
    string? Email,
    string City,
    string State,
    string LoanType,
    decimal RequiredLoanAmount,
    decimal MonthlyIncome,
    string EmploymentType,
    bool ConsentGiven,
    DateTime CreatedAt,
    bool CallbackRequested,
    string? PreferredContactTime,
    string LeadSource,
    string LeadStatus,
    DateOnly? FollowUpDate,
    string? FollowUpNotes,
    int Score,
    string Priority,
    IReadOnlyList<LeadScoreSignal> ScoreSignals,
    string AiSummary,
    bool WhatsAppConsentGiven,
    DateTime? WhatsAppConsentAt,
    DateTime? WhatsAppOptedOutAt,
    DateTime? LastWhatsAppActivityAt,
    string? LastWhatsAppStatus);

public sealed class UpdateLeadManagementRequest : IValidatableObject
{
    private static readonly string[] LeadStatuses = ["New", "Contacted", "Qualified", "Closed", "Disqualified"];

    [Required]
    public string LeadStatus { get; init; } = string.Empty;

    public DateOnly? FollowUpDate { get; init; }

    [StringLength(2000)]
    public string? FollowUpNotes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!LeadStatuses.Contains(LeadStatus, StringComparer.Ordinal))
        {
            yield return new ValidationResult("Select a supported lead status.", [nameof(LeadStatus)]);
        }
    }
}
