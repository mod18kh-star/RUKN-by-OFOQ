using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.ProductContentBlocks;

public sealed record ProductContentBlockInput(
    ProductContentBlockType Type,
    string? Title,
    string? Body,
    string? MediaUrl,
    bool IsVisible);

public sealed record SetProductContentBlocksCommand(
    ProductId ProductId,
    IReadOnlyCollection<ProductContentBlockInput> Blocks,
    UserId ActorUserId);
