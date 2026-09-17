using System.Text;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Content;

public sealed class NavigationItem : Entity<NavigationItemId>, ITenantDataScoped, IAuditable
{
    public const int MaxLabelLength = 120;
    public const int MaxUrlLength = 2048;
    private NavigationItem() { }

    private NavigationItem(NavigationItemId id,TenantId tenantId,NavigationLocation location,NavigationTargetType targetType,string label,Guid? targetId,string? externalUrl,NavigationItemId? parentItemId,int sortOrder,bool isVisible,DateTimeOffset createdAtUtc,Guid? createdByUserId):base(id)
    {
        if(tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.",nameof(tenantId));
        TenantId=tenantId; Location=location; TargetType=targetType; Label=NormalizeLabel(label); TargetId=targetId; ExternalUrl=NormalizeUrl(externalUrl); ParentItemId=parentItemId; SortOrder=ValidateSort(sortOrder); IsVisible=isVisible; ValidateTarget(); CreatedAtUtc=createdAtUtc; CreatedByUserId=createdByUserId;
    }

    public TenantId TenantId { get; private set; }
    public NavigationLocation Location { get; private set; }
    public NavigationTargetType TargetType { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public Guid? TargetId { get; private set; }
    public string? ExternalUrl { get; private set; }
    public NavigationItemId? ParentItemId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsVisible { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static NavigationItem Create(TenantId tenantId,NavigationLocation location,NavigationTargetType targetType,string label,Guid? targetId,string? externalUrl,NavigationItemId? parentItemId,int sortOrder,bool isVisible,DateTimeOffset at,Guid? by=null)
        => new(NavigationItemId.New(),tenantId,location,targetType,label,targetId,externalUrl,parentItemId,sortOrder,isVisible,at,by);

    public void Update(NavigationLocation location,NavigationTargetType targetType,string label,Guid? targetId,string? externalUrl,NavigationItemId? parentItemId,int sortOrder,bool isVisible,DateTimeOffset at,Guid? by)
    {
        if(parentItemId.HasValue && parentItemId.Value==Id) throw new ArgumentException("Navigation item cannot be its own parent.");
        Location=location; TargetType=targetType; Label=NormalizeLabel(label); TargetId=targetId; ExternalUrl=NormalizeUrl(externalUrl); ParentItemId=parentItemId; SortOrder=ValidateSort(sortOrder); IsVisible=isVisible; ValidateTarget(); UpdatedAtUtc=at; UpdatedByUserId=by;
    }

    public void SetSortOrder(int sortOrder,DateTimeOffset at,Guid? by){SortOrder=ValidateSort(sortOrder);UpdatedAtUtc=at;UpdatedByUserId=by;}
    private void ValidateTarget(){ if(TargetType==NavigationTargetType.External){ if(TargetId.HasValue)throw new ArgumentException("External navigation item cannot have an internal target ID."); if(string.IsNullOrWhiteSpace(ExternalUrl))throw new ArgumentException("External navigation item requires a URL."); } else { if(!TargetId.HasValue||TargetId.Value==Guid.Empty)throw new ArgumentException("Internal navigation item requires a target ID."); if(ExternalUrl is not null)throw new ArgumentException("Internal navigation item cannot have an external URL."); } }
    private static int ValidateSort(int value){if(value<0)throw new ArgumentOutOfRangeException(nameof(value));return value;}
    private static string NormalizeLabel(string value){if(string.IsNullOrWhiteSpace(value))throw new ArgumentException("Navigation label is required.");var n=value.Trim().Normalize(NormalizationForm.FormKC);if(n.Length>MaxLabelLength)throw new ArgumentException($"Navigation label cannot exceed {MaxLabelLength} characters.");return n;}
    private static string? NormalizeUrl(string? value){if(string.IsNullOrWhiteSpace(value))return null;var n=value.Trim();if(n.Length>MaxUrlLength)throw new ArgumentException($"Navigation URL cannot exceed {MaxUrlLength} characters.");if(!Uri.TryCreate(n,UriKind.Absolute,out var uri)||(uri.Scheme!="http"&&uri.Scheme!="https"))throw new ArgumentException("External navigation URL must be an absolute HTTP/HTTPS URL.");return n;}
}
