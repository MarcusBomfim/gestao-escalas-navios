using System.Text;

namespace PortManagement.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 7;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(SigningKey);

        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey deve possuir pelo menos 32 bytes e vir de uma variável de ambiente.");
        }

        if (AccessTokenMinutes is < 5 or > 60)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes deve estar entre 5 e 60.");
        }

        if (RefreshTokenDays is < 1 or > 90)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenDays deve estar entre 1 e 90.");
        }
    }
}
