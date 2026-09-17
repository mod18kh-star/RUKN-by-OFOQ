using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ICustomerAddressRepository
{
    Task<IReadOnlyList<CustomerAddress>> GetForUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<CustomerAddress?> GetByIdForUserAsync(CustomerAddressId addressId, UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default);
}
