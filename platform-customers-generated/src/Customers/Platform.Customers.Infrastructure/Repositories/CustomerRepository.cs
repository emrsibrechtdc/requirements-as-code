using Microsoft.EntityFrameworkCore;
using Platform.Customers.Domain.Customers;
using Platform.Customers.Infrastructure.Data;
using Platform.Shared.EntityFrameworkCore;

namespace Platform.Customers.Infrastructure.Repositories;

public class CustomerRepository : EfCoreRepository<Customer, Guid, CustomersDbContext>, ICustomerRepository
{
    public CustomerRepository(CustomersDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Customer>()
            .FirstOrDefaultAsync(c => c.CustomerCode == customerCode, cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Customer>()
            .FirstOrDefaultAsync(c => c.ContactInfo.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByCustomerTypeAsync(string customerType, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Customer>()
            .Where(c => c.CustomerType == customerType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByCompanyNamePatternAsync(string companyNamePattern, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Customer>()
            .Where(c => c.CompanyName.Contains(companyNamePattern))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Customer>()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Customer>()
            .AnyAsync(c => c.CustomerCode == customerCode, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Customer>()
            .AnyAsync(c => c.ContactInfo.Email == email.ToLowerInvariant(), cancellationToken);
    }
}