using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class PortCallEventConfiguration : IEntityTypeConfiguration<PortCallEvent>
{
    public void Configure(EntityTypeBuilder<PortCallEvent> builder)
    {
        builder.ToTable("port_call_events");
        builder.HasKey(portCallEvent => portCallEvent.Id);
        builder.Property(portCallEvent => portCallEvent.Id).ValueGeneratedNever();
        builder.Property(portCallEvent => portCallEvent.Phase).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(portCallEvent => portCallEvent.Action).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(portCallEvent => portCallEvent.Classifier).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(portCallEvent => portCallEvent.Source).HasMaxLength(100).IsRequired();
        builder.Property(portCallEvent => portCallEvent.RecordedBy).HasMaxLength(120).IsRequired();
        builder.Property(portCallEvent => portCallEvent.CorrectionReason).HasMaxLength(500);
        builder.HasIndex(portCallEvent => new
        {
            portCallEvent.PortCallId,
            portCallEvent.Phase,
            portCallEvent.Action,
            portCallEvent.Classifier,
            portCallEvent.OccursAtUtc
        });
        builder.HasOne(portCallEvent => portCallEvent.PortCall)
            .WithMany()
            .HasForeignKey(portCallEvent => portCallEvent.PortCallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(portCallEvent => portCallEvent.ReplacesEvent)
            .WithMany()
            .HasForeignKey(portCallEvent => portCallEvent.ReplacesEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
