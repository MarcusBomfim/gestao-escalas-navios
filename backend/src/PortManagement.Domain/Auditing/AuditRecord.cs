using PortManagement.Domain.Common;

namespace PortManagement.Domain.Auditing;

public enum AuditAction
{
    Created,
    Updated,
    Deleted
}

public sealed class AuditRecord : Entity
{
    private AuditRecord()
    {
    }

    private AuditRecord(
        Guid id,
        Guid userId,
        string userDisplayName,
        AuditAction action,
        string entityType,
        string entityId,
        string? changedFields,
        string httpMethod,
        string requestPath,
        string correlationId,
        DateTimeOffset occurredAtUtc)
        : base(id)
    {
        UserId = userId;
        UserDisplayName = DomainRules.RequiredText(userDisplayName, "Usuário", 160);
        Action = action;
        EntityType = DomainRules.RequiredText(entityType, "Tipo da entidade", 120);
        EntityId = DomainRules.RequiredText(entityId, "Identificador da entidade", 100);
        ChangedFields = DomainRules.OptionalText(changedFields, "Campos alterados", 1_000);
        HttpMethod = DomainRules.RequiredText(httpMethod, "Método HTTP", 10);
        RequestPath = DomainRules.RequiredText(requestPath, "Caminho da requisição", 300);
        CorrelationId = DomainRules.RequiredText(correlationId, "Identificador de correlação", 100);
        OccurredAtUtc = DomainRules.ToUtc(occurredAtUtc);
    }

    public Guid UserId { get; private set; }

    public string UserDisplayName { get; private set; } = string.Empty;

    public AuditAction Action { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string? ChangedFields { get; private set; }

    public string HttpMethod { get; private set; } = string.Empty;

    public string RequestPath { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditRecord Capture(
        Guid userId,
        string userDisplayName,
        AuditAction action,
        string entityType,
        string entityId,
        IReadOnlyCollection<string> changedFields,
        string httpMethod,
        string requestPath,
        string correlationId,
        DateTimeOffset occurredAtUtc) =>
        new(
            Guid.NewGuid(),
            userId,
            userDisplayName,
            action,
            entityType,
            entityId,
            changedFields.Count == 0 ? null : string.Join(',', changedFields.Order(StringComparer.Ordinal)),
            httpMethod,
            requestPath,
            correlationId,
            occurredAtUtc);
}
