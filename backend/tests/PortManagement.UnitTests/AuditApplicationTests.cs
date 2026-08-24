using PortManagement.Application.Auditing;
using PortManagement.Application.Common;
using PortManagement.Domain.Auditing;
using PortManagement.Domain.Common;

namespace PortManagement.UnitTests;

public sealed class AuditApplicationTests
{
    [Fact]
    public void AuditRecordCapturesMetadataWithoutPersistingChangedValues()
    {
        var userId = Guid.NewGuid();
        var record = AuditRecord.Capture(
            userId,
            "Administrador Demo",
            AuditAction.Updated,
            "Vessel",
            Guid.NewGuid().ToString(),
            ["Name", "MaximumDraftMeters"],
            "PUT",
            "/api/v1/vessels/123",
            "trace-123",
            new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(userId, record.UserId);
        Assert.Equal("MaximumDraftMeters,Name", record.ChangedFields);
        Assert.DoesNotContain("Administrador Demo", record.ChangedFields, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditRecordRejectsEmptyCorrelationIdentifier()
    {
        Assert.Throws<DomainException>(() => AuditRecord.Capture(
            Guid.NewGuid(),
            "Administrador",
            AuditAction.Created,
            "PortCall",
            Guid.NewGuid().ToString(),
            [],
            "POST",
            "/api/v1/port-calls",
            " ",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CsvExportNeutralizesSpreadsheetFormulas()
    {
        var row = new AuditLogResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "=HYPERLINK(\"https://invalid\")",
            AuditAction.Updated,
            "Vessel",
            Guid.NewGuid().ToString(),
            ["Name"],
            "PUT",
            "/api/v1/vessels/123",
            "trace-123",
            new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero));

        var csv = CsvReportBuilder.BuildAuditLog([row]);

        Assert.Contains("\"'=HYPERLINK(\"\"https://invalid\"\")\"", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditQueryRejectsAnInvertedPeriod()
    {
        var repository = new EmptyAuditRepository();
        var handler = new GetAuditLogHandler(repository);

        var result = await handler.HandleAsync(
            new AuditLogQuery(
                1,
                20,
                null,
                null,
                new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("audit.invalid_period", result.Error?.Code);
    }

    private sealed class EmptyAuditRepository : IAuditLogRepository
    {
        public Task<PagedResult<AuditLogResponse>> ListAsync(
            AuditLogQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<AuditLogResponse>([], query.Page, query.PageSize, 0));

        public Task<IReadOnlyCollection<AuditLogResponse>> ExportAsync(
            AuditLogQuery query,
            int maximumRows,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<AuditLogResponse>>([]);
    }
}
