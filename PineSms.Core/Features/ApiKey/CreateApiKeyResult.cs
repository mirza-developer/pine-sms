namespace PineSms.Core.Features.ApiKey;

public class CreateApiKeyResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? GeneratedKey { get; set; }
    public int? Id { get; set; }
}
