using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Content;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class NavigationItemRepository : INavigationItemRepository
{
    private readonly MarketDbContext _db;
    public NavigationItemRepository(MarketDbContext db)=>_db=db;
    public Task<NavigationItem?> GetByIdAsync(NavigationItemId id,CancellationToken cancellationToken=default)=>_db.NavigationItems.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken);
    public async Task<IReadOnlyList<NavigationItem>> GetAllAsync(CancellationToken cancellationToken=default)=>await _db.NavigationItems.OrderBy(x=>x.Location).ThenBy(x=>x.SortOrder).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyList<NavigationItem>> GetSiblingsAsync(NavigationLocation location,NavigationItemId? parentId,CancellationToken cancellationToken=default)=>await _db.NavigationItems.Where(x=>x.Location==location&&x.ParentItemId==parentId).OrderBy(x=>x.SortOrder).ToArrayAsync(cancellationToken);
    public Task AddAsync(NavigationItem item,CancellationToken cancellationToken=default)=>_db.NavigationItems.AddAsync(item,cancellationToken).AsTask();
    public void Remove(NavigationItem item)=>_db.NavigationItems.Remove(item);
}
