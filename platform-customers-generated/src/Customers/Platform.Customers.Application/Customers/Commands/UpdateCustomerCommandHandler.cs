using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.IntegrationEvents;
using Platform.Customers.Domain.Customers;
using Platform.Customers.Application.Customers.Dtos;
using Platform.Customers.Application.IntegrationEvents;
using FluentValidation;
using AutoMapper;

namespace Platform.Customers.Application.Customers.Commands;

public class UpdateCustomerCommandHandler : ICommandHandler<UpdateCustomerCommand, CustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<UpdateCustomerCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IValidator<UpdateCustomerCommand> validator,
        IMapper mapper,
        IIntegrationEventPublisher eventPublisher)
    {
        _customerRepository = customerRepository;
        _validator = validator;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<CustomerResponse> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);

        // Find existing customer
        var customer = await _customerRepository.GetByCustomerCodeAsync(request.CustomerCode, cancellationToken);
        if (customer == null)
        {
            throw new CustomerNotFoundException(request.CustomerCode);
        }

        // Application-level business rules validation
        await ValidateBusinessRules(request, customer.Id, cancellationToken);

        // Update customer using domain methods
        customer.UpdateCompanyName(request.CompanyName);
        customer.UpdateContactInfo(request.ContactFirstName, request.ContactLastName, request.Email, request.PhoneNumber);
        customer.UpdateAddress(request.AddressLine1, request.AddressLine2, request.City, request.State, request.PostalCode, request.Country);

        // Platform.Shared automatically handles:
        // - Setting audit fields (UpdatedAt, UpdatedBy)
        // - Database transaction management (via TransactionBehavior)
        await _customerRepository.UpdateAsync(customer, cancellationToken);

        // Publish integration event
        _eventPublisher.SaveIntegrationEvent(
            new CustomerUpdatedIntegrationEvent(
                customer.CustomerCode,
                customer.CompanyName,
                customer.ContactInfo.Email,
                customer.UpdatedAt?.DateTime ?? DateTime.UtcNow));

        return _mapper.Map<CustomerResponse>(customer);
    }

    private async Task ValidateBusinessRules(UpdateCustomerCommand request, Guid currentCustomerId, CancellationToken cancellationToken)
    {
        // Check email uniqueness (excluding current customer)
        var existingByEmail = await _customerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingByEmail != null && existingByEmail.Id != currentCustomerId)
        {
            throw new EmailAlreadyExistsException(request.Email);
        }
    }
}