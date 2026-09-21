using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Catalog.ProductAttributes;

public sealed class SetProductAttributesHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductAttributeValueRepository
        _attributeRepository;

    private readonly ITenantCommerceVerticalRepository
        _verticalRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public SetProductAttributesHandler(
        IProductRepository productRepository,
        IProductAttributeValueRepository attributeRepository,
        ITenantCommerceVerticalRepository verticalRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository =
            productRepository;

        _attributeRepository =
            attributeRepository;

        _verticalRepository =
            verticalRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<ProductAttributesResult?> HandleAsync(
        SetProductAttributesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenant();

        if (command.ProductId.IsEmpty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(command));
        }

        if (command.ActorUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(command));
        }

        ArgumentNullException.ThrowIfNull(
            command.Values);

        var product =
            await _productRepository.GetByIdAsync(
                command.ProductId,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        ProductAttributeSchema schema;

        if (string.Equals(
                product.VerticalCode,
                "general",
                StringComparison.OrdinalIgnoreCase))
        {
            // Legacy products created before Product.VerticalCode existed.
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

            schema =
                ProductAttributeSchemaCatalog.Get(
                    primary.VerticalType);
        }
        else
        {
            schema =
                ProductAttributeSchemaCatalog.GetByCode(
                    product.VerticalCode);
        }

        var normalizedInputs =
            NormalizeInputs(
                schema,
                command.Values);

        var existing =
            await _attributeRepository.GetByProductIdAsync(
                command.ProductId,
                cancellationToken);

        var existingByKey =
            existing.ToDictionary(
                value =>
                    value.Key,
                StringComparer.Ordinal);

        var now =
            _timeProvider.GetUtcNow();

        foreach (var current in
                 existing)
        {
            if (!normalizedInputs.ContainsKey(
                    current.Key))
            {
                _attributeRepository.Remove(
                    current);
            }
        }

        foreach (var pair in
                 normalizedInputs)
        {
            if (existingByKey.TryGetValue(
                    pair.Key,
                    out var current))
            {
                current.ChangeValue(
                    pair.Value,
                    now,
                    command.ActorUserId.Value);

                continue;
            }

            await _attributeRepository.AddAsync(
                ProductAttributeValue.Create(
                    _currentTenant.TenantId!.Value,
                    command.ProductId,
                    pair.Key,
                    pair.Value,
                    now,
                    command.ActorUserId.Value),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var saved =
            await _attributeRepository.GetByProductIdAsync(
                command.ProductId,
                cancellationToken);

        return GetProductAttributesHandler.Map(
            command.ProductId,
            schema,
            saved);
    }

    private static IReadOnlyDictionary<string, string>
        NormalizeInputs(
            ProductAttributeSchema schema,
            IReadOnlyCollection<ProductAttributeInput> inputs)
    {
        var result =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        foreach (var input in
                 inputs)
        {
            if (string.IsNullOrWhiteSpace(
                    input.Key))
            {
                throw new ArgumentException(
                    "Product attribute key is required.");
            }

            var key =
                input.Key.Trim()
                    .ToLowerInvariant();

            if (result.ContainsKey(
                    key))
            {
                throw new ArgumentException(
                    $"Duplicate product attribute key '{key}'.");
            }

            var definition =
                schema.Find(
                    key);

            if (definition is null)
            {
                throw new ArgumentException(
                    $"Product attribute '{key}' is not supported by the product vertical.");
            }

            var normalized =
                ProductAttributeValueNormalizer.Normalize(
                    definition,
                    input.Value);

            if (normalized is not null)
            {
                result.Add(
                    key,
                    normalized);
            }
        }

        return result;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to update product attributes.");
        }
    }
}