using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts;

public sealed class CartLockedForCheckoutException : InvalidOperationException
{
    public CartLockedForCheckoutException() : base(
        "This cart is no longer locked by a pending checkout.")
    {
    }
}

/// <summary>
/// Compatibility hook retained for existing cart handlers.
///
/// Pending orders are independent historical purchase snapshots.
/// They must never block the customer's current editable cart.
///
/// The source cart is converted when payment evidence is submitted.
/// The next add-to-cart operation therefore creates or uses a new
/// independent active cart.
/// </summary>
internal static class PendingCheckoutCartGuard
{
    public static Task EnsureEditableAsync(
        Cart cart,
        IOrderRepository? orders,
        CancellationToken cancellationToken)
    {
        _ = cart;
        _ = orders;
        _ = cancellationToken;

        return Task.CompletedTask;
    }
}