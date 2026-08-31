using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed class Tenant :
    AggregateRoot<TenantId>,
    IAuditable,
    ISoftDeletable
{
    private Tenant()
    {
    }

    private Tenant(
        TenantId id,
        string name,
        TenantSlug slug,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Status = TenantStatus.Draft;

        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public string Name { get; private set; } = string.Empty;

    public TenantSlug Slug { get; private set; }

    public TenantStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static Tenant Create(
        string name,
        string slug,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        var normalizedName = NormalizeName(name);
        var tenantSlug = TenantSlug.Create(slug);

        var tenant = new Tenant(
            TenantId.New(),
            normalizedName,
            tenantSlug,
            createdAtUtc,
            createdByUserId);

        tenant.RaiseDomainEvent(
            new TenantCreatedDomainEvent(
                tenant.Id,
                createdAtUtc));

        return tenant;
    }

    public void Rename(
        string name,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        Name = NormalizeName(name);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeSlug(
        string slug,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var newSlug = TenantSlug.Create(slug);

        if (Slug.Equals(newSlug))
            return;

        var previousSlug = Slug;

        Slug = newSlug;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        var domainEvent =
            new TenantSlugChangedDomainEvent(
                Id,
                previousSlug,
                newSlug,
                updatedAtUtc);

        RaiseDomainEvent(domainEvent);
    }

    public void Activate(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == TenantStatus.Active)
            return;

        Status = TenantStatus.Active;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Suspend(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == TenantStatus.Suspended)
            return;

        Status = TenantStatus.Suspended;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Delete(
        DateTimeOffset deletedAtUtc,
        Guid? deletedByUserId = null)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;

        MarkUpdated(
            deletedAtUtc,
            deletedByUserId);
    }

    public void Restore(
        DateTimeOffset restoredAtUtc,
        Guid? restoredByUserId = null)
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;

        MarkUpdated(
            restoredAtUtc,
            restoredByUserId);
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Tenant name is required.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > 200)
        {
            throw new ArgumentException(
                "Tenant name cannot exceed 200 characters.",
                nameof(name));
        }

        return normalizedName;
    }
}