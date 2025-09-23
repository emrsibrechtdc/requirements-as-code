using FluentValidation;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Domain;

namespace Platform.Locations.Application.Locations.Validators;

public class RegisterLocationCommandValidator : AbstractValidator<RegisterLocationCommand>
{
    public RegisterLocationCommandValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.LocationCodeMaxLength);
        
        RuleFor(x => x.LocationTypeCode)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.LocationTypeCodeMaxLength);
        
        RuleFor(x => x.AddressLine1)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.AddressLineMaxLength);
        
        RuleFor(x => x.AddressLine2)
            .MaximumLength(LocationsConstants.AddressLineMaxLength)
            .When(x => !string.IsNullOrEmpty(x.AddressLine2));
        
        RuleFor(x => x.City)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.CityMaxLength);
        
        RuleFor(x => x.State)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.StateMaxLength);
        
        RuleFor(x => x.ZipCode)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.ZipCodeMaxLength);
        
        RuleFor(x => x.Country)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.CountryMaxLength);
    }
}