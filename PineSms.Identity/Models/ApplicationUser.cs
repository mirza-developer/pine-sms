using Microsoft.AspNetCore.Identity;

namespace PineSms.Identity.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(128)]
    public string PersianName { get; set; } = string.Empty;
}
