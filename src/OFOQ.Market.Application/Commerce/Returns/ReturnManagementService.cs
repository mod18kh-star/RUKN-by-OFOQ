using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Returns;

public sealed record CreateReturnItemInput(Guid OrderItemId, int Quantity);
public sealed record ReturnItemResult(Guid Id, Guid OrderItemId, Guid ProductId, Guid ProductVariantId, int Quantity, int RestockedQuantity, DateTimeOffset? RestockedAtUtc);
public sealed record ReturnRequestResult(Guid Id, Guid OrderId, Guid CustomerUserId, string Reason, string Status, string? MerchantNote, DateTimeOffset CreatedAtUtc, DateTimeOffset? ApprovedAtUtc, DateTimeOffset? RejectedAtUtc, DateTimeOffset? ReceivedAtUtc, DateTimeOffset? CompletedAtUtc, DateTimeOffset? CancelledAtUtc, IReadOnlyList<ReturnItemResult> Items);

public sealed class ReturnManagementService
{
    private readonly ICurrentTenant _tenant;
    private readonly IOrderRepository _orders;
    private readonly IReturnRequestRepository _returns;
    private readonly IReturnRequestLockRepository _returnLocks;
    private readonly IOrderStateLockRepository _orderLocks;
    private readonly ITransactionExecutor _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public ReturnManagementService(ICurrentTenant tenant, IOrderRepository orders, IReturnRequestRepository returns, IReturnRequestLockRepository returnLocks, IOrderStateLockRepository orderLocks, ITransactionExecutor transactions, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _tenant=tenant; _orders=orders; _returns=returns; _returnLocks=returnLocks; _orderLocks=orderLocks; _transactions=transactions; _unitOfWork=unitOfWork; _time=time;
    }

    public async Task<ReturnRequestResult> CreateAsync(UserId customerUserId, OrderId orderId, string reason, IReadOnlyCollection<CreateReturnItemInput> items, CancellationToken ct=default)
    {
        EnsureTenant();
        if (customerUserId.IsEmpty) throw new ArgumentException("Customer user ID cannot be empty.", nameof(customerUserId));
        if (orderId.IsEmpty) throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
        ArgumentNullException.ThrowIfNull(items);
        var order=await _orders.GetByIdAsync(orderId,ct) ?? throw new KeyNotFoundException("Order was not found.");
        if (order.CustomerUserId != customerUserId) throw new UnauthorizedAccessException("The order does not belong to the current customer.");
        if (order.FulfillmentStatus is not (OrderFulfillmentStatus.Delivered or OrderFulfillmentStatus.Fulfilled)) throw new InvalidOperationException("Returns can only be requested after delivery.");
        if (items.Count==0) throw new ArgumentException("At least one return item is required.",nameof(items));

        var tuples=new List<(OrderItemId,ProductId,ProductVariantId,int)>(items.Count);
        var seen=new HashSet<OrderItemId>();
        foreach(var input in items)
        {
            var orderItemId=OrderItemId.From(input.OrderItemId);
            if(!seen.Add(orderItemId)) throw new ArgumentException("Duplicate order item in return request.",nameof(items));
            var orderItem=order.Items.SingleOrDefault(x=>x.Id==orderItemId) ?? throw new ArgumentException("Return item does not belong to the order.",nameof(items));
            if(input.Quantity<=0) throw new ArgumentOutOfRangeException(nameof(items),"Return quantity must be positive.");
            var reserved=await _returns.GetReservedQuantityAsync(orderItemId,ct);
            if(reserved+input.Quantity>orderItem.Quantity) throw new InvalidOperationException("Requested return quantity exceeds the remaining returnable quantity.");
            tuples.Add((orderItem.Id,orderItem.ProductId,orderItem.ProductVariantId,input.Quantity));
        }

        var now=_time.GetUtcNow();
        var request=ReturnRequest.Create(_tenant.TenantId!.Value,order.Id,customerUserId,reason,tuples,now,customerUserId.Value);
        await _returns.AddAsync(request,ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(request);
    }

    public async Task<IReadOnlyList<ReturnRequestResult>> GetMineAsync(UserId customerUserId,CancellationToken ct=default)
    { EnsureTenant(); return (await _returns.GetForCustomerAsync(customerUserId,ct)).Select(Map).ToArray(); }

    public async Task<IReadOnlyList<ReturnRequestResult>> GetAllAsync(int take,CancellationToken ct=default)
    { EnsureTenant(); if(take<1||take>200) throw new ArgumentOutOfRangeException(nameof(take)); return (await _returns.GetAllAsync(take,ct)).Select(Map).ToArray(); }

    public async Task<ReturnRequestResult?> GetAsync(ReturnRequestId id,CancellationToken ct=default)
    { EnsureTenant(); var x=await _returns.GetByIdAsync(id,ct); return x is null?null:Map(x); }

    public async Task<ReturnRequestResult?> CancelMineAsync(UserId customerUserId,ReturnRequestId id,CancellationToken ct=default)
    {
        EnsureTenant(); var r=await _returns.GetByIdAsync(id,ct); if(r is null)return null; if(r.CustomerUserId!=customerUserId) throw new UnauthorizedAccessException(); r.Cancel(_time.GetUtcNow(),customerUserId.Value); await _unitOfWork.SaveChangesAsync(ct); return Map(r);
    }

    public async Task<ReturnRequestResult?> ApproveAsync(ReturnRequestId id,string? note,Guid actor,CancellationToken ct=default)
    { EnsureTenant(); var r=await _returns.GetByIdAsync(id,ct); if(r is null)return null; r.Approve(note,_time.GetUtcNow(),actor); await _unitOfWork.SaveChangesAsync(ct); return Map(r); }

    public async Task<ReturnRequestResult?> RejectAsync(ReturnRequestId id,string? note,Guid actor,CancellationToken ct=default)
    { EnsureTenant(); var r=await _returns.GetByIdAsync(id,ct); if(r is null)return null; r.Reject(note,_time.GetUtcNow(),actor); await _unitOfWork.SaveChangesAsync(ct); return Map(r); }

    public Task<ReturnRequestResult?> ReceiveAndRestockAsync(ReturnRequestId id,Guid actor,CancellationToken ct=default)
    {
        EnsureTenant();
        return _transactions.ExecuteAsync(async tx=>
        {
            var r=await _returnLocks.GetForUpdateAsync(id,tx); if(r is null)return null;
            if(r.Status is ReturnRequestStatus.Received or ReturnRequestStatus.Completed) return Map(r);
            if(r.Status!=ReturnRequestStatus.Approved) throw new InvalidOperationException("Only approved return requests can be received.");
            var variantIds=r.Items.Select(x=>x.ProductVariantId).Distinct().ToArray();
            var variants=await _orderLocks.GetVariantsForUpdateAsync(variantIds,tx);
            if(variants.Count!=variantIds.Length) throw new InvalidOperationException("A returned product variant is no longer available for inventory restock.");
            var byId=variants.ToDictionary(x=>x.Id);
            var now=_time.GetUtcNow();
            foreach(var item in r.Items)
            {
                if(item.RestockedQuantity==item.Quantity) continue;
                if(!byId.TryGetValue(item.ProductVariantId,out var variant)) throw new InvalidOperationException("Returned variant could not be locked.");
                variant.IncreaseStock(item.Quantity,now,actor);
            }
            r.MarkReceived(now,actor);
            await _unitOfWork.SaveChangesAsync(tx);
            return Map(r);
        },ct);
    }

    public async Task<ReturnRequestResult?> CompleteAsync(ReturnRequestId id,string? note,Guid actor,CancellationToken ct=default)
    { EnsureTenant(); var r=await _returns.GetByIdAsync(id,ct); if(r is null)return null; r.Complete(note,_time.GetUtcNow(),actor); await _unitOfWork.SaveChangesAsync(ct); return Map(r); }

    private void EnsureTenant(){ if(!_tenant.IsAvailable||!_tenant.TenantId.HasValue||_tenant.TenantId.Value.IsEmpty) throw new TenantScopeViolationException("Tenant context is required."); }
    private static ReturnRequestResult Map(ReturnRequest x)=>new(x.Id.Value,x.OrderId.Value,x.CustomerUserId.Value,x.Reason,x.Status.ToString(),x.MerchantNote,x.CreatedAtUtc,x.ApprovedAtUtc,x.RejectedAtUtc,x.ReceivedAtUtc,x.CompletedAtUtc,x.CancelledAtUtc,x.Items.OrderBy(i=>i.Id.Value).Select(i=>new ReturnItemResult(i.Id.Value,i.OrderItemId.Value,i.ProductId.Value,i.ProductVariantId.Value,i.Quantity,i.RestockedQuantity,i.RestockedAtUtc)).ToArray());
}
