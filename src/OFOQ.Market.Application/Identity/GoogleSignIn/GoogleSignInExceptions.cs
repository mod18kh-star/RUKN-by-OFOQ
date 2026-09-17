namespace OFOQ.Market.Application.Identity.GoogleSignIn;

public sealed class GoogleSignInRejectedException : Exception
{
    public GoogleSignInRejectedException() : base("Google sign-in could not be completed.") { }
}

public sealed class GoogleAccountAlreadyLinkedException : Exception
{
    public GoogleAccountAlreadyLinkedException() : base("This local account is already linked to another Google identity.") { }
}
