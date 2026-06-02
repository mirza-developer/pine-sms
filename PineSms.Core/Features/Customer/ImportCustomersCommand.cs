namespace PineSms.Core.Features.Customer;

public class ImportCustomersCommand
{
    public List<string> PhoneNumbers { get; set; } = new();
    public bool IgnoreInvalid { get; set; } = false;
    /// <summary>Persian date string (yyyy/MM/dd). If null/empty, current UTC time is used.</summary>
    public string? SaveDate { get; set; }
}
