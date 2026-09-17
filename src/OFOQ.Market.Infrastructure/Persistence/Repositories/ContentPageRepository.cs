using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Content;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ContentPageRepository : IContentPageRepository
{
    private readonly MarketDbContext _db;
    public ContentPageRepository(MarketDbContext db)=>_db=db;
    public Task<ContentPage?> GetByIdAsync(ContentPageId id,CancellationToken cancellationToken=default)=>_db.ContentPages.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken);
    public Task<ContentPage?> GetBySlugAsync(string slug,CancellationToken cancellationToken=default)=>_db.ContentPages.SingleOrDefaultAsync(x=>x.Slug==slug,cancellationToken);
    public async Task<IReadOnlyList<ContentPage>> GetAllAsync(CancellationToken cancellationToken=default)=>await _db.ContentPages.OrderBy(x=>x.Title).ToArrayAsync(cancellationToken);
    public Task<bool> SlugExistsAsync(string slug,ContentPageId? excludingId=null,CancellationToken cancellationToken=default)=>_db.ContentPages.AnyAsync(x=>x.Slug==slug&&(!excludingId.HasValue||x.Id!=excludingId.Value),cancellationToken);
    public Task AddAsync(ContentPage page,CancellationToken cancellationToken=default)=>_db.ContentPages.AddAsync(page,cancellationToken).AsTask();
    public void Remove(ContentPage page)=>_db.ContentPages.Remove(page);
}
