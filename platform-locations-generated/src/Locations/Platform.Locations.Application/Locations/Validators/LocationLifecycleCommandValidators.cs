using FluentValidation;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Domain;

namespace Platform.Locations.Application.Locations.Validators;

public class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.LocationCodeMaxLength);
    }
}

public class ActivateLocationCommandValidator : AbstractValidator<ActivateLocationCommand>
{
    public ActivateLocationCommandValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.LocationCodeMaxLength);
    }
}

public class DeactivateLocationCommandValidator : AbstractValidator<DeactivateLocationCommand>
{
    public DeactivateLocationCommandValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty().NotNull()
            .MaximumLength(LocationsConstants.LocationCodeMaxLength);
    }
}