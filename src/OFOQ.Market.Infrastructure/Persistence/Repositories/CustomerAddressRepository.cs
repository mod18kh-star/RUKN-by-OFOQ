using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class CustomerAddressRepository : ICustomerAddressRepository
{
    private readonly MarketDbContext _db;
    public CustomerAddressRepository(MarketDbContext db) => _db = db;
    public async Task<IReadOnlyList<CustomerAddress>> GetForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => await _db.CustomerAddresses.Where(x => x.UserId == userId).OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).ToListAsync(cancellationToken);
    public Task<CustomerAddress?> GetByIdForUserAsync(CustomerAddressId addressId, UserId userId, CancellationToken cancellationToken = default)
        => _db.CustomerAddresses.SingleOrDefaultAsync(x => x.Id == addressId && x.UserId == userId, cancellationToken);
    public Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default)
        => _db.CustomerAddresses.AddAsync(address, cancellationToken).AsTask();
}
