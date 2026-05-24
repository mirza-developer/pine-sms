namespace PineSms.Core.Features.MenuLink;

public class SaveUserMenuLinksCommand
{
    public string UserId { get; set; } = string.Empty;
    public List<int> MenuLinkIds { get; set; } = [];
}
