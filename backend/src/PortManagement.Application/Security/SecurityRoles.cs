namespace PortManagement.Application.Security;

public static class SecurityRoles
{
    public const string Administrator = "Administrator";
    public const string Planner = "Planner";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(
        [Administrator, Planner, Operator, Viewer],
        StringComparer.Ordinal);
}

public static class AuthorizationPolicies
{
    public const string ManageUsers = nameof(ManageUsers);
    public const string ManageMasterData = nameof(ManageMasterData);
    public const string ManageVessels = nameof(ManageVessels);
    public const string CreatePortCalls = nameof(CreatePortCalls);
    public const string TransitionPortCalls = nameof(TransitionPortCalls);
    public const string ManageBerthPlanning = nameof(ManageBerthPlanning);
    public const string ManageOperationalExecution = nameof(ManageOperationalExecution);
    public const string ViewAuditReports = nameof(ViewAuditReports);
    public const string ViewObservability = nameof(ViewObservability);
}
