using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using PortManagement.Application.ControlTower;

namespace PortManagement.Api.Realtime;

internal sealed class ControlTowerBroadcastService(
    IServiceScopeFactory scopeFactory,
    IHubContext<ControlTowerHub> hub,
    ILogger<ControlTowerBroadcastService> logger)
    : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(15);
    private static readonly Action<ILogger, Exception?> LogBroadcastFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1101, nameof(ControlTowerBroadcastService)),
        "Falha ao verificar mudanças da torre de controle.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RefreshInterval);
        string? previousFingerprint = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<GetControlTowerHandler>();
                var response = (await handler.HandleAsync(stoppingToken)).Value!;
                var fingerprint = CreateFingerprint(response);

                if (previousFingerprint is not null
                    && !string.Equals(previousFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    await hub.Clients.All.SendAsync(
                        RealtimeEvents.ControlTowerUpdated,
                        response,
                        stoppingToken);
                }

                previousFingerprint = fingerprint;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogBroadcastFailure(logger, exception);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal static string CreateFingerprint(ControlTowerResponse response)
    {
        var callState = string.Join(
            ';',
            response.Calls.Select(call => string.Join(
                ':',
                call.Id,
                call.Status,
                call.AlertCount,
                call.LastActivityAtUtc?.UtcTicks ?? 0)));
        var alertState = string.Join(
            ';',
            response.Alerts.Select(alert => string.Join(
                ':',
                alert.Id,
                alert.Severity,
                alert.DeviationMinutes is null
                    ? string.Empty
                    : (alert.DeviationMinutes.Value / 5).ToString(CultureInfo.InvariantCulture))));

        return string.Join(
            '|',
            response.Summary.ActivePortCalls,
            response.Summary.InOperation,
            response.Summary.CallsRequiringAttention,
            response.Summary.CriticalAlerts,
            callState,
            alertState);
    }
}
