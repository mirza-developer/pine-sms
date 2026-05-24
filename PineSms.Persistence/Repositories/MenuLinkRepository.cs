using Microsoft.EntityFrameworkCore;
using PineSms.Core.Contracts;
using PineSms.Core.Features.MenuLink;
using PineSms.Persistence.Services;

namespace PineSms.Persistence.Repositories;

public class MenuLinkRepository : IMenuLinkService
{
    private readonly PineSmsDbContext context;

    public MenuLinkRepository(PineSmsDbContext context)
    {
        this.context = context;
    }

    public async Task<List<MenuLinkDto>> GetAllMenuLinksAsync()
    {
        return await context.MenuLink
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new MenuLinkDto
            {
                Id = m.Id,
                Title = m.Title,
                Url = m.Url,
                IconName = m.IconName,
                SectionLabel = m.SectionLabel,
                DisplayOrder = m.DisplayOrder
            })
            .ToListAsync();
    }

    public async Task<List<MenuLinkDto>> GetUserMenuLinksAsync(string userId)
    {
        return await context.UserMenuLink
            .Where(um => um.UserId == userId)
            .OrderBy(um => um.MenuLink.DisplayOrder)
            .Select(um => new MenuLinkDto
            {
                Id = um.MenuLink.Id,
                Title = um.MenuLink.Title,
                Url = um.MenuLink.Url,
                IconName = um.MenuLink.IconName,
                SectionLabel = um.MenuLink.SectionLabel,
                DisplayOrder = um.MenuLink.DisplayOrder
            })
            .ToListAsync();
    }

    public async Task SaveUserMenuLinksAsync(string userId, List<int> menuLinkIds)
    {
        // Remove all existing links for this user
        var existing = context.UserMenuLink.Where(um => um.UserId == userId);
        context.UserMenuLink.RemoveRange(existing);

        // Add the new selections
        foreach (var linkId in menuLinkIds.Distinct())
        {
            context.UserMenuLink.Add(new Core.Entities.UserMenuLink
            {
                UserId = userId,
                MenuLinkId = linkId
            });
        }

        await context.SaveChangesAsync();
    }
}
