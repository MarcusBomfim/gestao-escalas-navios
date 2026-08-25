namespace PortManagement.Infrastructure.Resilience;

public sealed class DatabaseResilienceOptions
{
    public int CommandTimeoutSeconds { get; init; } = 30;

    public int MaxRetryCount { get; init; } = 3;

    public int MaxRetryDelaySeconds { get; init; } = 5;

    public void Validate()
    {
        if (CommandTimeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException(
                "Resilience:Database:CommandTimeoutSeconds deve estar entre 1 e 300 segundos.");
        }

        if (MaxRetryCount is < 0 or > 10)
        {
            throw new InvalidOperationException(
                "Resilience:Database:MaxRetryCount deve estar entre 0 e 10.");
        }

        if (MaxRetryDelaySeconds is < 1 or > 60)
        {
            throw new InvalidOperationException(
                "Resilience:Database:MaxRetryDelaySeconds deve estar entre 1 e 60 segundos.");
        }
    }
}
