using IndiaFinanceAI.Domain;

namespace IndiaFinanceAI.Application.Leads;

public interface ICustomerLeadRepository
{
    Task AddAsync(CustomerLead lead, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerLead>> SearchAsync(string? loanType, string? leadStatus, string? search, CancellationToken cancellationToken);
    Task<CustomerLead?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
