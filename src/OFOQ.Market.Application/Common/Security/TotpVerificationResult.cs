namespace OFOQ.Market.Application.Common.Security;

public sealed record TotpVerificationResult(
    bool IsValid,
    long? TimeStep);