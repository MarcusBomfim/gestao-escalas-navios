using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class PortCallStatusHistoryConfiguration : IEntityTypeConfiguration<PortCallStatusHistory>
{
    public void Configure(EntityTypeBuilder<PortCallStatusHistory> builder)
    {
        builder.ToTable("port_call_status_history");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();
        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(history => history.ChangedBy).HasMaxLength(120).IsRequired();
        builder.Property(history => history.Reason).HasMaxLength(500);
        builder.HasIndex(history => new { history.PortCallId, history.ChangedAtUtc });
        builder.HasOne(history => history.PortCall)
            .WithMany(portCall => portCall.StatusHistory)
            .HasForeignKey(history => history.PortCallId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
