using PineAI.Core.Entities;

namespace PineAI.Core.Features.Customer;

public class GetCustomerByPhoneResult
{
    public Core.Entities.Customer? Customer { get; set; }
    public string? ErrorMessage { get; set; }
    public bool Success => Customer != null;
}
