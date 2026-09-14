using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Catalog.ProductAttributes;

public sealed class GetProductAttributesHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductAttributeValueRepository
        _attributeRepository;

    private readonly ITenantCommerceVerticalRepository
        _verticalRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetProductAttributesHandler(
        IProductRepository productRepository,
        IProductAttributeValueRepository attributeRepository,
        ITenantCommerceVerticalRepository verticalRepository,
        ICurrentTenant currentTenant)
    {
        _productRepository =
            productRepository;

        _attributeRepository =
            attributeRepository;

        _verticalRepository =
            verticalRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<ProductAttributesResult?> HandleAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        EnsureTenant();

        if (productId.IsEmpty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(productId));
        }

        var product =
            await _productRepository.GetByIdAsync(
                productId,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var verticalType =
            await GetPrimaryVerticalAsync(
                cancellationToken);

        var schema =
            ProductAttributeSchemaCatalog.Get(
                verticalType);

        var values =
            await _attributeRepository.GetByProductIdAsync(
                productId,
                cancellationToken);

        return Map(
            productId,
            schema,
            values);
    }

    internal static ProductAttributesResult Map(
        ProductId productId,
        ProductAttributeSchema schema,
        IReadOnlyCollection<ProductAttributeValue> values)
    {
        var valuesByKey =
            values.ToDictionary(
                value =>
                    value.Key,
                value =>
                    value.Value,
                StringComparer.Ordinal);

        var definition =
            CommerceVerticalCatalog.Get(
                schema.VerticalType);

        return new ProductAttributesResult(
            productId.Value,
            schema.VerticalType.ToString(),
            definition.Code,
            schema.Attributes
                .Select(
                    attribute =>
                        new ProductAttributeFieldResult(
                            attribute.Key,
                            attribute.Label,
                            attribute.ValueType.ToString(),
                            attribute.AllowedValues,
                            valuesByKey.GetValueOrDefault(
                                attribute.Key)))
                .ToArray());
    }

    private async Task<CommerceVerticalType>
        GetPrimaryVerticalAsync(
            CancellationToken cancellationToken)
    {
        var verticals =
            await _verticalRepository.GetAllAsync(
                cancellationToken);

        var primary =
            verticals.SingleOrDefault(
                vertical =>
                    vertical.IsEnabled &&
                    vertical.IsPrimary);

        if (primary is null)
        {
            throw new ProductAttributesVerticalNotConfiguredException();
        }

        return primary.VerticalType;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to query product attributes.");
        }
    }
}