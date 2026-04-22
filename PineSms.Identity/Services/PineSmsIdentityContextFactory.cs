using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PineSms.Identity.Services;

/// <summary>
/// Design-time factory used by EF Core tooling (e.g. dotnet ef migrations add).
/// Configures SQL Server so that generated migrations use SQL Server column types,
/// and skips the database initialization that would otherwise run in the constructor.
/// </summary>
public class PineSmsIdentityContextFactory : IDesignTimeDbContextFactory<PineSmsIdentityContext>
{
    public PineSmsIdentityContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PineSmsIdentityContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PineSmsIdentity_Design;Trusted_Connection=True;")
            .Options;

        PineSmsIdentityContext.SkipInitialization = true;
        try
        {
            return new PineSmsIdentityContext(options);
        }
        finally
        {
            PineSmsIdentityContext.SkipInitialization = false;
        }
    }
}
