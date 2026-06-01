namespace PineSms.UI.Features.TrackingImport;

/// <summary>
/// Represents a row of tracking data from imported Excel file.
/// </summary>
public class TrackingDataRow
{
    public string OrderCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
}
