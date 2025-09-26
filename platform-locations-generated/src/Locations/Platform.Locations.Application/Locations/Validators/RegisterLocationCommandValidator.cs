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
        
        // Coordinate validation - both latitude and longitude must be provided together
        RuleFor(x => x.Latitude)
            .Must((command, latitude) => 
                (latitude.HasValue && command.Longitude.HasValue) || 
                (!latitude.HasValue && !command.Longitude.HasValue))
            .WithMessage("Both latitude and longitude must be provided together, or both must be null.");
        
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90 degrees.");
        
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180 degrees.");
        
        RuleFor(x => x.GeofenceRadius)
            .GreaterThan(0)
            .When(x => x.GeofenceRadius.HasValue)
            .WithMessage("Geofence radius must be greater than 0 when specified.");
        
        // Geofence radius can only be specified if coordinates are provided
        RuleFor(x => x.GeofenceRadius)
            .Must((command, radius) => 
                !radius.HasValue || (command.Latitude.HasValue && command.Longitude.HasValue))
            .WithMessage("Geofence radius can only be specified when coordinates are provided.");
    }
}