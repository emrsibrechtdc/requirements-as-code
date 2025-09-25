using Platform.Shared.Auditing;
using Platform.Shared.Ddd.Domain;
using Platform.Shared.Entities;
using Platform.Shared.Instrumentation;
using Platform.Shared.MultiProduct;
using Platform.Customers.Domain.Customers.ValueObjects;

namespace Platform.Customers.Domain.Customers;

public class Customer : FullyAuditedActivableAggregateRoot<Guid>, IMultiProductObject, IInstrumentationObject
{
    public string Product { get; set; } = default!;
    public string CustomerCode { get; private set; } = default!;
    public string CustomerType { get; private set; } = default!;
    public string CompanyName { get; private set; } = default!;
    public ContactInfo ContactInfo { get; private set; } = default!;
    public Address Address { get; private set; } = default!;

    // Private constructor for EF Core
    private Customer() { }

    private Customer(string customerCode, string customerType, string companyName, ContactInfo contactInfo, Address address)
    {
        CustomerCode = customerCode;
        CustomerType = customerType;
        CompanyName = companyName;
        ContactInfo = contactInfo;
        Address = address;
    }

    // Static factory method with domain business logic
    public static Customer Create(string customerCode, string customerType, string companyName, 
        string contactFirstName, string contactLastName, string email, string? phoneNumber,
        string addressLine1, string? addressLine2, string city, string state, string postalCode, string country)
    {
        // Domain invariant validation - rules that must ALWAYS be true
        if (string.IsNullOrWhiteSpace(customerCode))
            throw new ArgumentException("Customer code cannot be empty", nameof(customerCode));
        
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name cannot be empty", nameof(companyName));

        // Additional domain rules that don't require external dependencies
        if (customerCode.Length > 50)
            throw new ArgumentException("Customer code cannot exceed 50 characters", nameof(customerCode));

        if (companyName.Length > 200)
            throw new ArgumentException("Company name cannot exceed 200 characters", nameof(companyName));

        // Validate customer type
        if (!IsValidCustomerType(customerType))
            throw new ArgumentException($"Invalid customer type: {customerType}", nameof(customerType));

        // Create value objects with their own validation
        var contactInfo = new ContactInfo(contactFirstName, contactLastName, email, phoneNumber);
        var address = new Address(addressLine1, addressLine2, city, state, postalCode, country);

        return new Customer(customerCode, customerType, companyName, contactInfo, address);
    }

    // Domain business logic methods
    public void UpdateCompanyName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name cannot be empty", nameof(companyName));
        
        if (companyName.Length > 200)
            throw new ArgumentException("Company name cannot exceed 200 characters", nameof(companyName));
        
        CompanyName = companyName;
    }

    public void UpdateContactInfo(string contactFirstName, string contactLastName, string email, string? phoneNumber)
    {
        ContactInfo = new ContactInfo(contactFirstName, contactLastName, email, phoneNumber);
    }

    public void UpdateAddress(string addressLine1, string? addressLine2, string city, string state, string postalCode, string country)
    {
        Address = new Address(addressLine1, addressLine2, city, state, postalCode, country);
    }

    public void UpdateCustomerType(string customerType)
    {
        if (!IsValidCustomerType(customerType))
            throw new ArgumentException($"Invalid customer type: {customerType}", nameof(customerType));
        
        CustomerType = customerType;
    }

    public new void Activate()
    {
        if (IsActive)
        {
            throw new CustomerAlreadyActiveException(CustomerCode);
        }
        base.Activate();
    }

    public new void Deactivate()
    {
        if (!IsActive)
        {
            throw new CustomerAlreadyInactiveException(CustomerCode);
        }
        base.Deactivate();
    }

    public Dictionary<string, string> ToInstrumentationProperties()
    {
        return new Dictionary<string, string>
        {
            { "customerCode", CustomerCode },
            { "customerType", CustomerType },
            { "companyName", CompanyName },
            { "contactEmail", ContactInfo.Email }
        };
    }

    private static bool IsValidCustomerType(string customerType)
    {
        var validTypes = new[] { "ENTERPRISE", "SMALL_BUSINESS", "RETAIL", "INDIVIDUAL" };
        return validTypes.Contains(customerType?.ToUpper());
    }
}