using IndiaFinanceAI.Domain;

namespace IndiaFinanceAI.Application.Leads;

public sealed class CustomerLeadService(ICustomerLeadRepository repository) : ICustomerLeadService
{
    public async Task<Guid> CreateAsync(CreateCustomerLeadRequest request, CancellationToken cancellationToken)
    {
        var lead = new CustomerLead
        {
            FullName = request.FullName.Trim(),
            MobileNumber = request.MobileNumber,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            State = request.State.Trim(),
            City = request.City.Trim(),
            LoanType = request.LoanType,
            RequiredLoanAmount = request.RequiredLoanAmount,
            MonthlyIncome = request.MonthlyIncome,
            EmploymentType = request.EmploymentType,
            ConsentGiven = request.ConsentGiven,
            WhatsAppConsentGiven = request.WhatsAppConsentGiven,
            WhatsAppConsentAt = request.WhatsAppConsentGiven ? DateTime.UtcNow : null,
            CallbackRequested = request.CallbackRequested,
            PreferredContactTime = request.PreferredContactTime,
            LeadSource = request.LeadSource,
            CreatedAt = DateTime.UtcNow,
            LeadStatus = "New",
        };

        await repository.AddAsync(lead, cancellationToken);
        return lead.Id;
    }

    public async Task<IReadOnlyList<AdminLeadListItem>> SearchAsync(
        string? loanType,
        string? leadStatus,
        string? search,
        CancellationToken cancellationToken)
    {
        var leads = await repository.SearchAsync(loanType, leadStatus, search, cancellationToken);
        return leads.Select(ToListItem).ToArray();
    }

    public async Task<AdminLeadDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var lead = await repository.GetByIdAsync(id, cancellationToken);
        if (lead is null) return null;

        var score = LeadScoringService.Calculate(lead);
        return new AdminLeadDetails(
            lead.Id,
            lead.FullName,
            lead.MobileNumber,
            lead.Email,
            lead.City,
            lead.State,
            lead.LoanType,
            lead.RequiredLoanAmount,
            lead.MonthlyIncome,
            lead.EmploymentType,
            lead.ConsentGiven,
            lead.CreatedAt,
            lead.CallbackRequested,
            lead.PreferredContactTime,
            lead.LeadSource,
            lead.LeadStatus,
            lead.FollowUpDate,
            lead.FollowUpNotes,
            score.Score,
            score.Priority,
            score.Signals,
            LeadSummaryGenerator.Generate(lead),
            lead.WhatsAppConsentGiven,
            lead.WhatsAppConsentAt,
            lead.WhatsAppOptedOutAt,
            lead.LastWhatsAppActivityAt,
            lead.LastWhatsAppStatus);
    }

    public async Task<bool> UpdateManagementAsync(
        Guid id,
        UpdateLeadManagementRequest request,
        CancellationToken cancellationToken)
    {
        var lead = await repository.GetByIdAsync(id, cancellationToken);
        if (lead is null) return false;

        lead.LeadStatus = request.LeadStatus;
        lead.FollowUpDate = request.FollowUpDate;
        lead.FollowUpNotes = string.IsNullOrWhiteSpace(request.FollowUpNotes) ? null : request.FollowUpNotes.Trim();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static AdminLeadListItem ToListItem(CustomerLead lead)
    {
        var score = LeadScoringService.Calculate(lead);
        return new AdminLeadListItem(
            lead.Id,
            lead.FullName,
            lead.City,
            lead.State,
            lead.LoanType,
            lead.RequiredLoanAmount,
            lead.CreatedAt,
            lead.LeadStatus,
            lead.LeadSource,
            lead.FollowUpDate,
            score.Score,
            score.Priority);
    }
}
