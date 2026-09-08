namespace OFOQ.Market.Domain.Commerce.Configuration;

public sealed record CommerceVerticalDefinition
{
    public CommerceVerticalDefinition(
        CommerceVerticalType verticalType,
        string code,
        IEnumerable<CommerceCapabilityType> defaultCapabilities)
    {
        if (verticalType == CommerceVerticalType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalType));
        }

        if (string.IsNullOrWhiteSpace(
                code))
        {
            throw new ArgumentException(
                "Commerce vertical code is required.",
                nameof(code));
        }

        ArgumentNullException.ThrowIfNull(
            defaultCapabilities);

        VerticalType =
            verticalType;

        Code =
            code.Trim();

        DefaultCapabilities =
            defaultCapabilities
                .Where(
                    capability =>
                        capability !=
                        CommerceCapabilityType.Unknown)
                .Distinct()
                .OrderBy(
                    capability =>
                        capability)
                .ToArray();
    }

    public CommerceVerticalType VerticalType { get; }

    public string Code { get; }

    public IReadOnlyCollection<CommerceCapabilityType>
        DefaultCapabilities { get; }
}