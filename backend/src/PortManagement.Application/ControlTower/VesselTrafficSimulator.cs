using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.ControlTower;

public static class VesselTrafficSimulator
{
    private const int ObservationIntervalSeconds = 5;

    public static VesselTrafficResponse Evaluate(
        ControlTowerSnapshot snapshot,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var observedAt = RoundToObservationInterval(nowUtc.ToUniversalTime());
        var positions = snapshot.Calls
            .Select(call => CreatePosition(call, observedAt))
            .OrderBy(position => position.VesselName, StringComparer.Ordinal)
            .ToArray();

        return new VesselTrafficResponse(
            observedAt,
            "Canal portuário demonstrativo",
            true,
            positions);
    }

    internal static VesselPositionResponse CreatePosition(
        ControlTowerCallSnapshot call,
        DateTimeOffset observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(call);

        var seed = StableSeed(call.PortCallId);
        var laneOffset = ((seed % 9) - 4) * 0.7m;
        var driftStep = (int)((observedAtUtc.ToUnixTimeSeconds() / ObservationIntervalSeconds + seed) % 7) - 3;
        var drift = driftStep * 0.22m;
        var (state, x, y, speed, course) = PositionFor(call.Status, seed, laneOffset, drift);

        return new VesselPositionResponse(
            call.PortCallId,
            call.PublicCode,
            call.VesselName,
            call.PortName,
            call.TerminalName,
            call.BerthName,
            call.Status,
            state,
            Clamp(x),
            Clamp(y),
            speed,
            course,
            observedAtUtc,
            true);
    }

    private static (VesselNavigationState State, decimal X, decimal Y, decimal Speed, int Course)
        PositionFor(PortCallStatus status, int seed, decimal laneOffset, decimal drift) => status switch
        {
            PortCallStatus.Draft or PortCallStatus.Requested or PortCallStatus.UnderReview =>
                (VesselNavigationState.AwaitingSchedule, 87m, 22m + ((seed % 4) * 14m), 0m, 0),
            PortCallStatus.Planned =>
                (VesselNavigationState.Approaching, 74m + drift, 38m + laneOffset, 8.4m, 255),
            PortCallStatus.AtAnchorage =>
                (VesselNavigationState.Anchored, 64m + (drift / 4m), 69m + laneOffset, 0m, 0),
            PortCallStatus.ClearedForBerthing =>
                (VesselNavigationState.Manoeuvring, 53m + drift, 51m + laneOffset, 3.2m, 250),
            PortCallStatus.Berthed => BerthPosition(
                VesselNavigationState.Berthed,
                seed),
            PortCallStatus.InOperation => BerthPosition(
                VesselNavigationState.Operating,
                seed),
            PortCallStatus.OperationCompleted => BerthPosition(
                VesselNavigationState.ReadyToSail,
                seed),
            PortCallStatus.Unberthed =>
                (VesselNavigationState.Departing, 68m + (drift * 1.4m), 47m + laneOffset, 9.1m, 80),
            _ => (VesselNavigationState.AwaitingSchedule, 90m, 82m, 0m, 0)
        };

    private static (VesselNavigationState State, decimal X, decimal Y, decimal Speed, int Course)
        BerthPosition(VesselNavigationState state, int seed) =>
        (
            state,
            23m + ((seed % 3) * 10m),
            30m + ((seed % 2) * 38m),
            0m,
            0);

    private static DateTimeOffset RoundToObservationInterval(DateTimeOffset value)
    {
        var seconds = value.ToUnixTimeSeconds();
        return DateTimeOffset.FromUnixTimeSeconds(
            seconds - (seconds % ObservationIntervalSeconds));
    }

    private static int StableSeed(Guid value)
    {
        var bytes = value.ToByteArray();
        return bytes.Aggregate(17, (current, item) => unchecked((current * 31) + item)) & int.MaxValue;
    }

    private static decimal Clamp(decimal value) => Math.Clamp(value, 5m, 95m);
}
