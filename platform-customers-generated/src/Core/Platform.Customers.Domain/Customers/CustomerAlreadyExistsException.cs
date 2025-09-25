using Platform.Shared.Exceptions;

namespace Platform.Customers.Domain.Customers;

public class CustomerAlreadyExistsException : BusinessException
{
    public string CustomerCode { get; init; }

    public CustomerAlreadyExistsException(string customerCode)
        : base("CUSTOMER_ALREADY_EXISTS",
               "Customer Already Exists",
               $"A customer with code '{customerCode}' already exists")
    {
        CustomerCode = customerCode;
    }
}