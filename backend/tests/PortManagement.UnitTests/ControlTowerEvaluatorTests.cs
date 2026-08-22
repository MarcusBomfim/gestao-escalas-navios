using PortManagement.Application.ControlTower;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;

namespace PortManagement.UnitTests;

public sealed class ControlTowerEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RequestedWindowPastItsStartCreatesCriticalAlert()
    {
        var call = CreateCall() with
        {
            Status = PortCallStatus.UnderReview,
            WindowStatus = BerthWindowStatus.Requested,
            WindowStartsAtUtc = Now.AddHours(-2),
            WindowEndsAtUtc = Now.AddHours(5)
        };

        var alert = Assert.Single(ControlTowerEvaluator.EvaluateCall(call, Now));

        Assert.Equal(OperationalAlertType.PendingBerthConfirmation, alert.Type);
        Assert.Equal(OperationalAlertSeverity.Critical, alert.Severity);
        Assert.Equal(120, alert.DeviationMinutes);
    }

    [Fact]
    public void PlannedCallWithoutWindowCreatesPlanningAlert()
    {
        var call = CreateCall() with
        {
            Status = PortCallStatus.Planned,
            WindowStatus = null,
            WindowStartsAtUtc = null,
            WindowEndsAtUtc = null
        };

        var alert = Assert.Single(ControlTowerEvaluator.EvaluateCall(call, Now));

        Assert.Equal(OperationalAlertType.MissingBerthPlan, alert.Type);
        Assert.Equal(OperationalAlertSeverity.Warning, alert.Severity);
    }

    [Fact]
    public void ConfirmedCallWithoutArrivalCreatesDelayAlert()
    {
        var call = CreateCall() with
        {
            Status = PortCallStatus.Planned,
            WindowStartsAtUtc = Now.AddHours(-3),
            WindowEndsAtUtc = Now.AddHours(5),
            ArrivedAtAnchorageUtc = null
        };

        var alert = Assert.Single(ControlTowerEvaluator.EvaluateCall(call, Now));

        Assert.Equal(OperationalAlertType.ArrivalDelay, alert.Type);
        Assert.Equal(OperationalAlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public void ActiveCargoPastItsPlanCreatesCargoDelayAlert()
    {
        var call = CreateCall() with
        {
            Status = PortCallStatus.InOperation,
            LastActivityAtUtc = Now,
            IncompleteCargoOperations = 1,
            OverdueCargoOperations = 1,
            OldestOverdueCargoEndUtc = Now.AddHours(-1)
        };

        var alert = Assert.Single(ControlTowerEvaluator.EvaluateCall(call, Now));

        Assert.Equal(OperationalAlertType.CargoDelay, alert.Type);
        Assert.Equal(60, alert.DeviationMinutes);
    }

    [Fact]
    public void OperationalCallWithoutRecentActivityCreatesStaleAlert()
    {
        var call = CreateCall() with
        {
            Status = PortCallStatus.AtAnchorage,
            LastActivityAtUtc = Now.AddHours(-5)
        };

        var alert = Assert.Single(ControlTowerEvaluator.EvaluateCall(call, Now));

        Assert.Equal(OperationalAlertType.StaleOperationalUpdate, alert.Type);
    }

    [Fact]
    public void SummaryConsolidatesAttentionOccupancyAndCompliance()
    {
        var compliant = CreateCall() with
        {
            Status = PortCallStatus.InOperation,
            LastActivityAtUtc = Now,
            BerthedAtUtc = Now.AddHours(-1)
        };
        var delayed = CreateCall() with
        {
            PortCallId = Guid.NewGuid(),
            PublicCode = "ESC-2026-DELAY",
            Status = PortCallStatus.Planned,
            WindowStartsAtUtc = Now.AddHours(-3),
            WindowEndsAtUtc = Now.AddHours(3),
            ArrivedAtAnchorageUtc = null,
            LastActivityAtUtc = Now.AddHours(-3)
        };

        var result = ControlTowerEvaluator.Evaluate(new ControlTowerSnapshot(4, [compliant, delayed]), Now);

        Assert.Equal(2, result.Summary.ActivePortCalls);
        Assert.Equal(1, result.Summary.InOperation);
        Assert.Equal(1, result.Summary.CallsRequiringAttention);
        Assert.Equal(1, result.Summary.OccupiedBerths);
        Assert.Equal(25m, result.Summary.BerthOccupancyPercent);
        Assert.Equal(50m, result.Summary.ScheduleCompliancePercent);
    }

    private static ControlTowerCallSnapshot CreateCall() => new(
        Guid.NewGuid(),
        "ESC-2026-TEST",
        "Navio de teste",
        PortCallStatus.Planned,
        "Porto de teste",
        "Terminal de teste",
        "Berço 01",
        BerthWindowStatus.Confirmed,
        Now.AddHours(-1),
        Now.AddHours(6),
        Now.AddMinutes(-30),
        null,
        null,
        null,
        null,
        Now,
        0,
        0,
        null);
}
