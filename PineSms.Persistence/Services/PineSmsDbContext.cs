using Microsoft.EntityFrameworkCore;
using PineSms.Core.Entities;

namespace PineSms.Persistence.Services;

public class PineSmsDbContext : DbContext
{
    public PineSmsDbContext(DbContextOptions<PineSmsDbContext> options) : base(options)
    {
        if (Database.IsSqlite())
            Database.EnsureCreated();
        else
            Database.Migrate();
    }

    public DbSet<Customer> Customer { get; set; }
    public DbSet<SmsLog> SmsLog { get; set; }
}
