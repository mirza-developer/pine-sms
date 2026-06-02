using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PineSms.Persistence.Services;

/// <summary>
/// Design-time factory used by EF Core tooling (e.g. dotnet ef migrations add).
/// Configures SQL Server so that generated migrations use SQL Server column types,
/// and skips the database initialization that would otherwise run in the constructor.
/// </summary>
public class PineSmsDbContextFactory : IDesignTimeDbContextFactory<PineSmsDbContext>
{
    public PineSmsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PineSmsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PineSms_Design;Trusted_Connection=True;")
            .Options;

        return new PineSmsDbContext(options);
    }
}
