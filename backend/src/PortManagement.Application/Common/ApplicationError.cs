namespace PortManagement.Application.Common;

public enum ApplicationErrorType
{
    Validation,
    NotFound,
    Conflict
}

public sealed record ApplicationError(
    string Code,
    string Description,
    ApplicationErrorType Type);
