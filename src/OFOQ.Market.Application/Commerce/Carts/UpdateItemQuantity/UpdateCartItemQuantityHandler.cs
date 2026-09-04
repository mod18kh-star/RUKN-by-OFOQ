using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts.UpdateItemQuantity;

public sealed class UpdateCartItemQuantityHandler
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

    public UpdateCartItemQuantityHandler(
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
        UpdateCartItemQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenantContext();

        if (command.CustomerUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(command));
        }

        if (command.CartItemId.IsEmpty)
        {
            throw new ArgumentException(
                "Cart item ID cannot be empty.",
                nameof(command));
        }

        if (command.Quantity <= 0 ||
            command.Quantity >
            CartItem.MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                $"Quantity must be between 1 and {CartItem.MaximumQuantity}.");
        }

        var cart =
            await _cartRepository
                .GetActiveByCustomerUserIdAsync(
                    command.CustomerUserId,
                    cancellationToken)
            ?? throw new CartNotFoundException();

        var item =
            cart.Items.FirstOrDefault(
                candidate =>
                    candidate.Id ==
                    command.CartItemId)
            ?? throw new CartItemNotFoundException();

        /*
         * Reducing quantity does not require a stock check.
         *
         * Increasing quantity does, and we also refresh
         * the authoritative server-side price at that point.
         */
        if (command.Quantity >
            item.Quantity)
        {
            var product =
                await _productRepository
                    .GetByIdAsync(
                        item.ProductId,
                        cancellationToken);

            if (product is null ||
                product.Status !=
                ProductStatus.Published ||
                !product.IsVisible)
            {
                throw new CartProductNotAvailableException();
            }

            var variant =
                await _productVariantRepository
                    .GetByIdAsync(
                        item.ProductVariantId,
                        cancellationToken);

            if (variant is null ||
                variant.ProductId !=
                product.Id ||
                !variant.IsEnabled)
            {
                throw new CartVariantNotAvailableException();
            }

            await EnsureStructuredVariantIsValidAsync(
                product.Id,
                variant.Id,
                cancellationToken);

            EnsureStockAvailable(
                variant,
                command.Quantity);

            var unitPrice =
                variant.PriceOverride
                ?? product.Price;

            cart.RefreshItemPrice(
                item.Id,
                unitPrice,
                _timeProvider.GetUtcNow(),
                command.CustomerUserId.Value);
        }

        var now =
            _timeProvider.GetUtcNow();

        cart.ChangeItemQuantity(
            command.CartItemId,
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

        if (options.Count == 0)
        {
            return;
        }

        var assignments =
            await _variantOptionValueRepository
                .GetByVariantIdAsync(
                    variantId,
                    cancellationToken);

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
        int requestedQuantity)
    {
        var inventory =
            variant.Inventory;

        if (!inventory.TrackInventory ||
            inventory.ContinueSellingWhenOutOfStock)
        {
            return;
        }

        if (requestedQuantity >
            inventory.Quantity)
        {
            throw new CartInsufficientStockException();
        }
    }

    private void EnsureTenantContext()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }
    }

    private static CartResult MapCart(
        Cart cart)
    {
        return new CartResult(
            cart.Id,
            cart.Currency?.Value,
            cart.TotalQuantity,
            cart.TotalAmount,
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
                .ToArray());
    }
}