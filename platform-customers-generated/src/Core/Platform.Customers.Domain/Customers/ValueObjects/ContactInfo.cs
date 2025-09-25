using System.Text.RegularExpressions;

namespace Platform.Customers.Domain.Customers.ValueObjects;

public class ContactInfo
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string? PhoneNumber { get; }

    public ContactInfo(string firstName, string lastName, string email, string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
            
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));
            
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        // Domain validation - format checking
        if (!IsValidEmailFormat(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (firstName.Length > 100)
            throw new ArgumentException("First name cannot exceed 100 characters", nameof(firstName));
            
        if (lastName.Length > 100)
            throw new ArgumentException("Last name cannot exceed 100 characters", nameof(lastName));
            
        if (email.Length > 255)
            throw new ArgumentException("Email cannot exceed 255 characters", nameof(email));

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            if (phoneNumber.Length > 20)
                throw new ArgumentException("Phone number cannot exceed 20 characters", nameof(phoneNumber));
                
            if (!IsValidPhoneNumberFormat(phoneNumber))
                throw new ArgumentException("Invalid phone number format", nameof(phoneNumber));
        }
        
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim();
    }

    public string FullName => $"{FirstName} {LastName}";

    private static bool IsValidEmailFormat(string email)
    {
        // Basic email format validation using regex
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern);
    }

    private static bool IsValidPhoneNumberFormat(string phoneNumber)
    {
        // Allow various phone number formats: digits, spaces, hyphens, parentheses, plus sign
        var phonePattern = @"^[\+]?[1-9]?[\d\s\-\(\)]{7,}$";
        return Regex.IsMatch(phoneNumber, phonePattern);
    }

    public override bool Equals(object? obj)
    {
        return obj is ContactInfo other &&
               FirstName == other.FirstName &&
               LastName == other.LastName &&
               Email == other.Email &&
               PhoneNumber == other.PhoneNumber;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FirstName, LastName, Email, PhoneNumber);
    }
}