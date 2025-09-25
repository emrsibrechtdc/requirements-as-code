using Platform.Shared.Cqrs.Mediatr;
using Platform.Customers.Domain.Customers;
using Platform.Customers.Application.Customers.Dtos;
using AutoMapper;

namespace Platform.Customers.Application.Customers.Queries;

public class GetCustomersQueryHandler : IQueryHandler<GetCustomersQuery, List<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<List<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        // Repository automatically filters by product context - no manual filtering needed
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        
        // Apply additional filters if specified
        if (!string.IsNullOrEmpty(request.CustomerCode))
        {
            customers = customers.Where(c => c.CustomerCode.Contains(request.CustomerCode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.CustomerType))
        {
            customers = customers.Where(c => c.CustomerType.Equals(request.CustomerType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.CompanyName))
        {
            customers = customers.Where(c => c.CompanyName.Contains(request.CompanyName, StringComparison.OrdinalIgnoreCase));
        }

        if (request.IsActive.HasValue)
        {
            customers = customers.Where(c => c.IsActive == request.IsActive.Value);
        }
        
        return _mapper.Map<List<CustomerDto>>(customers);
    }
}