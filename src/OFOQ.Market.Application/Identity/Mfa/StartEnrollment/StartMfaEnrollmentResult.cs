namespace OFOQ.Market.Application.Identity.Mfa.StartEnrollment;

public sealed record StartMfaEnrollmentResult(
    string ManualEntryKey,
    string ProvisioningUri);