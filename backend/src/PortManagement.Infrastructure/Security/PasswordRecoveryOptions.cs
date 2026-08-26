namespace PortManagement.Infrastructure.Security;

public sealed class PasswordRecoveryOptions
{
    public string SmtpHost { get; init; } = "localhost";

    public int SmtpPort { get; init; } = 1025;

    public bool EnableSsl { get; init; }

    public string FromAddress { get; init; } = "no-reply@portmanagement.local";

    public string FromName { get; init; } = "Port Management";

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string PublicWebUrl { get; init; } = "http://localhost:5173";

    public int TokenLifetimeMinutes { get; init; } = 30;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(SmtpHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(FromAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(FromName);
        ArgumentException.ThrowIfNullOrWhiteSpace(PublicWebUrl);

        if (SmtpPort is < 1 or > 65_535)
        {
            throw new InvalidOperationException("A porta SMTP deve estar entre 1 e 65535.");
        }

        if (!Uri.TryCreate(PublicWebUrl, UriKind.Absolute, out var publicUri) ||
            (publicUri.Scheme != Uri.UriSchemeHttp && publicUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("A URL pública da interface deve usar HTTP ou HTTPS.");
        }

        if (TokenLifetimeMinutes is < 5 or > 1_440)
        {
            throw new InvalidOperationException(
                "A validade do token de recuperação deve estar entre 5 e 1440 minutos.");
        }

        if (string.IsNullOrWhiteSpace(Username) != string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException(
                "Usuário e senha SMTP devem ser configurados em conjunto.");
        }
    }
}
