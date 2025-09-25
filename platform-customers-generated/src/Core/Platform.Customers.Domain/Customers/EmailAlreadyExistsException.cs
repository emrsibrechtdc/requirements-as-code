using Platform.Shared.Exceptions;

namespace Platform.Customers.Domain.Customers;

public class EmailAlreadyExistsException : BusinessException
{
    public string Email { get; init; }

    public EmailAlreadyExistsException(string email)
        : base("EMAIL_ALREADY_EXISTS",
               "Email Already Exists",
               $"A customer with email '{email}' already exists")
    {
        Email = email;
    }
}