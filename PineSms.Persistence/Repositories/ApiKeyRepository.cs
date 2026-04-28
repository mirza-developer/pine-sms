using Microsoft.EntityFrameworkCore;
using PineSms.Core.Contracts;
using PineSms.Core.Entities;
using PineSms.Core.Features.ApiKey;
using PineSms.Persistence.Services;

namespace PineSms.Persistence.Repositories;

public class ApiKeyRepository : IApiKeyService
{
    private readonly PineSmsDbContext dbContext;

    public ApiKeyRepository(PineSmsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<CreateApiKeyResult> CreateApiKey(CreateApiKeyCommand command)
    {
        var key = GenerateKey();

        dbContext.ApiKey.Add(new ApiKey
        {
            Name = command.Name,
            Key = key,
            ExpireAt = command.ExpireAt,
            CreatedAt = DateTime.Now
        });

        await dbContext.SaveChangesAsync();

        return new CreateApiKeyResult
        {
            Success = true,
            Message = "کلید API با موفقیت ایجاد شد",
            GeneratedKey = key
        };
    }

    public async Task<List<ApiKey>> GetAllApiKeys()
    {
        return await dbContext.ApiKey.OrderByDescending(k => k.CreatedAt).ToListAsync();
    }

    public async Task<(bool success, string message)> DeleteApiKey(int id)
    {
        var apiKey = await dbContext.ApiKey.FindAsync(id);
        if (apiKey == null)
            return (false, "کلید API یافت نشد");

        dbContext.ApiKey.Remove(apiKey);
        await dbContext.SaveChangesAsync();
        return (true, "کلید API حذف شد");
    }

    public async Task<bool> ValidateApiKey(string key)
    {
        return await dbContext.ApiKey.AnyAsync(k => k.Key == key && k.ExpireAt > DateTime.Now);
    }

    private static string GenerateKey()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
