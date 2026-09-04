namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductVariantSelectionResponse(
    Guid OptionId,
    string OptionName,
    Guid ValueId,
    string Value);