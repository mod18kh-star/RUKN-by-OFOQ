using OFOQ.Market.Application.Catalog.Products.CreateProduct;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using CatalogInventory =
    OFOQ.Market.Domain.Catalog.Inventory;

namespace OFOQ.Market.Application.Catalog.Products.Variants.CreateVariant;

public sealed class CreateProductVariantHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductVariantRepository
        _variantRepository;

    private readonly IProductOptionRepository
        _optionRepository;

    private readonly IProductOptionValueRepository
        _valueRepository;

    private readonly IProductVariantOptionValueRepository
        _assignmentRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CreateProductVariantHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IProductOptionRepository optionRepository,
        IProductOptionValueRepository valueRepository,
        IProductVariantOptionValueRepository assignmentRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository =
            productRepository;

        _variantRepository =
            variantRepository;

        _optionRepository =
            optionRepository;

        _valueRepository =
            valueRepository;

        _assignmentRepository =
            assignmentRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<StructuredProductVariantResult>
        HandleAsync(
            CreateProductVariantCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var tenantId =
            GetRequiredTenantId();

        var product =
            await _productRepository
                .GetByIdAsync(
                    command.ProductId,
                    cancellationToken);

        if (product is null)
        {
            throw new ProductVariantProductNotFoundException(
                command.ProductId);
        }

        var sku =
            ProductSku.Create(
                command.Sku);

        if (await _variantRepository
            .SkuExistsAsync(
                sku,
                excludingVariantId: null,
                cancellationToken))
        {
            throw new ProductSkuAlreadyExistsException(
                sku.Value);
        }

        var options =
            await _optionRepository
                .GetByProductIdAsync(
                    product.Id,
                    cancellationToken);

        if (options.Count == 0)
        {
            throw new ProductVariantInvalidOptionSelectionException(
                "The product must have at least one option before a structured variant can be created.");
        }

        if (command.OptionValueIds is null)
        {
            throw new ProductVariantInvalidOptionSelectionException(
                "Option values are required.");
        }

        if (command.OptionValueIds.Count !=
            options.Count)
        {
            throw new ProductVariantInvalidOptionSelectionException(
                "Exactly one value must be selected for every product option.");
        }

        if (command.OptionValueIds
                .Distinct()
                .Count() !=
            command.OptionValueIds.Count)
        {
            throw new ProductVariantInvalidOptionSelectionException(
                "Duplicate option values are not allowed.");
        }

        var optionById =
            options.ToDictionary(
                option =>
                    option.Id);

        var selectedValues =
            new List<ProductOptionValue>(
                command.OptionValueIds.Count);

        var selectedOptionIds =
            new HashSet<ProductOptionId>();

        foreach (var valueId in command.OptionValueIds)
        {
            var value =
                await _valueRepository
                    .GetByIdAsync(
                        valueId,
                        cancellationToken);

            if (value is null ||
                value.ProductId != product.Id ||
                !optionById.ContainsKey(
                    value.ProductOptionId))
            {
                throw new ProductVariantOptionValueNotFoundException(
                    valueId);
            }

            if (!selectedOptionIds.Add(
                    value.ProductOptionId))
            {
                throw new ProductVariantInvalidOptionSelectionException(
                    "Only one value may be selected for each product option.");
            }

            selectedValues.Add(
                value);
        }

        if (selectedOptionIds.Count !=
            options.Count)
        {
            throw new ProductVariantInvalidOptionSelectionException(
                "Exactly one value must be selected for every product option.");
        }

        await EnsureCombinationIsUniqueAsync(
            product.Id,
            selectedValues,
            cancellationToken);

        Money? priceOverride =
            command.PriceOverride.HasValue
                ? Money.Create(
                    command.PriceOverride.Value,
                    product.Price.Currency)
                : null;

        var inventory =
            CatalogInventory.Create(
                command.TrackInventory,
                command.Quantity,
                command.LowStockThreshold,
                command.ContinueSellingWhenOutOfStock);

        var now =
            _timeProvider.GetUtcNow();

        var variant =
            ProductVariant.Create(
                tenantId,
                product.Id,
                command.Name,
                sku,
                product.Price.Currency,
                inventory,
                now,
                priceOverride:
                    priceOverride,
                isDefault:
                    false,
                createdByUserId:
                    command.ActorUserId.Value);

        await _variantRepository
            .AddAsync(
                variant,
                cancellationToken);

        foreach (var value in selectedValues)
        {
            var assignment =
                ProductVariantOptionValue.Create(
                    tenantId,
                    product.Id,
                    variant.Id,
                    value.ProductOptionId,
                    value.Id,
                    now,
                    command.ActorUserId.Value);

            await _assignmentRepository
                .AddAsync(
                    assignment,
                    cancellationToken);
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Map(
            variant,
            selectedValues,
            optionById);
    }

    private async Task EnsureCombinationIsUniqueAsync(
        ProductId productId,
        IReadOnlyList<ProductOptionValue> selectedValues,
        CancellationToken cancellationToken)
    {
        var targetValueIds =
            selectedValues
                .Select(
                    value =>
                        value.Id)
                .ToHashSet();

        var variants =
            await _variantRepository
                .GetByProductIdAsync(
                    productId,
                    cancellationToken);

        foreach (var existingVariant in variants)
        {
            var assignments =
                await _assignmentRepository
                    .GetByVariantIdAsync(
                        existingVariant.Id,
                        cancellationToken);

            if (assignments.Count !=
                targetValueIds.Count)
            {
                continue;
            }

            var existingValueIds =
                assignments
                    .Select(
                        assignment =>
                            assignment.ProductOptionValueId)
                    .ToHashSet();

            if (existingValueIds.SetEquals(
                    targetValueIds))
            {
                throw new ProductVariantCombinationAlreadyExistsException();
            }
        }
    }

    private OFOQ.Market.Domain.Tenancy.TenantId
        GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to create a product variant.");
        }

        return _currentTenant.TenantId.Value;
    }

    private static StructuredProductVariantResult Map(
        ProductVariant variant,
        IReadOnlyList<ProductOptionValue> values,
        IReadOnlyDictionary<ProductOptionId, ProductOption> options)
    {
        return new StructuredProductVariantResult(
            variant.Id,
            variant.Name,
            variant.Sku.Value,
            variant.IsDefault,
            variant.PriceOverride?.Amount,
            variant.PriceOverride?.Currency.Value,
            variant.Inventory.TrackInventory,
            variant.Inventory.Quantity,
            variant.Inventory.LowStockThreshold,
            variant.Inventory.ContinueSellingWhenOutOfStock,
            variant.Inventory.IsAvailableForSale,
            variant.IsEnabled,
            values
                .OrderBy(
                    value =>
                        options[
                            value.ProductOptionId]
                            .SortOrder)
                .Select(
                    value =>
                        new ProductVariantSelectionResult(
                            value.ProductOptionId,
                            options[
                                value.ProductOptionId]
                                .Name,
                            value.Id,
                            value.Value))
                .ToArray());
    }
}