namespace PineAI.Core.Features.Account;

public class GetUserLoginResult
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
