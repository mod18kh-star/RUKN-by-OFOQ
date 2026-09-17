namespace OFOQ.Market.Application.Tenancy.StoreReadiness;

public sealed record StoreReadinessItemResult(
    string Code,
    int Weight,
    bool Completed,
    bool MerchantActionRequired);

public sealed record StoreReadinessResult(
    int Percentage,
    string State,
    string StoreStatus,
    IReadOnlyList<StoreReadinessItemResult> Items);
