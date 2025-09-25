using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.IntegrationEvents;
using Platform.Customers.Domain.Customers;
using Platform.Customers.Application.Customers.Dtos;
using Platform.Customers.Application.IntegrationEvents;
using FluentValidation;
using AutoMapper;

namespace Platform.Customers.Application.Customers.Commands;

public class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, CustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<CreateCustomerCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IValidator<CreateCustomerCommand> validator,
        IMapper mapper,
        IIntegrationEventPublisher eventPublisher)
    {
        _customerRepository = customerRepository;
        _validator = validator;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<CustomerResponse> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);
        
        // Application-level business rules validation
        await ValidateBusinessRules(request, cancellationToken);
        
        // Create domain entity with business logic in aggregate root
        var customer = Customer.Create(
            request.CustomerCode,
            request.CustomerType,
            request.CompanyName,
            request.ContactFirstName,
            request.ContactLastName,
            request.Email,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country);
        
        // Platform.Shared automatically handles:
        // - Setting audit fields (CreatedAt, CreatedBy)
        // - Setting product context from middleware
        // - Database transaction management (via TransactionBehavior)
        await _customerRepository.AddAsync(customer, cancellationToken);
        
        // Publish clean integration events (no product context in payload)
        // Product context automatically added to CloudEvents headers
        _eventPublisher.SaveIntegrationEvent(
            new CustomerCreatedIntegrationEvent(
                customer.CustomerCode,
                customer.CompanyName,
                customer.CustomerType,
                customer.ContactInfo.Email,
                customer.CreatedAt.DateTime));
        
        return _mapper.Map<CustomerResponse>(customer);
    }

    private async Task ValidateBusinessRules(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Repository automatically filters by product context - no manual filtering needed
        var existingCustomer = await _customerRepository.GetByCustomerCodeAsync(request.CustomerCode, cancellationToken);
        if (existingCustomer != null)
        {
            throw new CustomerAlreadyExistsException(request.CustomerCode);
        }

        // Check email uniqueness
        var existingByEmail = await _customerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingByEmail != null)
        {
            throw new EmailAlreadyExistsException(request.Email);
        }
    }
}