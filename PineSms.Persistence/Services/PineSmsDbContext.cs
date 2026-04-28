using Microsoft.EntityFrameworkCore;
using PineSms.Core.Entities;

namespace PineSms.Persistence.Services;

public class PineSmsDbContext : DbContext
{
    // Set to true by the design-time factory to suppress database initialization
    internal static bool SkipInitialization = false;

    public PineSmsDbContext(DbContextOptions<PineSmsDbContext> options) : base(options)
    {
        if (SkipInitialization) return;

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
    public DbSet<SmsSendJob> SmsSendJob { get; set; }
    public DbSet<SmsSendJobPart> SmsSendJobPart { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite index used by the scheduled SMS worker to efficiently find
        // pending parts that are due for sending.
        modelBuilder.Entity<SmsSendJobPart>()
            .HasIndex(p => new { p.Status, p.ScheduledAt })
            .HasDatabaseName("IX_SmsSendJobPart_Status_ScheduledAt");
    }
}
