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
        string hashedPassword = CryptographyTools.GetHashedStringSha256StringBuilder(request.Password);

        var user = await context.Users
            .FirstOrDefaultAsync(p => p.UserName == request.Username
                                   && p.PasswordHash == hashedPassword);

        if (user is null)
            return new GetUserLoginResult { Success = false, Message = "نام کاربری یا رمز عبور اشتباه است" };

        var claimsIdentity = await GetClaimsIdentityAsync(user);
        var configSec = configuration.GetSection("Identity");

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = configSec["Issuer"],
            Audience = configSec["Audience"],
            IssuedAt = DateTime.Now,
            NotBefore = DateTime.Now,
            Expires = DateTime.Now.AddHours(int.Parse(configSec["Expires"] ?? "8")),
            SigningCredentials = CryptographyTools.GetJwtCredential(configSec["Signing"] ?? "PineSms_JWT_Secret_Key_32Chars"),
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
