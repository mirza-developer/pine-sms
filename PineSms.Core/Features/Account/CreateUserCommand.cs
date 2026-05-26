namespace PineSms.Core.Features.Account;

public class CreateUserCommand
{
    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [StringLength(64, ErrorMessage = "نام کاربری حداکثر ۶۴ کاراکتر است")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام فارسی الزامی است")]
    [StringLength(128, ErrorMessage = "نام فارسی حداکثر ۱۲۸ کاراکتر است")]
    public string PersianName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [StringLength(64, MinimumLength = 6, ErrorMessage = "رمز عبور باید بین ۶ تا ۶۴ کاراکتر باشد")]
    public string Password { get; set; } = string.Empty;
}
