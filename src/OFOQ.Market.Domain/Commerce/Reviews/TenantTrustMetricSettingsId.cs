namespace OFOQ.Market.Domain.Commerce.Reviews;
public readonly record struct TenantTrustMetricSettingsId(Guid Value){public static TenantTrustMetricSettingsId New()=>new(Guid.NewGuid());public static TenantTrustMetricSettingsId From(Guid v){if(v==Guid.Empty)throw new ArgumentException("Trust metric settings ID cannot be empty.");return new(v);}}
