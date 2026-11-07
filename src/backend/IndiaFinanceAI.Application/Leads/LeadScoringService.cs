using IndiaFinanceAI.Domain;

namespace IndiaFinanceAI.Application.Leads;

public sealed record LeadScoreSignal(string Name, int Points, bool Applied);
public sealed record LeadScoreResult(int Score, string Priority, IReadOnlyList<LeadScoreSignal> Signals);

public static class LeadScoringService
{
    public static LeadScoreResult Calculate(CustomerLead lead)
    {
        var signals = new[]
        {
            new LeadScoreSignal("Requested callback", 20, lead.CallbackRequested),
            new LeadScoreSignal("Valid mobile number", 20, System.Text.RegularExpressions.Regex.IsMatch(lead.MobileNumber, "^[6-9][0-9]{9}$")),
            new LeadScoreSignal("Required enquiry fields completed", 20, HasRequiredFields(lead)),
            new LeadScoreSignal("Preferred contact time selected", 15, !string.IsNullOrWhiteSpace(lead.PreferredContactTime)),
            new LeadScoreSignal("Contact consent given", 15, lead.ConsentGiven),
            new LeadScoreSignal("Active loan enquiry", 10, IsActive(lead.LeadStatus)),
        };
        var score = signals.Where(signal => signal.Applied).Sum(signal => signal.Points);
        var priority = score >= 70 ? "High" : score >= 40 ? "Medium" : "Low";
        return new LeadScoreResult(score, priority, signals);
    }

    private static bool HasRequiredFields(CustomerLead lead) =>
        !string.IsNullOrWhiteSpace(lead.FullName)
        && !string.IsNullOrWhiteSpace(lead.MobileNumber)
        && !string.IsNullOrWhiteSpace(lead.State)
        && !string.IsNullOrWhiteSpace(lead.City)
        && !string.IsNullOrWhiteSpace(lead.LoanType)
        && lead.RequiredLoanAmount > 0
        && lead.MonthlyIncome > 0
        && !string.IsNullOrWhiteSpace(lead.EmploymentType);

    private static bool IsActive(string status) => status is "New" or "Contacted" or "Qualified";
}
