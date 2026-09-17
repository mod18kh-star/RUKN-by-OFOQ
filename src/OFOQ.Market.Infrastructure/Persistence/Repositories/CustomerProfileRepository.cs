using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly MarketDbContext _db;
    public CustomerProfileRepository(MarketDbContext db) => _db = db;
    public Task<CustomerProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => _db.CustomerProfiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    public Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default)
        => _db.CustomerProfiles.AddAsync(profile, cancellationToken).AsTask();
}
