namespace PineAI.Core.Features.Customer;

public class ImportCustomersResult
{
    public bool Success { get; set; }
    public int InsertedCount { get; set; }
    public List<string> InvalidNumbers { get; set; } = new();
    public List<string> DuplicateNumbers { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
