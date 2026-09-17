namespace OFOQ.Market.Infrastructure.Security;

public sealed class GoogleIdentityOptions
{
    public bool Enabled { get; init; }
    public string? ClientId { get; init; }

    public void Validate()
    {
        if (!Enabled)
            return;

        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("Google authentication is enabled but ClientId is not configured.");
    }
}
