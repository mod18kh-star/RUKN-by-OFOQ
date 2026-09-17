namespace OFOQ.Market.Domain.Platform;

public sealed record PlatformPlanDefinition(
    string Code,
    string Name,
    int Rank);

public static class PlatformPlanCatalog
{
    private static readonly PlatformPlanDefinition[] Definitions =
    [
        new("business", "Business", 10),
        new("pro", "Pro", 20),
        new("extra", "Extra", 30)
    ];

    public static IReadOnlyCollection<PlatformPlanDefinition> All =>
        Definitions;

    public static bool TryGet(
        string? code,
        out PlatformPlanDefinition definition)
    {
        var normalized = Normalize(code);

        definition =
            Definitions.FirstOrDefault(
                item => item.Code == normalized)!;

        return definition is not null;
    }

    public static string Normalize(string? code)
    {
        return (code ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }
}
