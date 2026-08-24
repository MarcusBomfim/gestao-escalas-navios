using PortManagement.Application.ControlTower;
using PortManagement.Application.Notifications;
using PortManagement.Domain.Common;
using PortManagement.Domain.Notifications;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;

namespace PortManagement.UnitTests;

public sealed class NotificationApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReceiptRejectsAnEmptyUser()
    {
        Assert.Throws<DomainException>(() =>
            new NotificationReceipt(Guid.NewGuid(), Guid.Empty, "alert:test", Now));
    }

    [Fact]
    public async Task NotificationCenterMergesPersistentReadState()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext();
        var alertId = context.Tower.Alerts.Single().Id;
        context.Receipts.Seed(userId, alertId, Now.AddMinutes(-5));

        var result = await context.Center.HandleAsync(userId, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.True(item.IsRead);
        Assert.Equal(0, result.Value.UnreadCount);
    }

    [Fact]
    public async Task MarkReadIsIdempotentAndReturnsUpdatedCenter()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext();
        var alertId = context.Tower.Alerts.Single().Id;
        var handler = new MarkNotificationReadHandler(
            context.TowerHandler,
            context.Center,
            context.Receipts,
            new FixedTimeProvider(Now));

        var first = await handler.HandleAsync(userId, alertId, TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(userId, alertId, TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value!.UnreadCount);
        Assert.Single(context.Receipts.Stored);
    }

    [Fact]
    public async Task MarkReadRejectsAnAlertThatIsNoLongerActive()
    {
        var context = CreateContext();
        var handler = new MarkNotificationReadHandler(
            context.TowerHandler,
            context.Center,
            context.Receipts,
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            "inactive-alert",
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("notifications.alert_not_found", result.Error?.Code);
    }

    private static NotificationContext CreateContext()
    {
        var snapshot = new ControlTowerSnapshot(
            2,
            [new ControlTowerCallSnapshot(
                Guid.NewGuid(),
                "ESC-2026-NOTIFICATION",
                "Navio Notificação",
                PortCallStatus.UnderReview,
                "Porto de teste",
                "Terminal de teste",
                "Berço 01",
                BerthWindowStatus.Requested,
                Now.AddHours(-2),
                Now.AddHours(5),
                null,
                null,
                null,
                null,
                null,
                Now.AddHours(-3),
                0,
                0,
                null)]);
        var tower = ControlTowerEvaluator.Evaluate(snapshot, Now);
        var towerHandler = new GetControlTowerHandler(
            new ControlTowerRepositoryFake(snapshot),
            new FixedTimeProvider(Now));
        var receipts = new NotificationReceiptRepositoryFake();
        var center = new GetNotificationCenterHandler(towerHandler, receipts);
        return new NotificationContext(tower, towerHandler, receipts, center);
    }

    private sealed record NotificationContext(
        ControlTowerResponse Tower,
        GetControlTowerHandler TowerHandler,
        NotificationReceiptRepositoryFake Receipts,
        GetNotificationCenterHandler Center);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class ControlTowerRepositoryFake(ControlTowerSnapshot snapshot) : IControlTowerRepository
    {
        public Task<ControlTowerSnapshot> GetSnapshotAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class NotificationReceiptRepositoryFake : INotificationReceiptRepository
    {
        public Dictionary<(Guid UserId, string AlertId), DateTimeOffset> Stored { get; } = [];

        public void Seed(Guid userId, string alertId, DateTimeOffset readAtUtc) =>
            Stored[(userId, alertId)] = readAtUtc;

        public Task<IReadOnlyCollection<NotificationReceiptResponse>> ListAsync(
            Guid userId,
            IReadOnlyCollection<string> alertIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<NotificationReceiptResponse> result = Stored
                .Where(pair => pair.Key.UserId == userId && alertIds.Contains(pair.Key.AlertId))
                .Select(pair => new NotificationReceiptResponse(pair.Key.AlertId, pair.Value))
                .ToArray();
            return Task.FromResult(result);
        }

        public Task MarkReadAsync(
            Guid userId,
            IReadOnlyCollection<string> alertIds,
            DateTimeOffset readAtUtc,
            CancellationToken cancellationToken)
        {
            foreach (var alertId in alertIds)
            {
                Stored[(userId, alertId)] = readAtUtc;
            }

            return Task.CompletedTask;
        }
    }
}
