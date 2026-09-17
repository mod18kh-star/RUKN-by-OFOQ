using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Returns;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ReturnRequestLockRepository : IReturnRequestLockRepository
{
    private readonly MarketDbContext _db;
    public ReturnRequestLockRepository(MarketDbContext db)=>_db=db;

    public async Task<ReturnRequest?> GetForUpdateAsync(ReturnRequestId id,CancellationToken cancellationToken=default)
    {
        if(!_db.HasCurrentTenant||_db.CurrentTenantId.IsEmpty) throw new TenantScopeViolationException("Tenant context is required for return locking.");
        var request=await _db.ReturnRequests.FromSqlInterpolated($"SELECT * FROM commerce_return_requests WHERE tenant_id = {_db.CurrentTenantId.Value} AND id = {id.Value} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
        if(request is null)return null;
        await _db.Entry(request).Collection("_items").LoadAsync(cancellationToken);
        return request;
    }
}
