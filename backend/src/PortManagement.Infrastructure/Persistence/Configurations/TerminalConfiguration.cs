using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Ports;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class TerminalConfiguration : IEntityTypeConfiguration<Terminal>
{
    public void Configure(EntityTypeBuilder<Terminal> builder)
    {
        builder.ToTable("terminals");
        builder.HasKey(terminal => terminal.Id);
        builder.Property(terminal => terminal.Id).ValueGeneratedNever();
        builder.Property(terminal => terminal.Code).HasMaxLength(30).IsRequired();
        builder.Property(terminal => terminal.Name).HasMaxLength(160).IsRequired();
        builder.Property(terminal => terminal.TimeZoneId).HasMaxLength(80).IsRequired();
        builder.Property(terminal => terminal.UpdatedAtUtc).IsConcurrencyToken();
        builder.HasIndex(terminal => new { terminal.PortId, terminal.Code }).IsUnique();
        builder.HasOne(terminal => terminal.Port)
            .WithMany()
            .HasForeignKey(terminal => terminal.PortId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
