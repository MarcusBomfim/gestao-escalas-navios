using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Planning;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class BerthWindowConfiguration : IEntityTypeConfiguration<BerthWindow>
{
    public void Configure(EntityTypeBuilder<BerthWindow> builder)
    {
        builder.ToTable(
            "berth_windows",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_berth_windows_valid_period",
                "ends_at_utc > starts_at_utc"));
        builder.HasKey(window => window.Id);
        builder.Property(window => window.Id).ValueGeneratedNever();
        builder.Property(window => window.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(window => window.RequestedBy).HasMaxLength(120).IsRequired();
        builder.Property(window => window.LastChangedBy).HasMaxLength(120);
        builder.Property(window => window.LastChangeReason).HasMaxLength(500);
        builder.HasIndex(window => new { window.BerthId, window.StartsAtUtc, window.EndsAtUtc });
        builder.HasIndex(window => new { window.PortCallId, window.Status });
        builder.HasOne(window => window.PortCall)
            .WithMany()
            .HasForeignKey(window => window.PortCallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(window => window.Berth)
            .WithMany()
            .HasForeignKey(window => window.BerthId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(window => window.Revisions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
