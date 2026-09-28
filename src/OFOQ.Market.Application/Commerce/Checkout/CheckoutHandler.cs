using OFOQ.Market.Application.Common.Notifications;
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

    private readonly CheckoutPricingService?
        _checkoutPricingService;

    private readonly ITransactionalEmailQueue?
        _emailQueue;

    private readonly IStockHoldLedger? _holds;

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
        TimeProvider timeProvider,
        CheckoutPricingService? checkoutPricingService = null,
        ITransactionalEmailQueue? emailQueue = null,
        IStockHoldLedger? holds = null)
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

        _checkoutPricingService =
            checkoutPricingService;

        _emailQueue =
            emailQueue;
        _holds = holds;
    }

    // Used exclusively by the atomic manual-receipt submission endpoint. The caller
    // owns the EF/Npgsql transaction and its execution-strategy retry.
    public Task<CheckoutResult> HandleWithinExistingTransactionAsync(
        CheckoutCommand command,
        CancellationToken cancellationToken = default) =>
        HandleCoreAsync(command, cancellationToken, withinExistingTransaction: true);

    public Task<CheckoutResult> HandleAsync(
        CheckoutCommand command,
        CancellationToken cancellationToken = default) =>
        HandleCoreAsync(command, cancellationToken, withinExistingTransaction: false);

    private async Task<CheckoutResult> HandleCoreAsync(
        CheckoutCommand command,
        CancellationToken cancellationToken,
        bool withinExistingTransaction)
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

            // With deferred stock capture the same cart stays visible until payment.
            // Only replay when it is still the source cart of this pending order.
            if (activeCart is not null &&
                (activeCart.Id != completedOrder.SourceCartId ||
                 completedOrder.Status != OrderStatus.Pending))
            {
                throw new CheckoutIdempotencyConflictException();
            }

            return MapOrder(
                completedOrder,
                isIdempotentReplay: true);
        }

        async Task<CheckoutResult> ExecuteCheckoutAsync(CancellationToken transactionCancellationToken)
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

                    if (_holds?.Enabled == true)
                    {
                        // Reusing the current checkout, even with a freshly generated
                        // request key, must not create a second order for one cart.
                        var sourceOrder = await _orderRepository.GetBySourceCartIdAsync(
                            cart.Id, transactionCancellationToken);
                        if (sourceOrder is not null)
                        {
                            if (sourceOrder.Status != OrderStatus.Pending)
                                throw new CheckoutIdempotencyConflictException();
                            return MapOrder(sourceOrder, isIdempotentReplay: true);
                        }
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

                    var pricingContext =
                        _checkoutPricingService is null
                            ? CheckoutPricingContext.Empty
                            : await _checkoutPricingService.ResolveAsync(
                                command.CustomerUserId,
                                command.CustomerAddressId,
                                command.ShippingMethodId,
                                command.CouponCode,
                                snapshots
                                    .Select(snapshot =>
                                        new CheckoutPricingItem(
                                            snapshot.ProductId,
                                            snapshot.UnitPrice.Amount * snapshot.Quantity))
                                    .ToArray(),
                                orderCurrency.Value,
                                transactionCancellationToken);

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

                    order.ApplyCheckoutContext(
                        pricingContext.ShippingAmount,
                        pricingContext.DiscountAmount,
                        pricingContext.ShippingMethodId,
                        pricingContext.ShippingMethodName,
                        pricingContext.ShippingMethodType,
                        pricingContext.ShippingAddressId,
                        pricingContext.RecipientName,
                        pricingContext.RecipientPhone,
                        pricingContext.CountryCode,
                        pricingContext.Region,
                        pricingContext.City,
                        pricingContext.PostalCode,
                        pricingContext.AddressLine1,
                        pricingContext.AddressLine2,
                        pricingContext.CouponCode,
                        now,
                        command.CustomerUserId.Value);

                    if (_holds?.Enabled != true)
                    {
                        // Legacy behavior remains available until the new migration
                        // and manual-payment acceptance suite have been verified.
                        foreach (var item in cart.Items)
                        {
                            var variant = variantsById[item.ProductVariantId];
                            var before = variant.Inventory.Quantity;
                            variant.DecreaseStock(item.Quantity, now, command.CustomerUserId.Value);
                            order.RecordCheckoutInventoryDeduction(item.ProductId, variant.Id,
                                before, variant.Inventory.Quantity, now, command.CustomerUserId.Value);
                        }
                    }

                    await _orderRepository
                        .AddAsync(
                            order,
                            idempotencyKey,
                            transactionCancellationToken);

                    if (_checkoutPricingService is not null)
                    {
                        await _checkoutPricingService.RecordRedemptionAsync(
                            order,
                            pricingContext,
                            command.CustomerUserId,
                            transactionCancellationToken);
                    }

                    if (_holds?.Enabled == true)
                    {
                        // The order and redemption are saved in this same transaction
                        // before adding the ledger marker's foreign key.
                        await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                        try
                        {
                            await _holds.ConvertCartToOrderAsync(cart, order, now,
                                transactionCancellationToken);
                        }
                        catch (StockHoldUnavailableException)
                        {
                            throw new CheckoutInsufficientStockException();
                        }
                    }

                    // Legacy checkout deducts stock now and converts the cart.
                    // Deferred checkout keeps it visible but freezes all mutations
                    // until a payment decision or an explicit safe edit request.
                    if (_holds?.Enabled != true)
                        cart.MarkConverted(now, command.CustomerUserId.Value);

                    if (_emailQueue is not null)
                    {
                        await _emailQueue.QueueNewOrderAsync(
                            _currentTenant.TenantId!.Value,
                            order.Id,
                            order.TotalAmount,
                            order.Currency.Value,
                            now,
                            transactionCancellationToken);
                    }

                    await _unitOfWork
                        .SaveChangesAsync(
                            transactionCancellationToken);

                    return MapOrder(
                        order,
                        isIdempotentReplay: false);
        }

        return withinExistingTransaction
            ? await ExecuteCheckoutAsync(cancellationToken)
            : await _transactionExecutor.ExecuteAsync(ExecuteCheckoutAsync, cancellationToken);
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
            order.SubtotalAmount,
            order.ShippingAmount,
            order.DiscountAmount,
            order.TotalAmount,
            order.AppliedCouponCode,
            order.ShippingMethodName,
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
