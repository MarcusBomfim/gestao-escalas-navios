using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class PortCallConfiguration : IEntityTypeConfiguration<PortCall>
{
    public void Configure(EntityTypeBuilder<PortCall> builder)
    {
        builder.ToTable("port_calls");
        builder.HasKey(portCall => portCall.Id);
        builder.Property(portCall => portCall.Id).ValueGeneratedNever();
        builder.Property(portCall => portCall.PublicCode).HasMaxLength(24).IsRequired();
        builder.Property(portCall => portCall.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(portCall => portCall.Purpose).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(portCall => portCall.VoyageNumber).HasMaxLength(50);
        builder.Property(portCall => portCall.PreviousPortUnLocode).HasMaxLength(5).IsFixedLength();
        builder.Property(portCall => portCall.NextPortUnLocode).HasMaxLength(5).IsFixedLength();
        builder.Property(portCall => portCall.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(portCall => portCall.Version).IsConcurrencyToken();
        builder.HasIndex(portCall => portCall.PublicCode).IsUnique();
        builder.HasIndex(portCall => portCall.IdempotencyKey).IsUnique();
        builder.HasIndex(portCall => new { portCall.PortId, portCall.Status });
        builder.HasIndex(portCall => new { portCall.PlannedBerthId, portCall.Status });

        builder.HasOne(portCall => portCall.Vessel)
            .WithMany()
            .HasForeignKey(portCall => portCall.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(portCall => portCall.Port)
            .WithMany()
            .HasForeignKey(portCall => portCall.PortId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(portCall => portCall.AgentOrganization)
            .WithMany()
            .HasForeignKey(portCall => portCall.AgentOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(portCall => portCall.ShippingLineOrganization)
            .WithMany()
            .HasForeignKey(portCall => portCall.ShippingLineOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(portCall => portCall.PlannedTerminal)
            .WithMany()
            .HasForeignKey(portCall => portCall.PlannedTerminalId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(portCall => portCall.PlannedBerth)
            .WithMany()
            .HasForeignKey(portCall => portCall.PlannedBerthId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(portCall => portCall.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
