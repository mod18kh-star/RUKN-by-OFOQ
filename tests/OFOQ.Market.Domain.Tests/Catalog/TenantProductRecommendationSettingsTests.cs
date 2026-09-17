using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class TenantProductRecommendationSettingsTests
{
    [Fact]
    public void Update_ChangesMerchantRecommendationPreferences()
    {
        var settings =
            TenantProductRecommendationSettings.Create(
                TenantId.New(),
                true,
                false,
                DateTimeOffset.UtcNow);

        settings.Update(
            false,
            true,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.False(
            settings.IsEnabled);

        Assert.True(
            settings.AutomaticSuggestionsEnabled);
    }
}
