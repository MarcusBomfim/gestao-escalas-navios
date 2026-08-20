using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Vessels;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class VesselConfiguration : IEntityTypeConfiguration<Vessel>
{
    public void Configure(EntityTypeBuilder<Vessel> builder)
    {
        builder.ToTable("vessels");
        builder.HasKey(vessel => vessel.Id);
        builder.Property(vessel => vessel.Id).ValueGeneratedNever();
        builder.Property(vessel => vessel.Name).HasMaxLength(160).IsRequired();
        builder.Property(vessel => vessel.ImoNumber)
            .HasConversion(
                imoNumber => imoNumber == null ? null : imoNumber.Value,
                value => value == null ? null : ImoNumber.Parse(value))
            .HasMaxLength(10);
        builder.Property(vessel => vessel.FlagCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(vessel => vessel.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(vessel => vessel.LengthOverallMeters).HasPrecision(8, 2);
        builder.Property(vessel => vessel.BeamMeters).HasPrecision(7, 2);
        builder.Property(vessel => vessel.MaximumDraftMeters).HasPrecision(6, 2);
        builder.Property(vessel => vessel.CallSign).HasMaxLength(20);
        builder.Property(vessel => vessel.Mmsi).HasMaxLength(9);
        builder.HasIndex(vessel => vessel.ImoNumber)
            .IsUnique()
            .HasFilter("imo_number IS NOT NULL AND is_active");
        builder.HasIndex(vessel => vessel.Name);
    }
}
