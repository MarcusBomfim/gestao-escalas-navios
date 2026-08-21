namespace PortManagement.Application.Common;

public enum ApplicationErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}

public sealed record ApplicationError(
    string Code,
    string Description,
    ApplicationErrorType Type);
