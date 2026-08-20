using PortManagement.Application.Common;

namespace PortManagement.Application.PortCalls;

public sealed class GetPortCallHandler(IPortCallRepository portCalls)
{
    public async Task<Result<PortCallResponse>> HandleAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var portCall = await portCalls.GetDetailsByPublicCodeAsync(
            publicCode.Trim().ToUpperInvariant(),
            cancellationToken);

        return portCall is null
            ? Result.Failure<PortCallResponse>(ApplicationErrors.NotFound(
                "port_calls.not_found",
                "A escala solicitada não foi encontrada."))
            : Result.Success(portCall);
    }
}
