using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts.AddItem;

public sealed class AddToCartHandler
{
    private readonly ICartRepository
        _cartRepository;

    private readonly IProductRepository
        _productRepository;

    private readonly IProductVariantRepository
        _productVariantRepository;

    private readonly IProductOptionRepository
        _productOptionRepository;

    private readonly IProductVariantOptionValueRepository
        _variantOptionValueRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public AddToCartHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IProductVariantRepository productVariantRepository,
        IProductOptionRepository productOptionRepository,
        IProductVariantOptionValueRepository variantOptionValueRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _cartRepository =
            cartRepository;

        _productRepository =
            productRepository;

        _productVariantRepository =
            productVariantRepository;

        _productOptionRepository =
            productOptionRepository;

        _variantOptionValueRepository =
            variantOptionValueRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CartResult> HandleAsync(
        AddToCartCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        if (command.CustomerUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(command));
        }

        if (command.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Quantity must be greater than zero.");
        }

        if (command.Quantity >
            CartItem.MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                $"Quantity cannot exceed {CartItem.MaximumQuantity}.");
        }

        var product =
            await _productRepository.GetByIdAsync(
                command.ProductId,
                cancellationToken);

        if (product is null ||
            product.Status != ProductStatus.Published ||
            !product.IsVisible)
        {
            throw new CartProductNotAvailableException();
        }

        var variant =
            await _productVariantRepository.GetByIdAsync(
                command.ProductVariantId,
                cancellationToken);

        if (variant is null ||
            variant.ProductId != product.Id ||
            !variant.IsEnabled)
        {
            throw new CartVariantNotAvailableException();
        }

        await EnsureStructuredVariantIsValidAsync(
            product.Id,
            variant.Id,
            cancellationToken);

        var cart =
            await _cartRepository
                .GetActiveByCustomerUserIdAsync(
                    command.CustomerUserId,
                    cancellationToken);

        var existingQuantity =
            cart?.Items
                .FirstOrDefault(
                    item =>
                        item.ProductVariantId ==
                        variant.Id)
                ?.Quantity
            ?? 0;

        int requestedTotalQuantity;

        try
        {
            requestedTotalQuantity =
                checked(
                    existingQuantity +
                    command.Quantity);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                $"Cart item quantity cannot exceed {CartItem.MaximumQuantity}.");
        }

        if (requestedTotalQuantity >
            CartItem.MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                $"Cart item quantity cannot exceed {CartItem.MaximumQuantity}.");
        }

        EnsureStockAvailable(
            variant,
            requestedTotalQuantity);

        var now =
            _timeProvider.GetUtcNow();

        if (cart is null)
        {
            cart =
                Cart.Create(
                    _currentTenant.TenantId.Value,
                    command.CustomerUserId,
                    now,
                    command.CustomerUserId.Value);

            await _cartRepository.AddAsync(
                cart,
                cancellationToken);
        }

        var unitPrice =
            variant.PriceOverride
            ?? product.Price;

        cart.AddItem(
            product.Id,
            variant.Id,
            unitPrice,
            command.Quantity,
            now,
            command.CustomerUserId.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return MapCart(
            cart);
    }

    private async Task EnsureStructuredVariantIsValidAsync(
        ProductId productId,
        ProductVariantId variantId,
        CancellationToken cancellationToken)
    {
        var options =
            await _productOptionRepository
                .GetByProductIdAsync(
                    productId,
                    cancellationToken);

        /*
         * A simple product has no structured options,
         * therefore its default Variant is valid.
         */
        if (options.Count == 0)
        {
            return;
        }

        var assignments =
            await _variantOptionValueRepository
                .GetByVariantIdAsync(
                    variantId,
                    cancellationToken);

        /*
         * A structured product must select exactly
         * one value for every active product option.
         *
         * This intentionally rejects the old placeholder
         * Default Variant, which has zero selections.
         */
        if (assignments.Count !=
            options.Count)
        {
            throw new StructuredVariantRequiredException();
        }

        var expectedOptionIds =
            options
                .Select(
                    option =>
                        option.Id)
                .ToHashSet();

        var selectedOptionIds =
            assignments
                .Select(
                    assignment =>
                        assignment.ProductOptionId)
                .ToHashSet();

        if (selectedOptionIds.Count !=
            expectedOptionIds.Count ||
            !selectedOptionIds.SetEquals(
                expectedOptionIds))
        {
            throw new StructuredVariantRequiredException();
        }
    }

    private static void EnsureStockAvailable(
        ProductVariant variant,
        int requestedTotalQuantity)
    {
        var inventory =
            variant.Inventory;

        if (!inventory.TrackInventory)
        {
            return;
        }

        if (inventory.ContinueSellingWhenOutOfStock)
        {
            return;
        }

        if (requestedTotalQuantity >
            inventory.Quantity)
        {
            throw new CartInsufficientStockException();
        }
    }

    private static CartResult MapCart(
        Cart cart)
    {
        var items =
            cart.Items
                .Select(
                    item =>
                        new CartItemResult(
                            item.Id,
                            item.ProductId,
                            item.ProductVariantId,
                            item.UnitPrice.Amount,
                            item.UnitPrice.Currency.Value,
                            item.Quantity,
                            item.LineTotal))
                .ToArray();

        return new CartResult(
            cart.Id,
            cart.Currency?.Value,
            cart.TotalQuantity,
            cart.TotalAmount,
            items);
    }
}