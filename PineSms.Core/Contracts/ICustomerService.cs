using PineSms.Core.Entities;
using PineSms.Core.Features.Customer;

namespace PineSms.Core.Contracts;

public interface ICustomerService
{
    Task<(bool success, string message)> InsertCustomer(InsertCustomerCommand command, string userId);
    Task<ImportCustomersResult> ImportCustomers(ImportCustomersCommand command, string userId);
    Task<List<Customer>> GetCustomersByDateRange(DateTime from, DateTime to);
    Task<Customer?> GetCustomerByPhoneNumber(string phoneNumber);
}
