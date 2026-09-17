using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.ProductContentBlocks;

public sealed class GetProductContentBlocksHandler
{
    private readonly IProductRepository _products;
    private readonly IProductContentBlockRepository _blocks;

    public GetProductContentBlocksHandler(
        IProductRepository products,
        IProductContentBlockRepository blocks)
    {
        _products = products;
        _blocks = blocks;
    }

    public async Task<ProductContentBlocksResult?> HandleAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        if (await _products.GetByIdAsync(productId, cancellationToken) is null)
            return null;

        var blocks =
            await _blocks.GetByProductIdAsync(
                productId,
                cancellationToken);

        return Map(productId, blocks);
    }

    internal static ProductContentBlocksResult Map(
        ProductId productId,
        IReadOnlyCollection<ProductContentBlock> blocks)
    {
        return new ProductContentBlocksResult(
            productId.Value,
            blocks
                .OrderBy(block => block.SortOrder)
                .Select(block =>
                    new ProductContentBlockResult(
                        block.Id.Value,
                        block.Type.ToString(),
                        block.Title,
                        block.Body,
                        block.MediaUrl,
                        block.SortOrder,
                        block.IsVisible))
                .ToArray());
    }
}
