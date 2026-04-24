namespace PineSms.Core.Features.Customer;

public class UpdateCustomerCommand
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? Gender { get; set; }
    public int? BirthYear { get; set; }
    public string? BirthDate { get; set; }
    public bool IsTester { get; set; } = false;
}

public class UpdateCustomerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
