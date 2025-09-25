using Platform.Shared.Exceptions;

namespace Platform.Customers.Domain.Customers;

public class CustomerNotFoundException : BusinessException
{
    public string CustomerCode { get; init; }

    public CustomerNotFoundException(string customerCode)
        : base("CUSTOMER_NOT_FOUND",
               "Customer Not Found",
               $"The customer with code '{customerCode}' was not found")
    {
        CustomerCode = customerCode;
    }
}