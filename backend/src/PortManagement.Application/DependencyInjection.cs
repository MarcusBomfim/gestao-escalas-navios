using Microsoft.Extensions.DependencyInjection;
using PortManagement.Application.Auditing;
using PortManagement.Application.ControlTower;
using PortManagement.Application.Notifications;
using PortManagement.Application.Operations;
using PortManagement.Application.Planning;
using PortManagement.Application.PortCalls;
using PortManagement.Application.ReferenceData;
using PortManagement.Application.Security;
using PortManagement.Application.Vessels;

namespace PortManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAuditLogHandler>();
        services.AddScoped<ExportAuditLogHandler>();
        services.AddScoped<RegisterVesselHandler>();
        services.AddScoped<UpdateVesselHandler>();
        services.AddScoped<GetVesselHandler>();
        services.AddScoped<ListVesselsHandler>();
        services.AddScoped<CreatePortCallHandler>();
        services.AddScoped<GetPortCallHandler>();
        services.AddScoped<ListPortCallsHandler>();
        services.AddScoped<TransitionPortCallHandler>();
        services.AddScoped<RequestBerthWindowHandler>();
        services.AddScoped<ReprogramBerthWindowHandler>();
        services.AddScoped<ConfirmBerthWindowHandler>();
        services.AddScoped<CancelBerthWindowHandler>();
        services.AddScoped<GetPortCallBerthWindowHandler>();
        services.AddScoped<ListBerthWindowsHandler>();
        services.AddScoped<GetOperationalExecutionHandler>();
        services.AddScoped<RecordOperationalMilestoneHandler>();
        services.AddScoped<CreateCargoOperationHandler>();
        services.AddScoped<StartCargoOperationHandler>();
        services.AddScoped<CompleteCargoOperationHandler>();
        services.AddScoped<GetControlTowerHandler>();
        services.AddScoped<GetNotificationCenterHandler>();
        services.AddScoped<MarkNotificationReadHandler>();
        services.AddScoped<MarkAllNotificationsReadHandler>();
        services.AddScoped<GetPortStructureHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<RevokeSessionHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<ListUsersHandler>();
        services.AddScoped<GetUserManagementOptionsHandler>();
        services.AddScoped<UpdateUserHandler>();

        return services;
    }
}
