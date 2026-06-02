using PineSms.Core.Entities;

namespace PineSms.Core.Features.Customer;

public class GetCustomerByPhoneResult
{
    public Core.Entities.Customer? Customer { get; set; }
    public string? ErrorMessage { get; set; }
    public bool Success => Customer != null;
}
