using IndiaFinanceAI.Application.Leads;
using IndiaFinanceAI.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndiaFinanceAI.Infrastructure.Persistence;

public sealed class CustomerLeadRepository(FinanceDbContext dbContext) : ICustomerLeadRepository
{
    public async Task AddAsync(CustomerLead lead, CancellationToken cancellationToken)
    {
        await dbContext.CustomerLeads.AddAsync(lead, cancellationToken);
        if (lead.WhatsAppConsentGiven)
        {
            await dbContext.WhatsAppCommunicationEvents.AddAsync(new WhatsAppCommunicationEvent
            {
                CustomerLeadId = lead.Id,
                EventType = "ConsentGiven",
                Status = "OptedIn",
                CreatedAt = lead.WhatsAppConsentAt ?? DateTime.UtcNow,
            }, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerLead>> SearchAsync(
        string? loanType,
        string? leadStatus,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CustomerLeads.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(loanType))
        {
            query = query.Where(lead => lead.LoanType == loanType);
        }

        if (!string.IsNullOrWhiteSpace(leadStatus))
        {
            query = query.Where(lead => lead.LeadStatus == leadStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = Guid.TryParse(value, out var id)
                ? query.Where(lead => lead.Id == id || EF.Functions.ILike(lead.FullName, $"%{value}%"))
                : query.Where(lead => EF.Functions.ILike(lead.FullName, $"%{value}%"));
        }

        return await query
            .OrderByDescending(lead => lead.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerLead?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.CustomerLeads.FirstOrDefaultAsync(lead => lead.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
