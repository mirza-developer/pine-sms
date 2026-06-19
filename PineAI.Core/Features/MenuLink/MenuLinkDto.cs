namespace PineAI.Core.Features.MenuLink;

public class MenuLinkDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string SectionLabel { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
