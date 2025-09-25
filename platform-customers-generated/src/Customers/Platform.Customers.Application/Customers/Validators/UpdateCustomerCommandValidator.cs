using FluentValidation;
using Platform.Customers.Application.Customers.Commands;

namespace Platform.Customers.Application.Customers.Validators;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerCode).NotEmpty().NotNull().MaximumLength(50)
            .WithMessage("Customer code is required and cannot exceed 50 characters");
            
        RuleFor(x => x.CompanyName).NotEmpty().NotNull().MaximumLength(200)
            .WithMessage("Company name is required and cannot exceed 200 characters");
            
        RuleFor(x => x.ContactFirstName).NotEmpty().NotNull().MaximumLength(100)
            .WithMessage("Contact first name is required and cannot exceed 100 characters");
            
        RuleFor(x => x.ContactLastName).NotEmpty().NotNull().MaximumLength(100)
            .WithMessage("Contact last name is required and cannot exceed 100 characters");
            
        RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress().MaximumLength(255)
            .WithMessage("A valid email address is required and cannot exceed 255 characters");
            
        RuleFor(x => x.PhoneNumber).MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number cannot exceed 20 characters");
            
        RuleFor(x => x.AddressLine1).NotEmpty().NotNull().MaximumLength(255)
            .WithMessage("Address line 1 is required and cannot exceed 255 characters");
            
        RuleFor(x => x.AddressLine2).MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.AddressLine2))
            .WithMessage("Address line 2 cannot exceed 255 characters");
            
        RuleFor(x => x.City).NotEmpty().NotNull().MaximumLength(100)
            .WithMessage("City is required and cannot exceed 100 characters");
            
        RuleFor(x => x.State).NotEmpty().NotNull().MaximumLength(50)
            .WithMessage("State is required and cannot exceed 50 characters");
            
        RuleFor(x => x.PostalCode).NotEmpty().NotNull().MaximumLength(20)
            .WithMessage("Postal code is required and cannot exceed 20 characters");
            
        RuleFor(x => x.Country).NotEmpty().NotNull().MaximumLength(100)
            .WithMessage("Country is required and cannot exceed 100 characters");
    }
}