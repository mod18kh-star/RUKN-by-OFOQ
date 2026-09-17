using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Identity;
namespace OFOQ.Market.Infrastructure.Persistence.Repositories;
internal sealed class ProductReviewRepository:IProductReviewRepository
{
 private readonly MarketDbContext _db; public ProductReviewRepository(MarketDbContext db)=>_db=db;
 public Task<ProductReview?> GetByIdAsync(ProductReviewId id,CancellationToken cancellationToken=default)=>_db.ProductReviews.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken);
 public Task<ProductReview?> GetByCustomerAndProductAsync(UserId customerUserId,ProductId productId,CancellationToken cancellationToken=default)=>_db.ProductReviews.SingleOrDefaultAsync(x=>x.CustomerUserId==customerUserId&&x.ProductId==productId,cancellationToken);
 public async Task<IReadOnlyList<ProductReview>> GetAllAsync(int take,CancellationToken cancellationToken=default)=>await _db.ProductReviews.OrderByDescending(x=>x.CreatedAtUtc).Take(take).ToArrayAsync(cancellationToken);
 public Task AddAsync(ProductReview review,CancellationToken cancellationToken=default)=>_db.ProductReviews.AddAsync(review,cancellationToken).AsTask();
}
