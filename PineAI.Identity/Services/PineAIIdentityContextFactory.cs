using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PineAI.Identity.Services;

/// <summary>
/// Design-time factory used by EF Core tooling (e.g. dotnet ef migrations add).
/// Configures SQL Server so that generated migrations use SQL Server column types,
/// and skips the database initialization that would otherwise run in the constructor.
/// </summary>
public class PineAIIdentityContextFactory : IDesignTimeDbContextFactory<PineAIIdentityContext>
{
    public PineAIIdentityContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PineAIIdentityContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PineAIIdentity_Design;Trusted_Connection=True;")
            .Options;

        PineAIIdentityContext.SkipInitialization = true;
        try
        {
            return new PineAIIdentityContext(options);
        }
        finally
        {
            PineAIIdentityContext.SkipInitialization = false;
        }
    }
}
