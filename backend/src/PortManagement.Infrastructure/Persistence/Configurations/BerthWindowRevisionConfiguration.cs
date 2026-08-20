using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Planning;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class BerthWindowRevisionConfiguration : IEntityTypeConfiguration<BerthWindowRevision>
{
    public void Configure(EntityTypeBuilder<BerthWindowRevision> builder)
    {
        builder.ToTable("berth_window_revisions");
        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Id).ValueGeneratedNever();
        builder.Property(revision => revision.ChangedBy).HasMaxLength(120).IsRequired();
        builder.Property(revision => revision.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(revision => new { revision.BerthWindowId, revision.ChangedAtUtc });
        builder.HasOne(revision => revision.BerthWindow)
            .WithMany(window => window.Revisions)
            .HasForeignKey(revision => revision.BerthWindowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
