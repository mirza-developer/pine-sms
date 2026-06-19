using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PineAI.Identity.Models;
using PineAI.Identity.Utilities;

namespace PineAI.Identity.Seeds;

public static class SeedData
{
    public static void SeedApplicationUserData(this ModelBuilder modelBuilder)
    {
        string adminId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
        string adminRoleId = "b2c3d4e5-f6a7-8901-bcde-f12345678901";

        modelBuilder.Entity<ApplicationUser>()
            .HasData(new ApplicationUser
            {
                Id = adminId,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                PersianName = "مدیر سیستم",
                PasswordHash = CryptographyTools.GetHashedStringSha256StringBuilder("Admin@123"),
                ConcurrencyStamp = "be993071-73a8-4aa6-aecd-614a40aad3ec",
                AccessFailedCount = 0,
                LockoutEnabled = false,
                TwoFactorEnabled = false,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                SecurityStamp = "static-security-stamp-pine-sms-admin"
            });

        modelBuilder.Entity<IdentityRole>()
            .HasData(new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "static-concurrency-stamp-admin-role"
            });

        modelBuilder.Entity<IdentityUserRole<string>>()
            .HasData(new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminId
            });
    }
}
