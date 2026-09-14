using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductAttributeTests
{
    [Fact]
    public void MobilePhonesSchema_ContainsExpectedFields()
    {
        var schema =
            ProductAttributeSchemaCatalog.Get(
                CommerceVerticalType.MobilePhones);

        Assert.Equal(
            CommerceVerticalType.MobilePhones,
            schema.VerticalType);

        Assert.Contains(
            schema.Attributes,
            attribute =>
                attribute.Key ==
                "brand");

        Assert.Contains(
            schema.Attributes,
            attribute =>
                attribute.Key ==
                "storage-gb");

        Assert.Contains(
            schema.Attributes,
            attribute =>
                attribute.Key ==
                "ram-gb");

        Assert.Contains(
            schema.Attributes,
            attribute =>
                attribute.Key ==
                "dual-sim");
    }

    [Fact]
    public void ValueNormalizer_NormalizesSupportedTypes()
    {
        var integer =
            new ProductAttributeDefinition(
                "storage",
                "Storage",
                ProductAttributeValueType.Integer);

        var number =
            new ProductAttributeDefinition(
                "volume",
                "Volume",
                ProductAttributeValueType.Decimal);

        var boolean =
            new ProductAttributeDefinition(
                "enabled",
                "Enabled",
                ProductAttributeValueType.Boolean);

        var choice =
            new ProductAttributeDefinition(
                "gender",
                "Gender",
                ProductAttributeValueType.Choice,
                new[]
                {
                    "Men",
                    "Women"
                });

        Assert.Equal(
            "128",
            ProductAttributeValueNormalizer.Normalize(
                integer,
                "00128"));

        Assert.Equal(
            "12.5",
            ProductAttributeValueNormalizer.Normalize(
                number,
                "12.500"));

        Assert.Equal(
            "true",
            ProductAttributeValueNormalizer.Normalize(
                boolean,
                "TRUE"));

        Assert.Equal(
            "Women",
            ProductAttributeValueNormalizer.Normalize(
                choice,
                "women"));
    }

    [Fact]
    public void ValueNormalizer_BlankValue_ReturnsNull()
    {
        var definition =
            new ProductAttributeDefinition(
                "brand",
                "Brand",
                ProductAttributeValueType.Text);

        var result =
            ProductAttributeValueNormalizer.Normalize(
                definition,
                "   ");

        Assert.Null(
            result);
    }

    [Fact]
    public void ValueNormalizer_InvalidChoice_Throws()
    {
        var definition =
            new ProductAttributeDefinition(
                "fit",
                "Fit",
                ProductAttributeValueType.Choice,
                new[]
                {
                    "Slim",
                    "Regular"
                });

        Assert.Throws<ArgumentException>(
            () =>
                ProductAttributeValueNormalizer.Normalize(
                    definition,
                    "Invalid"));
    }

    [Fact]
    public void Schema_DuplicateNormalizedKeys_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new ProductAttributeSchema(
                    CommerceVerticalType.GeneralRetail,
                    new[]
                    {
                        new ProductAttributeDefinition(
                            "brand",
                            "Brand",
                            ProductAttributeValueType.Text),

                        new ProductAttributeDefinition(
                            " BRAND ",
                            "Another Brand",
                            ProductAttributeValueType.Text)
                    }));
    }

    [Fact]
    public void AttributeValue_NormalizesKeyAndTracksUpdate()
    {
        var tenantId =
            TenantId.New();

        var productId =
            ProductId.New();

        var actor =
            Guid.NewGuid();

        var createdAt =
            DateTimeOffset.UtcNow;

        var value =
            ProductAttributeValue.Create(
                tenantId,
                productId,
                " BRAND ",
                " Apple ",
                createdAt,
                actor);

        Assert.Equal(
            "brand",
            value.Key);

        Assert.Equal(
            "Apple",
            value.Value);

        var updatedAt =
            createdAt.AddMinutes(
                1);

        value.ChangeValue(
            "Samsung",
            updatedAt,
            actor);

        Assert.Equal(
            "Samsung",
            value.Value);

        Assert.Equal(
            updatedAt,
            value.UpdatedAtUtc);

        Assert.Equal(
            actor,
            value.UpdatedByUserId);
    }
}