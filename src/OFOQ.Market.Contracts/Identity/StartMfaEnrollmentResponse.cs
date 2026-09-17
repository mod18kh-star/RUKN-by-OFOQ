namespace OFOQ.Market.Contracts.Identity;

public sealed record StartMfaEnrollmentResponse(
    string ManualEntryKey,
    string ProvisioningUri);
