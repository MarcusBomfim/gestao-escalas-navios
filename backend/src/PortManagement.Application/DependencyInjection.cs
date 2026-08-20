using Microsoft.Extensions.DependencyInjection;
using PortManagement.Application.PortCalls;
using PortManagement.Application.ReferenceData;
using PortManagement.Application.Vessels;

namespace PortManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterVesselHandler>();
        services.AddScoped<GetVesselHandler>();
        services.AddScoped<ListVesselsHandler>();
        services.AddScoped<CreatePortCallHandler>();
        services.AddScoped<GetPortCallHandler>();
        services.AddScoped<ListPortCallsHandler>();
        services.AddScoped<TransitionPortCallHandler>();
        services.AddScoped<GetPortStructureHandler>();

        return services;
    }
}
