namespace OFOQ.Market.Application.Commerce.Configuration.Common;

public sealed class CommerceConfigurationConflictException :
    InvalidOperationException
{
    public CommerceConfigurationConflictException(
        string message)
        : base(message)
    {
    }
}