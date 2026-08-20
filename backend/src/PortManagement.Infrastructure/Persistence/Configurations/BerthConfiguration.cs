using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Ports;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class BerthConfiguration : IEntityTypeConfiguration<Berth>
{
    public void Configure(EntityTypeBuilder<Berth> builder)
    {
        builder.ToTable("berths");
        builder.HasKey(berth => berth.Id);
        builder.Property(berth => berth.Id).ValueGeneratedNever();
        builder.Property(berth => berth.Code).HasMaxLength(30).IsRequired();
        builder.Property(berth => berth.Name).HasMaxLength(120).IsRequired();
        builder.Property(berth => berth.UsefulLengthMeters).HasPrecision(8, 2);
        builder.Property(berth => berth.MaximumBeamMeters).HasPrecision(7, 2);
        builder.Property(berth => berth.MaximumDraftMeters).HasPrecision(6, 2);
        builder.Property(berth => berth.SupportedVesselTypes).HasColumnType("integer[]").IsRequired();
        builder.Property(berth => berth.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(berth => new { berth.TerminalId, berth.Code }).IsUnique();
        builder.HasOne(berth => berth.Terminal)
            .WithMany()
            .HasForeignKey(berth => berth.TerminalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
