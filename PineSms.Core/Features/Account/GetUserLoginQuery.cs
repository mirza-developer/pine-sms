namespace PineSms.Core.Features.Account;

public class GetUserLoginQuery
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
