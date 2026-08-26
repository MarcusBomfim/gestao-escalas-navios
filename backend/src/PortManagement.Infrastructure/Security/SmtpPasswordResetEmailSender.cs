using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using PortManagement.Application.Security;

namespace PortManagement.Infrastructure.Security;

internal sealed partial class SmtpPasswordResetEmailSender(
    PasswordRecoveryOptions options,
    ILogger<SmtpPasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public async Task<bool> SendAsync(
        string recipientEmail,
        string displayName,
        string userId,
        string encodedToken,
        CancellationToken cancellationToken)
    {
        var resetLink = PasswordResetLinkBuilder.Build(
            options.PublicWebUrl,
            userId,
            encodedToken);

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = "Redefinição de senha — Port Management",
            Body = $"""
                Olá, {displayName}.

                Recebemos uma solicitação para redefinir a senha da sua conta.
                Use o link abaixo dentro do prazo configurado:

                {resetLink}

                Se você não fez essa solicitação, ignore esta mensagem. Sua senha não será alterada.
                """,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = options.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            client.Credentials = new NetworkCredential(options.Username, options.Password);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (SmtpException exception)
        {
            LogDeliveryFailure(logger, exception.StatusCode);
            return false;
        }
    }

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Error,
        Message = "Não foi possível entregar uma mensagem de recuperação de senha pelo SMTP configurado. Código: {StatusCode}.")]
    private static partial void LogDeliveryFailure(ILogger logger, SmtpStatusCode statusCode);
}

internal static class PasswordResetLinkBuilder
{
    public static string Build(string publicWebUrl, string userId, string encodedToken)
    {
        var baseUrl = publicWebUrl.TrimEnd('/');
        return $"{baseUrl}/redefinir-senha" +
            $"?user={Uri.EscapeDataString(userId)}" +
            $"&token={Uri.EscapeDataString(encodedToken)}";
    }
}
