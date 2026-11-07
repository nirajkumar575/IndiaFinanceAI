using IndiaFinanceAI.Application.Leads;
using IndiaFinanceAI.Domain;

namespace IndiaFinanceAI.Tests;

public sealed class LeadSummaryTests
{
    [Fact]
    public void SummaryOnlyIncludesAvailableLeadFacts()
    {
        var summary = LeadSummaryGenerator.Generate(new CustomerLead
        {
            LoanType = "Personal Loan",
        });

        Assert.Equal("Personal Loan enquiry.", summary);
        Assert.DoesNotContain("income", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("amount", summary, StringComparison.OrdinalIgnoreCase);
    }
}
