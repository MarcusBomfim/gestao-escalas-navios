using PortManagement.Domain.Common;
using PortManagement.Domain.Operations;

namespace PortManagement.UnitTests;

public sealed class CargoOperationTests
{
    [Fact]
    public void ScheduleRejectsAnEndBeforeTheStart()
    {
        var operation = CreateOperation();
        var start = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<DomainException>(() =>
            operation.Schedule(start, start.AddMinutes(-1), start));

        Assert.Contains("posterior", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartRecordsTheActualTimestamp()
    {
        var operation = CreateOperation();
        var start = DateTimeOffset.UtcNow;

        operation.Start(start, start);

        Assert.Equal(start, operation.ActualStartAtUtc);
        Assert.Null(operation.ActualEndAtUtc);
    }

    [Fact]
    public void StartCannotBeRecordedTwice()
    {
        var operation = CreateOperation();
        var start = DateTimeOffset.UtcNow;
        operation.Start(start, start);

        Assert.Throws<DomainException>(() => operation.Start(start.AddMinutes(1), start.AddMinutes(1)));
    }

    [Fact]
    public void CompleteRequiresTheOperationToBeStarted()
    {
        var operation = CreateOperation();

        Assert.Throws<DomainException>(() =>
            operation.Complete(90, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CompleteRecordsQuantityAndEndTimestamp()
    {
        var operation = CreateOperation();
        var start = DateTimeOffset.UtcNow;
        operation.Start(start, start);

        operation.Complete(98.5m, start.AddHours(2), start.AddHours(2));

        Assert.Equal(98.5m, operation.ActualQuantity);
        Assert.Equal(start.AddHours(2), operation.ActualEndAtUtc);
    }

    private static CargoOperation CreateOperation() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        CargoOperationDirection.Loading,
        "Carga de teste",
        100,
        CargoQuantityUnit.MetricTon,
        false,
        DateTimeOffset.UtcNow);
}
