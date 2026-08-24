using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Domain.Notifications;
using PortManagement.Infrastructure.Identity;

namespace PortManagement.Infrastructure.Persistence.Configurations;

internal sealed class NotificationReceiptConfiguration : IEntityTypeConfiguration<NotificationReceipt>
{
    public void Configure(EntityTypeBuilder<NotificationReceipt> builder)
    {
        builder.ToTable("notification_receipts");
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Id).ValueGeneratedNever();
        builder.Property(receipt => receipt.AlertId).HasMaxLength(160).IsRequired();
        builder.HasIndex(receipt => new { receipt.UserId, receipt.AlertId }).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(receipt => receipt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
