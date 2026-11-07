namespace IndiaFinanceAI.Application.Leads;

public interface ICustomerLeadService
{
    Task<Guid> CreateAsync(CreateCustomerLeadRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminLeadListItem>> SearchAsync(string? loanType, string? leadStatus, string? search, CancellationToken cancellationToken);
    Task<AdminLeadDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> UpdateManagementAsync(Guid id, UpdateLeadManagementRequest request, CancellationToken cancellationToken);
}
