using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutHandler
{
    public const int MaximumIdempotencyKeyLength =
        128;

    private readonly ICartRepository
        _cartRepository;

    private readonly ICheckoutLockRepository
        _checkoutLockRepository;

    private readonly IOrderRepository
        _orderRepository;

    private readonly IProductRepository
        _productRepository;

    private readonly IProductOptionRepository
        _productOptionRepository;

    private readonly IProductVariantOptionValueRepository
        _variantOptionValueRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITransactionExecutor
        _transactionExecutor;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CheckoutHandler(
        ICartRepository cartRepository,
        ICheckoutLockRepository checkoutLockRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IProductOptionRepository productOptionRepository,
        IProductVariantOptionValueRepository variantOptionValueRepository,
        ICurrentTenant currentTenant,
        ITransactionExecutor transactionExecutor,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _cartRepository =
            cartRepository;

        _checkoutLockRepository =
            checkoutLockRepository;

        _orderRepository =
            orderRepository;

        _productRepository =
            productRepository;

        _productOptionRepository =
            productOptionRepository;

        _variantOptionValueRepository =
            variantOptionValueRepository;

        _currentTenant =
            currentTenant;

        _transactionExecutor =
            transactionExecutor;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CheckoutResult> HandleAsync(
        CheckoutCommand command,
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

        var idempotencyKey =
            NormalizeIdempotencyKey(
                command.IdempotencyKey);

        /*
         * Fast replay path.
         *
         * A completed checkout owns this key permanently for
         * this tenant/customer. If a new active Cart already
         * exists, reusing the same key would represent a
         * different checkout request and must be rejected.
         */
        var completedOrder =
            await _orderRepository
                .GetByCheckoutIdempotencyKeyAsync(
                    command.CustomerUserId,
                    idempotencyKey,
                    cancellationToken);

        if (completedOrder is not null)
        {
            var activeCart =
                await _cartRepository
                    .GetActiveByCustomerUserIdAsync(
                        command.CustomerUserId,
                        cancellationToken);

            if (activeCart is not null)
            {
                throw new CheckoutIdempotencyConflictException();
            }

            return MapOrder(
                completedOrder,
                isIdempotentReplay: true);
        }

        return await _transactionExecutor
            .ExecuteAsync(
                async transactionCancellationToken =>
                {
                    var cart =
                        await _checkoutLockRepository
                            .GetActiveCartForUpdateAsync(
                                command.CustomerUserId,
                                transactionCancellationToken);

                    if (cart is null)
                    {
                        /*
                         * A concurrent request may have owned the
                         * same Cart lock, completed checkout, and
                         * converted the Cart while this request was
                         * waiting. Re-check the key only after the
                         * lock wait has resolved.
                         */
                        var replayOrder =
                            await _orderRepository
                                .GetByCheckoutIdempotencyKeyAsync(
                                    command.CustomerUserId,
                                    idempotencyKey,
                                    transactionCancellationToken);

                        if (replayOrder is not null)
                        {
                            return MapOrder(
                                replayOrder,
                                isIdempotentReplay: true);
                        }

                        throw new CheckoutCartNotFoundException();
                    }

                    var existingOrder =
                        await _orderRepository
                            .GetByCheckoutIdempotencyKeyAsync(
                                command.CustomerUserId,
                                idempotencyKey,
                                transactionCancellationToken);

                    if (existingOrder is not null)
                    {
                        if (existingOrder.SourceCartId !=
                            cart.Id)
                        {
                            throw new CheckoutIdempotencyConflictException();
                        }

                        return MapOrder(
                            existingOrder,
                            isIdempotentReplay: true);
                    }

                    if (cart.Items.Count == 0)
                    {
                        throw new CheckoutCartEmptyException();
                    }

                    var variantIds =
                        cart.Items
                            .Select(
                                item =>
                                    item.ProductVariantId)
                            .Distinct()
                            .ToArray();

                    var lockedVariants =
                        await _checkoutLockRepository
                            .GetVariantsForUpdateAsync(
                                variantIds,
                                transactionCancellationToken);

                    if (lockedVariants.Count !=
                        variantIds.Length)
                    {
                        throw new CheckoutVariantNotAvailableException();
                    }

                    var variantsById =
                        lockedVariants.ToDictionary(
                            variant =>
                                variant.Id);

                    var productsById =
                        new Dictionary<
                            ProductId,
                            Product>();

                    foreach (var productId in
                             cart.Items
                                 .Select(
                                     item =>
                                         item.ProductId)
                                 .Distinct())
                    {
                        var product =
                            await _productRepository
                                .GetByIdAsync(
                                    productId,
                                    transactionCancellationToken);

                        if (product is null ||
                            product.Status !=
                            ProductStatus.Published ||
                            !product.IsVisible)
                        {
                            throw new CheckoutProductNotAvailableException();
                        }

                        productsById.Add(
                            product.Id,
                            product);
                    }

                    var snapshots =
                        new List<OrderItemSnapshot>(
                            cart.Items.Count);

                    CurrencyCode? orderCurrency =
                        null;

                    foreach (var item in
                             cart.Items)
                    {
                        var product =
                            productsById[
                                item.ProductId];

                        if (!variantsById.TryGetValue(
                                item.ProductVariantId,
                                out var variant) ||
                            variant.ProductId !=
                            product.Id ||
                            !variant.IsEnabled)
                        {
                            throw new CheckoutVariantNotAvailableException();
                        }

                        await EnsureStructuredVariantIsValidAsync(
                            product.Id,
                            variant.Id,
                            transactionCancellationToken);

                        EnsureStockAvailable(
                            variant,
                            item.Quantity);

                        var authoritativePrice =
                            variant.PriceOverride
                            ?? product.Price;

                        if (!orderCurrency.HasValue)
                        {
                            orderCurrency =
                                authoritativePrice.Currency;
                        }
                        else if (orderCurrency.Value !=
                            authoritativePrice.Currency)
                        {
                            throw new CheckoutCurrencyChangedException();
                        }

                        snapshots.Add(
                            new OrderItemSnapshot(
                                product.Id,
                                variant.Id,
                                product.Name,
                                variant.Name,
                                variant.Sku.Value,
                                authoritativePrice,
                                item.Quantity));
                    }

                    if (!orderCurrency.HasValue)
                    {
                        throw new CheckoutCartEmptyException();
                    }

                    var now =
                        _timeProvider.GetUtcNow();

                    var order =
                        Order.Create(
                            _currentTenant.TenantId!.Value,
                            command.CustomerUserId,
                            cart.Id,
                            orderCurrency.Value,
                            snapshots,
                            now,
                            command.CustomerUserId.Value);

                    foreach (var item in
                             cart.Items)
                    {
                        var variant =
                            variantsById[
                                item.ProductVariantId];

                        var quantityBefore =
                            variant.Inventory.Quantity;

                        variant.DecreaseStock(
                            item.Quantity,
                            now,
                            command.CustomerUserId.Value);

                        var quantityAfter =
                            variant.Inventory.Quantity;

                        order.RecordCheckoutInventoryDeduction(
                            item.ProductId,
                            variant.Id,
                            quantityBefore,
                            quantityAfter,
                            now,
                            command.CustomerUserId.Value);
                    }

                    await _orderRepository
                        .AddAsync(
                            order,
                            idempotencyKey,
                            transactionCancellationToken);

                    cart.MarkConverted(
                        now,
                        command.CustomerUserId.Value);

                    await _unitOfWork
                        .SaveChangesAsync(
                            transactionCancellationToken);

                    return MapOrder(
                        order,
                        isIdempotentReplay: false);
                },
                cancellationToken);
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
            throw new CheckoutStructuredVariantInvalidException();
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
            throw new CheckoutStructuredVariantInvalidException();
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
            throw new CheckoutInsufficientStockException();
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

    private static string NormalizeIdempotencyKey(
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency-Key is required.",
                nameof(idempotencyKey));
        }

        var normalized =
            idempotencyKey.Trim();

        if (normalized.Length >
            MaximumIdempotencyKeyLength)
        {
            throw new ArgumentException(
                $"Idempotency-Key cannot exceed {MaximumIdempotencyKeyLength} characters.",
                nameof(idempotencyKey));
        }

        if (normalized.Any(
                char.IsControl))
        {
            throw new ArgumentException(
                "Idempotency-Key cannot contain control characters.",
                nameof(idempotencyKey));
        }

        return normalized;
    }

    private static CheckoutResult MapOrder(
        Order order,
        bool isIdempotentReplay)
    {
        return new CheckoutResult(
            order.Id,
            order.SourceCartId,
            order.Status.ToString(),
            order.Currency.Value,
            order.TotalQuantity,
            order.TotalAmount,
            order.CreatedAtUtc,
            isIdempotentReplay,
            order.Items
                .Select(
                    item =>
                        new CheckoutItemResult(
                            item.Id,
                            item.ProductId,
                            item.ProductVariantId,
                            item.ProductName,
                            item.VariantName,
                            item.Sku,
                            item.UnitPrice.Amount,
                            item.UnitPrice.Currency.Value,
                            item.Quantity,
                            item.LineTotal))
                .ToArray());
    }
}
