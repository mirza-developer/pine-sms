using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PineSms.Identity.Models;
using PineSms.Identity.Seeds;

namespace PineSms.Identity.Services;

public class PineSmsIdentityContext : IdentityDbContext<ApplicationUser>
{
    // Set to true by the design-time factory to suppress database initialization
    internal static bool SkipInitialization = false;

    public PineSmsIdentityContext(DbContextOptions<PineSmsIdentityContext> options) : base(options)
    {
        if (SkipInitialization) return;

        if (Database.IsSqlite())
        {
            // EnsureCreated creates the schema from the model (incl. seeded data); safe to call multiple times
            Database.EnsureCreated();
        }
        else
        {
            // SQL Server: apply pending migrations (creates DB if not exists)
            Database.Migrate();
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.SeedApplicationUserData();
    }
}
