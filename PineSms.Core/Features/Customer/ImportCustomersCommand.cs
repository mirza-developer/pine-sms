namespace PineSms.Core.Features.Customer;

public class ImportCustomersCommand
{
    public List<string> PhoneNumbers { get; set; } = new();
    public bool IgnoreInvalid { get; set; } = false;
}

public class ImportCustomersResult
{
    public bool Success { get; set; }
    public int InsertedCount { get; set; }
    public List<string> InvalidNumbers { get; set; } = new();
    public List<string> DuplicateNumbers { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
