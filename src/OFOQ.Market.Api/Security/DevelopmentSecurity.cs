namespace OFOQ.Market.Api.Security;

public static class DevelopmentSecurity
{
    public const string MfaBypassConfigurationKey =
        "DevelopmentSecurity:BypassMfa";

    public static bool IsMfaBypassEnabled(
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            environment);

        ArgumentNullException.ThrowIfNull(
            configuration);

        /*
         * Fail closed:
         * - Production can never use this bypass.
         * - Development must opt in explicitly.
         */
        return environment.IsDevelopment() &&
            configuration.GetValue<bool>(
                MfaBypassConfigurationKey);
    }
}
