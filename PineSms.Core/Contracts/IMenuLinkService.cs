using PineSms.Core.Features.MenuLink;

namespace PineSms.Core.Contracts;

public interface IMenuLinkService
{
    Task<List<MenuLinkDto>> GetAllMenuLinksAsync();
    Task<List<MenuLinkDto>> GetUserMenuLinksAsync(string userId);
    Task SaveUserMenuLinksAsync(string userId, List<int> menuLinkIds);
}
