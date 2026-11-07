using System.Globalization;
using IndiaFinanceAI.Domain;

namespace IndiaFinanceAI.Application.Leads;

public static class LeadSummaryGenerator
{
    public static string Generate(CustomerLead lead)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(lead.LoanType)) parts.Add($"{lead.LoanType} enquiry");
        if (lead.RequiredLoanAmount > 0) parts.Add($"requested amount {lead.RequiredLoanAmount.ToString("C0", CultureInfo.GetCultureInfo("en-IN"))}");
        if (lead.MonthlyIncome > 0) parts.Add($"monthly income {lead.MonthlyIncome.ToString("C0", CultureInfo.GetCultureInfo("en-IN"))}");
        if (!string.IsNullOrWhiteSpace(lead.EmploymentType)) parts.Add(lead.EmploymentType);
        if (!string.IsNullOrWhiteSpace(lead.City) && !string.IsNullOrWhiteSpace(lead.State)) parts.Add($"location {lead.City}, {lead.State}");
        if (lead.CallbackRequested) parts.Add("callback requested");
        if (!string.IsNullOrWhiteSpace(lead.PreferredContactTime)) parts.Add($"preferred contact time {lead.PreferredContactTime}");
        return parts.Count == 0 ? "No loan requirement details were provided." : string.Join("; ", parts) + ".";
    }
}
