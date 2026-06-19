using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PineAI.Persistence.Services;

/// <summary>
/// Design-time factory used by EF Core tooling (e.g. dotnet ef migrations add).
/// Configures SQL Server so that generated migrations use SQL Server column types,
/// and skips the database initialization that would otherwise run in the constructor.
/// </summary>
public class PineAIDbContextFactory : IDesignTimeDbContextFactory<PineAIDbContext>
{
    public PineAIDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PineAIDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PineAI_Design;Trusted_Connection=True;")
            .Options;

        return new PineAIDbContext(options);
    }
}
