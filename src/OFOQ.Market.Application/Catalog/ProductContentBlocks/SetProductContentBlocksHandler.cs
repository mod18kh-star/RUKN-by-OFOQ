using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.ProductContentBlocks;

public sealed class SetProductContentBlocksHandler
{
    private const int MaximumBlocks = 20;

    private readonly IProductRepository _products;
    private readonly IProductContentBlockRepository _blocks;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetProductContentBlocksHandler(
        IProductRepository products,
        IProductContentBlockRepository blocks,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _products = products;
        _blocks = blocks;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ProductContentBlocksResult?> HandleAsync(
        SetProductContentBlocksCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Blocks);

        EnsureTenant();

        if (command.Blocks.Count > MaximumBlocks)
        {
            throw new ArgumentException(
                $"A product cannot have more than {MaximumBlocks} content blocks.");
        }

        if (await _products.GetByIdAsync(command.ProductId, cancellationToken) is null)
            return null;

        var existing =
            await _blocks.GetByProductIdAsync(
                command.ProductId,
                cancellationToken);

        if (existing.Count > 0)
            _blocks.RemoveRange(existing);

        var now = _timeProvider.GetUtcNow();

        var replacements =
            command.Blocks
                .Select((input, index) =>
                    ProductContentBlock.Create(
                        _currentTenant.TenantId!.Value,
                        command.ProductId,
                        input.Type,
                        input.Title,
                        input.Body,
                        input.MediaUrl,
                        index,
                        input.IsVisible,
                        now,
                        command.ActorUserId.Value))
                .ToArray();

        if (replacements.Length > 0)
            await _blocks.AddRangeAsync(replacements, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return GetProductContentBlocksHandler.Map(
            command.ProductId,
            replacements);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to manage product content.");
        }
    }
}
