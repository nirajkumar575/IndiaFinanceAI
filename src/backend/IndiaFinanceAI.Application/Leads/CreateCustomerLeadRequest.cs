using System.ComponentModel.DataAnnotations;

namespace IndiaFinanceAI.Application.Leads;

public sealed class CreateCustomerLeadRequest : IValidatableObject
{
    private static readonly string[] LoanTypes =
    [
        "Personal Loan",
        "Business Loan",
        "MSME Loan",
        "Home Loan",
        "Vehicle Loan",
        "Loan Against Property",
    ];

    private static readonly string[] EmploymentTypes =
    [
        "Salaried",
        "Self-employed",
        "Business owner",
        "Other",
    ];

    [Required, StringLength(100, MinimumLength = 1)]
    public string FullName { get; init; } = string.Empty;

    [Required, RegularExpression("^[6-9][0-9]{9}$")]
    public string MobileNumber { get; init; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string State { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; init; } = string.Empty;

    [Required]
    public string LoanType { get; init; } = string.Empty;

    [Range(typeof(decimal), "10000", "50000000")]
    public decimal RequiredLoanAmount { get; init; }

    [Range(typeof(decimal), "1", "100000000")]
    public decimal MonthlyIncome { get; init; }

    [Required]
    public string EmploymentType { get; init; } = string.Empty;

    public bool ConsentGiven { get; init; }
    public bool WhatsAppConsentGiven { get; init; }
    public bool CallbackRequested { get; init; }

    [StringLength(20)]
    public string? PreferredContactTime { get; init; }

    [Required, StringLength(30)]
    public string LeadSource { get; init; } = "Website";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!LoanTypes.Contains(LoanType, StringComparer.Ordinal))
        {
            yield return new ValidationResult("Select a supported loan type.", [nameof(LoanType)]);
        }

        if (!EmploymentTypes.Contains(EmploymentType, StringComparer.Ordinal))
        {
            yield return new ValidationResult("Select a supported employment type.", [nameof(EmploymentType)]);
        }

        if (PreferredContactTime is not null && !new[] { "Morning", "Afternoon", "Evening" }.Contains(PreferredContactTime, StringComparer.Ordinal))
        {
            yield return new ValidationResult("Select a supported preferred contact time.", [nameof(PreferredContactTime)]);
        }

        if (LeadSource is not "Website" and not "AI Assistant")
        {
            yield return new ValidationResult("Select a supported lead source.", [nameof(LeadSource)]);
        }

        if (!ConsentGiven)
        {
            yield return new ValidationResult("Customer consent is required.", [nameof(ConsentGiven)]);
        }
    }
}
