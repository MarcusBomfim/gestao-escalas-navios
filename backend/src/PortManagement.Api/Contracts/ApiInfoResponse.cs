namespace PortManagement.Api.Contracts;

public sealed record ApiInfoResponse(
    string Name,
    string Version,
    string Status,
    DateTimeOffset Timestamp);

