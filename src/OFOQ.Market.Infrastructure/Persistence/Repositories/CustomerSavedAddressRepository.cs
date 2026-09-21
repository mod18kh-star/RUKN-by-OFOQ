using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class CustomerSavedAddressRepository
    : ICustomerSavedAddressRepository
{
    private readonly MarketDbContext _db;

    public CustomerSavedAddressRepository(
        MarketDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerSavedAddress>>
        GetActiveForUserAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        return await _db.CustomerSavedAddresses
            .Where(x =>
                x.UserId == userId &&
                x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x =>
                x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerSavedAddress?>
        GetByIdForUserAsync(
            Guid addressId,
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        return _db.CustomerSavedAddresses
            .SingleOrDefaultAsync(
                x =>
                    x.Id == addressId &&
                    x.UserId == userId &&
                    x.IsActive,
                cancellationToken);
    }

    public async Task AddAsync(
        CustomerSavedAddress address,
        CancellationToken cancellationToken = default)
    {
        await _db.CustomerSavedAddresses.AddAsync(
            address,
            cancellationToken);
    }
}