using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Auditing;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.UserDisplayName).HasMaxLength(160).IsRequired();
        builder.Property(record => record.Action).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(record => record.EntityType).HasMaxLength(120).IsRequired();
        builder.Property(record => record.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(record => record.ChangedFields).HasMaxLength(1_000);
        builder.Property(record => record.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(record => record.RequestPath).HasMaxLength(300).IsRequired();
        builder.Property(record => record.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(record => record.OccurredAtUtc).IsRequired();
        builder.HasIndex(record => record.OccurredAtUtc);
        builder.HasIndex(record => new { record.UserId, record.OccurredAtUtc });
        builder.HasIndex(record => new { record.EntityType, record.OccurredAtUtc });
    }
}
