using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Discounts;
namespace OFOQ.Market.Infrastructure.Persistence.Repositories;
public sealed class DiscountCouponLockRepository : IDiscountCouponLockRepository
{
    private readonly MarketDbContext _db; private readonly ICurrentTenant _tenant;
    public DiscountCouponLockRepository(MarketDbContext db,ICurrentTenant tenant){_db=db;_tenant=tenant;}
    public Task<DiscountCoupon?> GetByCodeForUpdateAsync(string normalizedCode,CancellationToken cancellationToken=default)
    {
        if(!_tenant.IsAvailable||!_tenant.TenantId.HasValue)throw new TenantScopeViolationException("Tenant context is required.");
        var tenantId=_tenant.TenantId.Value.Value;
        return _db.DiscountCoupons.FromSqlInterpolated($"SELECT * FROM commerce_discount_coupons WHERE tenant_id = {tenantId} AND code = {normalizedCode} FOR UPDATE").Include("_productTargets").Include("_categoryTargets").SingleOrDefaultAsync(cancellationToken);
    }
}
