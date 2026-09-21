using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductVerticalCoverageTests
{
    [Fact]
    public void EveryCommerceVertical_HasResolvableProductSchema()
    {
        foreach (var definition in CommerceVerticalCatalog.All)
        {
            var schema = ProductAttributeSchemaCatalog.GetByCode(definition.Code);

            Assert.Equal(definition.VerticalType, schema.VerticalType);
            Assert.NotEmpty(schema.Attributes);
        }
    }

    [Fact]
    public void SpecializedVerticalSchemas_HaveUniqueKeys()
    {
        foreach (var definition in CommerceVerticalCatalog.All)
        {
            var schema = ProductAttributeSchemaCatalog.Get(definition.VerticalType);
            var keys = schema.Attributes.Select(attribute => attribute.Key).ToArray();

            Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        }
    }
}