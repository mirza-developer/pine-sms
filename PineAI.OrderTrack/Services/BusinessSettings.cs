namespace PineAI.OrderTrack.Services;

public class ColorPalette
{
    public string PineBg { get; set; } = string.Empty;
    public string PineSurface { get; set; } = string.Empty;
    public string PineNeutral { get; set; } = string.Empty;
    public string PineText { get; set; } = string.Empty;
    public string PineAccent1 { get; set; } = string.Empty;
    public string PineAccent2 { get; set; } = string.Empty;
    public string PineAccent2Dark { get; set; } = string.Empty;
    public string PineAccent2Darker { get; set; } = string.Empty;
    public string PineAccent2Light { get; set; } = string.Empty;
    public string PineFooterBg { get; set; } = string.Empty;
    public string PineFooterBottom { get; set; } = string.Empty;
    public string PineFooterText { get; set; } = string.Empty;
    public string PineTopbar { get; set; } = string.Empty;
}

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
    public ColorPalette Colors { get; set; } = new();
    public string AboutFa { get; set; } = string.Empty;
    public string PostalTrackingUrl { get; set; } = string.Empty;
    public string CopyrightFa { get; set; } = string.Empty;
    public List<NavItemConfig> NavItems { get; set; } = new();
}
