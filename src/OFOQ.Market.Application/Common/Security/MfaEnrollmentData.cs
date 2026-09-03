namespace OFOQ.Market.Application.Common.Security;

public sealed record MfaEnrollmentData(
    string Secret,
    string ProvisioningUri);