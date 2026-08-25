namespace PortManagement.Api.Resilience;

public sealed class ApiResilienceOptions
{
    public int RequestTimeoutSeconds { get; init; } = 30;

    public int ShutdownTimeoutSeconds { get; init; } = 30;

    public void Validate()
    {
        if (RequestTimeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException(
                "Resilience:RequestTimeoutSeconds deve estar entre 1 e 300 segundos.");
        }

        if (ShutdownTimeoutSeconds is < 1 or > 120)
        {
            throw new InvalidOperationException(
                "Resilience:ShutdownTimeoutSeconds deve estar entre 1 e 120 segundos.");
        }
    }
}
