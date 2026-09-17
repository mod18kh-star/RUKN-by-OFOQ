namespace OFOQ.Market.Application.Catalog.ProductContentBlocks;

public sealed record ProductContentBlockResult(
    Guid BlockId,
    string Type,
    string? Title,
    string? Body,
    string? MediaUrl,
    int SortOrder,
    bool IsVisible);

public sealed record ProductContentBlocksResult(
    Guid ProductId,
    IReadOnlyList<ProductContentBlockResult> Blocks);
