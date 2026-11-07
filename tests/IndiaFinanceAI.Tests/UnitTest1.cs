using System.ComponentModel.DataAnnotations;
using IndiaFinanceAI.Application.Leads;

namespace IndiaFinanceAI.Tests;

public class CustomerLeadRequestTests
{
    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = ValidRequest();
        var results = Validate(request);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(false, "Personal Loan", "9876543210")]
    [InlineData(true, "Unknown Loan", "9876543210")]
    [InlineData(true, "Personal Loan", "123")]
    public void InvalidConsentLoanTypeOrMobile_FailsValidation(bool consent, string loanType, string mobile)
    {
        Assert.NotEmpty(Validate(ValidRequest(consent, loanType, mobile)));
    }

    [Theory]
    [InlineData("Unknown", null)]
    [InlineData("Website", "Night")]
    public void UnsupportedSourceOrPreferredTime_FailsValidation(string source, string? preferredTime)
    {
        Assert.NotEmpty(Validate(ValidRequest(source: source, preferredTime: preferredTime)));
    }

    private static List<ValidationResult> Validate(CreateCustomerLeadRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static CreateCustomerLeadRequest ValidRequest(
        bool consent = true,
        string loanType = "Personal Loan",
        string mobile = "9876543210",
        string source = "Website",
        string? preferredTime = null) => new()
    {
        FullName = "Fictional Customer",
        MobileNumber = mobile,
        Email = "test@example.invalid",
        State = "Maharashtra",
        City = "Pune",
        LoanType = loanType,
        RequiredLoanAmount = 250000,
        MonthlyIncome = 50000,
        EmploymentType = "Salaried",
        ConsentGiven = consent,
        LeadSource = source,
        PreferredContactTime = preferredTime,
    };
}

public sealed class UpdateLeadManagementRequestTests
{
    [Fact]
    public void UnsupportedStatus_FailsValidation()
    {
        var request = new UpdateLeadManagementRequest { LeadStatus = "Root" };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.NotEmpty(results);
    }
}
