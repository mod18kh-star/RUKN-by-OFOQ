using OFOQ.Market.Domain.Content;
namespace OFOQ.Market.Application.Common.Persistence;
public interface INavigationItemRepository
{
    Task<NavigationItem?> GetByIdAsync(NavigationItemId id,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<NavigationItem>> GetAllAsync(CancellationToken cancellationToken=default);
    Task<IReadOnlyList<NavigationItem>> GetSiblingsAsync(NavigationLocation location,NavigationItemId? parentId,CancellationToken cancellationToken=default);
    Task AddAsync(NavigationItem item,CancellationToken cancellationToken=default);
    void Remove(NavigationItem item);
}
