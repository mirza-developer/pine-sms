using System.ComponentModel.DataAnnotations;

namespace PineSms.Core.Entities;

public class MenuLink : IBaseEntity
{
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Url { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string IconName { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string SectionLabel { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsShown { get; set; } = true;

    public ICollection<UserMenuLink> UserMenuLinks { get; set; } = [];
}
