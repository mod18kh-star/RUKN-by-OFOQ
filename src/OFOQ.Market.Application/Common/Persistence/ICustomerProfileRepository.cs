using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ICustomerProfileRepository
{
    Task<CustomerProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default);
}
