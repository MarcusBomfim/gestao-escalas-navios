using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PortManagement.Api.Security;
using PortManagement.Application.Security;

namespace PortManagement.IntegrationTests;

public sealed class UserDataScopeTests
{
    [Fact]
    public void OrganizationClaimCreatesRestrictedScope()
    {
        var organizationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var scope = CreateScope(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(DataScopeClaims.OrganizationId, organizationId.ToString()),
            new Claim(ClaimTypes.Role, SecurityRoles.Planner));

        Assert.False(scope.HasGlobalAccess);
        Assert.Equal(organizationId, scope.OrganizationId);
    }

    [Fact]
    public void AdministratorReceivesGlobalScope()
    {
        var scope = CreateScope(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, SecurityRoles.Administrator));

        Assert.True(scope.HasGlobalAccess);
        Assert.Null(scope.OrganizationId);
    }

    [Fact]
    public void ExplicitSignedClaimAllowsGlobalDemonstrationScope()
    {
        var scope = CreateScope(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, SecurityRoles.Viewer),
            new Claim(DataScopeClaims.Scope, DataScopeClaims.Global));

        Assert.True(scope.HasGlobalAccess);
    }

    [Fact]
    public void AuthenticatedUserWithoutScopeHasNoImplicitGlobalAccess()
    {
        var scope = CreateScope(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, SecurityRoles.Operator));

        Assert.False(scope.HasGlobalAccess);
        Assert.Null(scope.OrganizationId);
    }

    [Fact]
    public void BackgroundExecutionWithoutHttpContextUsesSystemScope()
    {
        var scope = new HttpUserDataScope(new HttpContextAccessor());

        Assert.True(scope.HasGlobalAccess);
        Assert.Null(scope.OrganizationId);
    }

    private static HttpUserDataScope CreateScope(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return new HttpUserDataScope(new HttpContextAccessor { HttpContext = context });
    }
}
