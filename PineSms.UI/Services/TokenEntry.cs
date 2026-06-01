namespace PineSms.UI.Services;

/// <summary>
/// Represents a token entry for Excel download.
/// </summary>
public class TokenEntry
{
    public List<string> Phones { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
