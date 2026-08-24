using PortManagement.Domain.Common;

namespace PortManagement.Domain.Notifications;

public sealed class NotificationReceipt : Entity
{
    private NotificationReceipt()
    {
        AlertId = string.Empty;
    }

    public NotificationReceipt(
        Guid id,
        Guid userId,
        string alertId,
        DateTimeOffset readAtUtc)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("O usuário da leitura é obrigatório.");
        }

        UserId = userId;
        AlertId = DomainRules.RequiredText(alertId, "Identificador do alerta", 160);
        ReadAtUtc = DomainRules.ToUtc(readAtUtc);
    }

    public Guid UserId { get; private set; }

    public string AlertId { get; private set; }

    public DateTimeOffset ReadAtUtc { get; private set; }
}
