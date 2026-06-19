using PineAI.Core.Entities;
using PineAI.Core.Features.ApiKey;

namespace PineAI.Core.Contracts;

public interface IApiKeyService
{
    Task<CreateApiKeyResult> CreateApiKey(CreateApiKeyCommand command);
    Task<List<ApiKey>> GetAllApiKeys();
    Task<DeleteApiKeyResult> DeleteApiKey(int id);
    Task<bool> ValidateApiKey(string key);
}
