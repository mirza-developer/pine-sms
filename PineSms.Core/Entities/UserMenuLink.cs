namespace PineSms.Core.Entities;

public class UserMenuLink : IBaseEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int MenuLinkId { get; set; }
    public MenuLink MenuLink { get; set; } = null!;
}
