using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;

public sealed record VerifyMfaTotpResult(
    UserId UserId,
    string Email,
    
    UserSessionAuthenticationMethod AuthenticationMethod);
