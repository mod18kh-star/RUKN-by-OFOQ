using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Options.GetOptions;

public sealed class GetProductOptionsHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductOptionRepository
        _optionRepository;

    private readonly IProductOptionValueRepository
        _valueRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetProductOptionsHandler(
        IProductRepository productRepository,
        IProductOptionRepository optionRepository,
        IProductOptionValueRepository valueRepository,
        ICurrentTenant currentTenant)
    {
        _productRepository =
            productRepository;

        _optionRepository =
            optionRepository;

        _valueRepository =
            valueRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<IReadOnlyList<ProductOptionResult>>
        HandleAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        EnsureTenant();

        var product =
            await _productRepository
                .GetByIdAsync(
                    productId,
                    cancellationToken);

        if (product is null)
        {
            throw new ProductOptionProductNotFoundException(
                productId);
        }

        var options =
            await _optionRepository
                .GetByProductIdAsync(
                    product.Id,
                    cancellationToken);

        var results =
            new List<ProductOptionResult>(
                options.Count);

        foreach (var option in options)
        {
            var values =
                await _valueRepository
                    .GetByOptionIdAsync(
                        option.Id,
                        cancellationToken);

            results.Add(
                new ProductOptionResult(
                    option.Id,
                    option.Name,
                    option.SortOrder,
                    values
                        .Select(
                            value =>
                                new ProductOptionValueResult(
                                    value.Id,
                                    value.Value,
                                    value.SortOrder))
                        .ToArray()));
        }

        return results;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to read product options.");
        }
    }
}