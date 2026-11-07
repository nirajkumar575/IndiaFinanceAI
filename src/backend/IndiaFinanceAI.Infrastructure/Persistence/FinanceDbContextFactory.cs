using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IndiaFinanceAI.Infrastructure.Persistence;

public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FinanceDatabase")
            ?? throw new InvalidOperationException("Set ConnectionStrings__FinanceDatabase for EF design-time commands.");

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FinanceDbContext(options);
    }
}
