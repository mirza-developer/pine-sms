using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PineSms.Core.Contracts;
using PineSms.Core.Features.Account;
using PineSms.Identity.Models;
using PineSms.Identity.Utilities;

namespace PineSms.Identity.Services;

public class AuthService : IAuthService
{
    private readonly PineSmsIdentityContext context;
    private readonly IConfiguration configuration;

    public AuthService(PineSmsIdentityContext context, IConfiguration configuration)
    {
        this.context = context;
        this.configuration = configuration;
    }

    public async Task<GetUserLoginResult> Authenticate(GetUserLoginQuery request)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return new GetUserLoginResult { Success = false, Message = "نام کاربری و رمز عبور الزامی است" };

        if (request.Username.Length > 100 || request.Password.Length > 100)
            return new GetUserLoginResult { Success = false, Message = "نام کاربری یا رمز عبور نامعتبر است" };

        string hashedPassword = CryptographyTools.GetHashedStringSha256StringBuilder(request.Password);

        var user = await context.Users
            .FirstOrDefaultAsync(p => p.UserName == request.Username
                                   && p.PasswordHash == hashedPassword);

        if (user is null)
            return new GetUserLoginResult { Success = false, Message = "نام کاربری یا رمز عبور اشتباه است" };

        var claimsIdentity = await GetClaimsIdentityAsync(user);
        var configSec = configuration.GetSection("Identity");

        // Validate JWT configuration security
        var secret = configSec["Signing"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("JWT secret key must be configured and at least 32 characters long");

        var issuer = configSec["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JWT Issuer must be configured");

        var audience = configSec["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT Audience must be configured");

        if (!int.TryParse(configSec["Expires"], out var expiresHours) || expiresHours <= 0)
            throw new InvalidOperationException("JWT Expires must be configured as a positive integer (hours)");

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = DateTime.Now,
            NotBefore = DateTime.Now,
            Expires = DateTime.Now.AddHours(expiresHours),
            SigningCredentials = CryptographyTools.GetJwtCredential(secret),
            Subject = claimsIdentity
        };

        JwtSecurityTokenHandler tokenHandler = new();
        SecurityToken securityToken = tokenHandler.CreateToken(descriptor);
        string jwt = tokenHandler.WriteToken(securityToken);

        return new GetUserLoginResult { Success = true, Token = jwt };
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await context.Users
            .OrderBy(u => u.UserName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                PersianName = u.PersianName
            })
            .ToListAsync();
    }

    public async Task<List<UserDto>> GetNonAdminUsersAsync()
    {
        var adminUserIds = await context.UserRoles
            .Join(context.Roles!, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == "Admin")
            .Select(x => x.UserId)
            .ToListAsync();

        return await context.Users
            .Where(u => !adminUserIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                PersianName = u.PersianName
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)> CreateUserAsync(CreateUserCommand command)
    {
        if (await context.Users.AnyAsync(u => u.UserName == command.UserName))
            return (false, "این نام کاربری قبلاً ثبت شده است");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = command.UserName,
            NormalizedUserName = command.UserName.ToUpperInvariant(),
            PersianName = command.PersianName,
            PasswordHash = CryptographyTools.GetHashedStringSha256StringBuilder(command.Password),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString(),
            AccessFailedCount = 0,
            LockoutEnabled = false,
            TwoFactorEnabled = false,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (true, "کاربر با موفقیت ایجاد شد");
    }

    public async Task<(bool success, string message)> UpdateUserAsync(UpdateUserCommand command)
    {
        var user = await context.Users.FindAsync(command.Id);
        if (user is null)
            return (false, "کاربر یافت نشد");

        if (!string.IsNullOrEmpty(command.NewPassword) && command.NewPassword.Length < 6)
            return (false, "رمز عبور باید حداقل ۶ کاراکتر باشد");

        user.PersianName = command.PersianName;

        if (!string.IsNullOrEmpty(command.NewPassword))
            user.PasswordHash = CryptographyTools.GetHashedStringSha256StringBuilder(command.NewPassword);

        await context.SaveChangesAsync();
        return (true, "اطلاعات کاربر به‌روزرسانی شد");
    }

    public async Task<(bool success, string message)> DeleteUserAsync(string userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null)
            return (false, "کاربر یافت نشد");

        var isAdmin = await context.UserRoles
            .Join(context.Roles!, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == "Admin");

        if (isAdmin)
            return (false, "امکان حذف کاربر مدیر وجود ندارد");

        var userRoles = context.UserRoles.Where(ur => ur.UserId == userId);
        context.UserRoles.RemoveRange(userRoles);
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return (true, "کاربر با موفقیت حذف شد");
    }

    private async Task<ClaimsIdentity> GetClaimsIdentityAsync(ApplicationUser user)
    {
        var roles = await context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(context.Roles!, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
            .ToListAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Surname, user.PersianName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        return new ClaimsIdentity(claims);
    }
}
