using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Categories;

internal static class CategoryPosition
{
    public static int Resolve(
        int? requestedPosition,
        int itemCountAfterInsertion)
    {
        if (itemCountAfterInsertion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(itemCountAfterInsertion));
        }

        if (!requestedPosition.HasValue)
        {
            return itemCountAfterInsertion;
        }

        if (requestedPosition.Value <= 0)
        {
            throw new ArgumentException(
                "Category position must be greater than zero.",
                nameof(requestedPosition));
        }

        return Math.Min(
            requestedPosition.Value,
            itemCountAfterInsertion);
    }

    public static void Normalize(
        IReadOnlyList<Category> categories,
        DateTimeOffset updatedAtUtc,
        Guid actorUserId)
    {
        for (var index = 0;
             index < categories.Count;
             index++)
        {
            categories[index]
                .ChangeSortOrder(
                    index + 1,
                    updatedAtUtc,
                    actorUserId);
        }
    }
}
