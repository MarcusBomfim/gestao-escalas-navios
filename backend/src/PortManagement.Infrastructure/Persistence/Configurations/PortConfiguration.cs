using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Ports;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class PortConfiguration : IEntityTypeConfiguration<Port>
{
    public void Configure(EntityTypeBuilder<Port> builder)
    {
        builder.ToTable("ports");
        builder.HasKey(port => port.Id);
        builder.Property(port => port.Id).ValueGeneratedNever();
        builder.Property(port => port.Name).HasMaxLength(160).IsRequired();
        builder.Property(port => port.UnLocode).HasMaxLength(5).IsFixedLength().IsRequired();
        builder.Property(port => port.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(port => port.TimeZoneId).HasMaxLength(80).IsRequired();
        builder.Property(port => port.UpdatedAtUtc).IsConcurrencyToken();
        builder.HasIndex(port => port.UnLocode).IsUnique();
    }
}
