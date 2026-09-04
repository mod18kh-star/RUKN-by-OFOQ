using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductVariantOptionValueTests
{
    [Fact]
    public void Create_PreservesAllRelationships()
    {
        var tenantId =
            TenantId.New();

        var productId =
            ProductId.New();

        var variantId =
            ProductVariantId.New();

        var optionId =
            ProductOptionId.New();

        var optionValueId =
            ProductOptionValueId.New();

        var assignment =
            ProductVariantOptionValue.Create(
                tenantId,
                productId,
                variantId,
                optionId,
                optionValueId,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            tenantId,
            assignment.TenantId);

        Assert.Equal(
            productId,
            assignment.ProductId);

        Assert.Equal(
            variantId,
            assignment.ProductVariantId);

        Assert.Equal(
            optionId,
            assignment.ProductOptionId);

        Assert.Equal(
            optionValueId,
            assignment.ProductOptionValueId);

        Assert.False(
            assignment.IsDeleted);
    }

    [Fact]
    public void Create_WithEmptyVariantId_Throws()
    {
        Assert.Throws<
            ArgumentException>(
                () =>
                    ProductVariantOptionValue.Create(
                        TenantId.New(),
                        ProductId.New(),
                        default,
                        ProductOptionId.New(),
                        ProductOptionValueId.New(),
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Delete_SoftDeletesAssignment()
    {
        var now =
            DateTimeOffset.UtcNow;

        var assignment =
            ProductVariantOptionValue.Create(
                TenantId.New(),
                ProductId.New(),
                ProductVariantId.New(),
                ProductOptionId.New(),
                ProductOptionValueId.New(),
                now);

        assignment.Delete(
            now.AddMinutes(1));

        Assert.True(
            assignment.IsDeleted);

        Assert.Equal(
            now.AddMinutes(1),
            assignment.DeletedAtUtc);
    }
}