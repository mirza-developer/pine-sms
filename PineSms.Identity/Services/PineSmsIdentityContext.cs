using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PineSms.Identity.Models;
using PineSms.Identity.Seeds;

namespace PineSms.Identity.Services;

public class PineSmsIdentityContext : IdentityDbContext<ApplicationUser>
{
    public PineSmsIdentityContext(DbContextOptions<PineSmsIdentityContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.SeedApplicationUserData();
    }
}
