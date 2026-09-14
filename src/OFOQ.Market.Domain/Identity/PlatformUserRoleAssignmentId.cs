namespace OFOQ.Market.Domain.Identity;

public readonly record struct PlatformUserRoleAssignmentId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static PlatformUserRoleAssignmentId New()
    {
        return new PlatformUserRoleAssignmentId(
            Guid.NewGuid());
    }

    public static PlatformUserRoleAssignmentId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Platform user role assignment ID cannot be empty.",
                nameof(value));
        }

        return new PlatformUserRoleAssignmentId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
