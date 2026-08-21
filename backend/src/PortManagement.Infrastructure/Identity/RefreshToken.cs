namespace PortManagement.Infrastructure.Identity;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string createdByIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByIp);

        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public string CreatedByIp { get; private set; } = string.Empty;

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    public bool IsActive(DateTimeOffset now) =>
        RevokedAtUtc is null && ExpiresAtUtc > now;

    public void Revoke(
        DateTimeOffset revokedAtUtc,
        string revokedByIp,
        string? replacedByTokenHash = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(revokedByIp);

        RevokedAtUtc = revokedAtUtc;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
