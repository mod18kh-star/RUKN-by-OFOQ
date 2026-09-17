using OFOQ.Market.Domain.Content;
namespace OFOQ.Market.Application.Common.Persistence;
public interface IContentPageRepository
{
    Task<ContentPage?> GetByIdAsync(ContentPageId id,CancellationToken cancellationToken=default);
    Task<ContentPage?> GetBySlugAsync(string slug,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<ContentPage>> GetAllAsync(CancellationToken cancellationToken=default);
    Task<bool> SlugExistsAsync(string slug,ContentPageId? excludingId=null,CancellationToken cancellationToken=default);
    Task AddAsync(ContentPage page,CancellationToken cancellationToken=default);
    void Remove(ContentPage page);
}
