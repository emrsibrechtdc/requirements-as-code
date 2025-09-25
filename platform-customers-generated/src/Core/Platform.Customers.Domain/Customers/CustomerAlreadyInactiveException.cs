using Platform.Shared.Exceptions;

namespace Platform.Customers.Domain.Customers;

public class CustomerAlreadyInactiveException : BusinessException
{
    public string CustomerCode { get; init; }

    public CustomerAlreadyInactiveException(string customerCode)
        : base("CUSTOMER_ALREADY_INACTIVE",
               "Customer Already Inactive",
               $"Customer '{customerCode}' is already inactive")
    {
        CustomerCode = customerCode;
    }
}