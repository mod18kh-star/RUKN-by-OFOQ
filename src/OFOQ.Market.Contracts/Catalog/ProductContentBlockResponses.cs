namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductContentBlockResponse(
    Guid BlockId,
    string Type,
    string? Title,
    string? Body,
    string? MediaUrl,
    int SortOrder,
    bool IsVisible);

public sealed record ProductContentBlocksResponse(
    Guid ProductId,
    IReadOnlyList<ProductContentBlockResponse> Blocks);

public sealed record SetProductContentBlockRequest(
    string Type,
    string? Title,
    string? Body,
    string? MediaUrl,
    bool IsVisible);

public sealed record SetProductContentBlocksRequest(
    IReadOnlyCollection<SetProductContentBlockRequest> Blocks);
