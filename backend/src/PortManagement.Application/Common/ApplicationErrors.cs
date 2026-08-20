namespace PortManagement.Application.Common;

public static class ApplicationErrors
{
    public static ApplicationError Validation(string code, string description) =>
        new(code, description, ApplicationErrorType.Validation);

    public static ApplicationError NotFound(string code, string description) =>
        new(code, description, ApplicationErrorType.NotFound);

    public static ApplicationError Conflict(string code, string description) =>
        new(code, description, ApplicationErrorType.Conflict);
}
