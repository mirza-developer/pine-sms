using PineSms.Core.Entities;
using PineSms.Core.Features.ApiKey;

namespace PineSms.Core.Contracts;

public interface IApiKeyService
{
    Task<CreateApiKeyResult> CreateApiKey(CreateApiKeyCommand command);
    Task<List<ApiKey>> GetAllApiKeys();
    Task<(bool success, string message)> DeleteApiKey(int id);
    Task<bool> ValidateApiKey(string key);
}
