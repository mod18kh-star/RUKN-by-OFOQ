using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Identity;
namespace OFOQ.Market.Application.Common.Persistence;
public interface IProductReviewRepository
{
 Task<ProductReview?> GetByIdAsync(ProductReviewId id,CancellationToken cancellationToken=default);
 Task<ProductReview?> GetByCustomerAndProductAsync(UserId customerUserId,ProductId productId,CancellationToken cancellationToken=default);
 Task<IReadOnlyList<ProductReview>> GetAllAsync(int take,CancellationToken cancellationToken=default);
 Task AddAsync(ProductReview review,CancellationToken cancellationToken=default);
}
