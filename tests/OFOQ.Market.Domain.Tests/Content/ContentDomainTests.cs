using OFOQ.Market.Domain.Content;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Content;

public sealed class ContentDomainTests
{
    [Fact]
    public void Content_page_publish_and_unpublish_are_explicit()
    {
        var now=DateTimeOffset.UtcNow;
        var page=ContentPage.Create(TenantId.From(Guid.NewGuid()),"About","about-us","Body",null,null,now);
        page.Publish(now.AddMinutes(1),Guid.NewGuid());
        Assert.True(page.IsPublished);
        page.Unpublish(now.AddMinutes(2),Guid.NewGuid());
        Assert.False(page.IsPublished);
        Assert.Null(page.PublishedAtUtc);
    }

    [Fact]
    public void External_navigation_requires_http_url()
    {
        Assert.Throws<ArgumentException>(() => NavigationItem.Create(
            TenantId.From(Guid.NewGuid()),NavigationLocation.Header,NavigationTargetType.External,"Bad",null,"javascript:alert(1)",null,0,true,DateTimeOffset.UtcNow));
    }
}
