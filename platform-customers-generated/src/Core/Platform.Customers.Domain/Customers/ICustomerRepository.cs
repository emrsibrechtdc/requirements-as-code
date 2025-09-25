using Platform.Shared.DataLayer.Repositories;

namespace Platform.Customers.Domain.Customers;

public interface ICustomerRepository : IRepository<Customer, Guid>
{
    Task<Customer?> GetByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByCustomerTypeAsync(string customerType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByCompanyNamePatternAsync(string companyNamePattern, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}