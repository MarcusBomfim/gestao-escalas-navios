namespace PortManagement.Application.ReferenceData;

public sealed class GetPortStructureHandler(IPortStructureRepository ports)
{
    public Task<IReadOnlyCollection<PortReferenceResponse>> HandleAsync(
        CancellationToken cancellationToken) =>
        ports.ListActiveAsync(cancellationToken);
}
