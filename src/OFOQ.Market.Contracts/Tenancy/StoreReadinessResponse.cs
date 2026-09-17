namespace OFOQ.Market.Contracts.Tenancy;

public sealed record StoreReadinessItemResponse(
    string Code,
    int Weight,
    bool Completed,
    bool MerchantActionRequired);

public sealed record StoreReadinessResponse(
    int Percentage,
    string State,
    string StoreStatus,
    IReadOnlyList<StoreReadinessItemResponse> Items);
