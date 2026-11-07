using IndiaFinanceAI.Application.Leads;
using IndiaFinanceAI.Domain;

namespace IndiaFinanceAI.Tests;

public sealed class LeadScoringTests
{
    [Fact]
    public void AllSalesSignals_AwardOneHundredAndHighPriority()
    {
        var result = LeadScoringService.Calculate(CompleteLead());

        Assert.Equal(100, result.Score);
        Assert.Equal("High", result.Priority);
        Assert.All(result.Signals, signal => Assert.True(signal.Applied));
    }

    [Theory]
    [InlineData(70, "High")]
    [InlineData(65, "Medium")]
    [InlineData(40, "Medium")]
    [InlineData(35, "Low")]
    [InlineData(0, "Low")]
    public void ScoreMapsToConfiguredPriority(int expectedScore, string expectedPriority)
    {
        var lead = LeadForScore(expectedScore);
        var result = LeadScoringService.Calculate(lead);

        Assert.Equal(expectedScore, result.Score);
        Assert.Equal(expectedPriority, result.Priority);
    }

    private static CustomerLead CompleteLead() => new()
    {
        FullName = "Fictional Customer",
        MobileNumber = "9876543210",
        State = "Maharashtra",
        City = "Pune",
        LoanType = "Home Loan",
        RequiredLoanAmount = 2500000,
        MonthlyIncome = 75000,
        EmploymentType = "Salaried",
        CallbackRequested = true,
        PreferredContactTime = "Evening",
        ConsentGiven = true,
        LeadStatus = "New",
    };

    private static CustomerLead LeadForScore(int score)
    {
        var lead = CompleteLead();
        switch (score)
        {
            case 70:
                lead.LeadStatus = "Closed";
                lead.CallbackRequested = false;
                break;
            case 65:
                lead.CallbackRequested = false;
                lead.ConsentGiven = false;
                break;
            case 40:
                lead.LeadStatus = "Closed";
                lead.CallbackRequested = false;
                lead.ConsentGiven = false;
                lead.PreferredContactTime = null;
                break;
            case 35:
                lead.LeadStatus = "Closed";
                lead.CallbackRequested = true;
                lead.MobileNumber = "invalid";
                lead.FullName = string.Empty;
                lead.City = string.Empty;
                lead.State = string.Empty;
                lead.LoanType = string.Empty;
                lead.RequiredLoanAmount = 0;
                lead.MonthlyIncome = 0;
                lead.EmploymentType = string.Empty;
                lead.PreferredContactTime = null;
                lead.ConsentGiven = true;
                break;
            case 0:
                lead.LeadStatus = "Closed";
                lead.CallbackRequested = false;
                lead.MobileNumber = "invalid";
                lead.FullName = string.Empty;
                lead.City = string.Empty;
                lead.State = string.Empty;
                lead.LoanType = string.Empty;
                lead.RequiredLoanAmount = 0;
                lead.MonthlyIncome = 0;
                lead.EmploymentType = string.Empty;
                lead.PreferredContactTime = null;
                lead.ConsentGiven = false;
                break;
        }
        return lead;
    }
}
