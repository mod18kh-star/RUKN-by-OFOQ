using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed class TenantStoreProfile :
    Entity<TenantStoreProfileId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxWebsiteUrlLength = 2048;
    public const int MaxPhoneLength = 40;
    public const int MaxCommercialRegistrationLength = 120;

    private TenantStoreProfile()
    {
    }

    private TenantStoreProfile(
        TenantStoreProfileId id,
        TenantId tenantId,
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        TenantId =
            tenantId;

        Apply(
            websiteUrl,
            whatsAppNumber,
            customerServicePhone,
            commercialRegistrationNumber,
            commercialRegistrationNotApplicable);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public string? WebsiteUrl { get; private set; }

    public string? WhatsAppNumber { get; private set; }

    public string? CustomerServicePhone { get; private set; }

    public string? CommercialRegistrationNumber { get; private set; }

    public bool CommercialRegistrationNotApplicable { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantStoreProfile Create(
        TenantId tenantId,
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantStoreProfile(
            TenantStoreProfileId.New(),
            tenantId,
            websiteUrl,
            whatsAppNumber,
            customerServicePhone,
            commercialRegistrationNumber,
            commercialRegistrationNotApplicable,
            createdAtUtc,
            createdByUserId);
    }

    public void Update(
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        Apply(
            websiteUrl,
            whatsAppNumber,
            customerServicePhone,
            commercialRegistrationNumber,
            commercialRegistrationNotApplicable);

        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private void Apply(
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable)
    {
        var normalizedCommercialRegistrationNumber =
            NormalizeOptional(
                commercialRegistrationNumber,
                MaxCommercialRegistrationLength,
                "Commercial registration number");

        if (commercialRegistrationNotApplicable &&
            normalizedCommercialRegistrationNumber is not null)
        {
            throw new ArgumentException(
                "Commercial registration number cannot be provided when commercial registration is marked as not applicable.",
                nameof(commercialRegistrationNumber));
        }

        WebsiteUrl =
            NormalizeWebsiteUrl(
                websiteUrl);

        WhatsAppNumber =
            NormalizeOptional(
                whatsAppNumber,
                MaxPhoneLength,
                "WhatsApp number");

        CustomerServicePhone =
            NormalizeOptional(
                customerServicePhone,
                MaxPhoneLength,
                "Customer service phone");

        CommercialRegistrationNumber =
            normalizedCommercialRegistrationNumber;

        CommercialRegistrationNotApplicable =
            commercialRegistrationNotApplicable;
    }

    private static string? NormalizeWebsiteUrl(
        string? value)
    {
        var normalized =
            NormalizeOptional(
                value,
                MaxWebsiteUrlLength,
                "Website URL");

        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException(
                "Website URL must be an absolute HTTP or HTTPS URL.",
                nameof(value));
        }

        return uri.AbsoluteUri;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
