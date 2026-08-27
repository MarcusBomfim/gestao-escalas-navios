namespace PortManagement.Application.Security;

public interface IUserDataScope
{
    Guid? OrganizationId { get; }

    bool HasGlobalAccess { get; }
}

public static class DataScopeClaims
{
    public const string Scope = "data_scope";
    public const string Global = "global";
    public const string OrganizationId = "organization_id";
    public const string SecurityStamp = "security_stamp";
}
