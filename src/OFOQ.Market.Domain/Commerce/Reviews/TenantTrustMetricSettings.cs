using OFOQ.Market.Domain.Common; using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Domain.Commerce.Reviews;
public sealed class TenantTrustMetricSettings:Entity<TenantTrustMetricSettingsId>,ITenantDataScoped,IAuditable
{
 private TenantTrustMetricSettings(){} private TenantTrustMetricSettings(TenantTrustMetricSettingsId id,TenantId tenantId,DateTimeOffset at,Guid? by):base(id){TenantId=tenantId;ShowCustomerCount=true;ShowCompletedOrderCount=true;ShowAverageRating=true;CreatedAtUtc=at;CreatedByUserId=by;}
 public TenantId TenantId{get;private set;} public bool ShowCustomerCount{get;private set;} public bool ShowCompletedOrderCount{get;private set;} public bool ShowAverageRating{get;private set;} public DateTimeOffset CreatedAtUtc{get;private set;} public Guid? CreatedByUserId{get;private set;} public DateTimeOffset? UpdatedAtUtc{get;private set;} public Guid? UpdatedByUserId{get;private set;}
 public static TenantTrustMetricSettings Create(TenantId tenantId,DateTimeOffset at,Guid? by=null){if(tenantId.IsEmpty)throw new ArgumentException("Tenant ID cannot be empty.");return new(TenantTrustMetricSettingsId.New(),tenantId,at,by);} public void Update(bool customers,bool orders,bool rating,DateTimeOffset at,Guid? by){ShowCustomerCount=customers;ShowCompletedOrderCount=orders;ShowAverageRating=rating;UpdatedAtUtc=at;UpdatedByUserId=by;}
}
