using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductRelationTests
{
    [Theory]
    [InlineData(ProductRelationType.Related)]
    [InlineData(ProductRelationType.Accessory)]
    [InlineData(ProductRelationType.Alternative)]
    [InlineData(ProductRelationType.Compatible)]
    [InlineData(ProductRelationType.FrequentlyBoughtTogether)]
    [InlineData(ProductRelationType.Upsell)]
    public void Create_SupportedRelationType_Succeeds(
        ProductRelationType type)
    {
        var relation =
            ProductRelation.Create(
                TenantId.New(),
                ProductId.New(),
                ProductId.New(),
                type,
                0,
                true,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            type,
            relation.Type);
    }

    [Fact]
    public void Create_SelfRelation_Throws()
    {
        var productId =
            ProductId.New();

        Assert.Throws<ArgumentException>(
            () =>
                ProductRelation.Create(
                    TenantId.New(),
                    productId,
                    productId,
                    ProductRelationType.Accessory,
                    0,
                    true,
                    DateTimeOffset.UtcNow));
    }
}
