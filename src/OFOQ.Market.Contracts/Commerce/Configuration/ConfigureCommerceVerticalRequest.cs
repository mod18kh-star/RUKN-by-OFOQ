namespace OFOQ.Market.Contracts.Commerce.Configuration;

public sealed record ConfigureCommerceVerticalRequest(
    string VerticalType,
    bool Enabled,
    bool Primary);