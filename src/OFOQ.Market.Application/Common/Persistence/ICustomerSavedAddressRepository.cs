using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ICustomerSavedAddressRepository
{
    Task<IReadOnlyList<CustomerSavedAddress>> GetActiveForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<CustomerSavedAddress?> GetByIdForUserAsync(
        Guid addressId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerSavedAddress address,
        CancellationToken cancellationToken = default);
}