using Microsoft.AspNetCore.Identity;

namespace PortManagement.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    public Guid? OrganizationId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; } = [];
}

public sealed class ApplicationRole : IdentityRole<Guid>;
