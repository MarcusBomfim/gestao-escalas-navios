using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.Infrastructure.Identity;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", PortManagementDbContext.IdentitySchema);
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(token => token.CreatedByIp)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(token => token.RevokedByIp)
            .HasMaxLength(64);
        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(64);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
        builder.Property(token => token.RevokedAtUtc)
            .IsConcurrencyToken();

        builder.HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
