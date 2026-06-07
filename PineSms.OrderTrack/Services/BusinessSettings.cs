namespace PineSms.OrderTrack.Services;

public class NavItemConfig
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class BusinessSettings
{
    public string NameFa { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string LogoAlt { get; set; } = string.Empty;
    public string TrackingUrl { get; set; } = string.Empty;
    public string ShopUrl { get; set; } = string.Empty;
    public string CartUrl { get; set; } = string.Empty;
    public string SupportBaleUrl { get; set; } = string.Empty;
    public string SupportBaleUsername { get; set; } = string.Empty;
    public string BaleChannelUrl { get; set; } = string.Empty;
    public string TelegramUrl { get; set; } = string.Empty;
    public string InstagramUrl { get; set; } = string.Empty;
    public string RubikaUrl { get; set; } = string.Empty;
    public string RubikaIconUrl { get; set; } = string.Empty;
    public string BaleIconUrl { get; set; } = string.Empty;
    public string CopyrightFa { get; set; } = string.Empty;
    public List<NavItemConfig> NavItems { get; set; } = new();
}
