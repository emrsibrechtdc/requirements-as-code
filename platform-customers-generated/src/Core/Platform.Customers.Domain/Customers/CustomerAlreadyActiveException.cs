using Platform.Shared.Exceptions;

namespace Platform.Customers.Domain.Customers;

public class CustomerAlreadyActiveException : BusinessException
{
    public string CustomerCode { get; init; }

    public CustomerAlreadyActiveException(string customerCode)
        : base("CUSTOMER_ALREADY_ACTIVE",
               "Customer Already Active",
               $"Customer '{customerCode}' is already active")
    {
        CustomerCode = customerCode;
    }
}