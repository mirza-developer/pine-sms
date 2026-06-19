namespace PineAI.Core.Features.Account;

public class UpdateUserCommand
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام فارسی الزامی است")]
    [StringLength(128, ErrorMessage = "نام فارسی حداکثر ۱۲۸ کاراکتر است")]
    public string PersianName { get; set; } = string.Empty;

    /// <summary>Leave empty to keep the current password.</summary>
    [StringLength(64, ErrorMessage = "رمز عبور حداکثر ۶۴ کاراکتر است")]
    public string? NewPassword { get; set; }
}
