using PortManagement.Application.Security;
using PortManagement.Domain.Operations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal static class OrganizationScopeQueryExtensions
{
    public static IQueryable<PortCall> ApplyOrganizationScope(
        this IQueryable<PortCall> query,
        IUserDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.HasGlobalAccess)
        {
            return query;
        }

        return scope.OrganizationId is Guid organizationId
            ? query.Where(portCall =>
                portCall.AgentOrganizationId == organizationId
                || portCall.ShippingLineOrganizationId == organizationId)
            : query.Where(_ => false);
    }

    public static IQueryable<BerthWindow> ApplyOrganizationScope(
        this IQueryable<BerthWindow> query,
        IUserDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.HasGlobalAccess)
        {
            return query;
        }

        return scope.OrganizationId is Guid organizationId
            ? query.Where(window =>
                window.PortCall.AgentOrganizationId == organizationId
                || window.PortCall.ShippingLineOrganizationId == organizationId)
            : query.Where(_ => false);
    }

    public static IQueryable<PortCallEvent> ApplyOrganizationScope(
        this IQueryable<PortCallEvent> query,
        IUserDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.HasGlobalAccess)
        {
            return query;
        }

        return scope.OrganizationId is Guid organizationId
            ? query.Where(portCallEvent =>
                portCallEvent.PortCall.AgentOrganizationId == organizationId
                || portCallEvent.PortCall.ShippingLineOrganizationId == organizationId)
            : query.Where(_ => false);
    }

    public static IQueryable<CargoOperation> ApplyOrganizationScope(
        this IQueryable<CargoOperation> query,
        IUserDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.HasGlobalAccess)
        {
            return query;
        }

        return scope.OrganizationId is Guid organizationId
            ? query.Where(operation =>
                operation.PortCall.AgentOrganizationId == organizationId
                || operation.PortCall.ShippingLineOrganizationId == organizationId)
            : query.Where(_ => false);
    }
}
