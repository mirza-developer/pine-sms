using Microsoft.EntityFrameworkCore;
using PineSms.Core.Entities;

namespace PineSms.Persistence.Services;

public class PineSmsDbContext : DbContext
{
    public PineSmsDbContext(DbContextOptions<PineSmsDbContext> options) : base(options)
    {
        if (Database.IsSqlite())
        {
            // EnsureCreated creates the schema from the model; safe to call multiple times
            Database.EnsureCreated();
        }
        else
        {
            // SQL Server: apply pending migrations (creates DB if not exists)
            Database.Migrate();
        }
    }

    public DbSet<Customer> Customer { get; set; }
    public DbSet<SmsLog> SmsLog { get; set; }
}
