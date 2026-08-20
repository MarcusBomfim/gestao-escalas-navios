using PortManagement.Application.Common;

namespace PortManagement.Application.Vessels;

public sealed class GetVesselHandler(IVesselRepository vessels)
{
    public async Task<Result<VesselResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vessel = await vessels.GetByIdAsync(id, cancellationToken);

        return vessel is null
            ? Result.Failure<VesselResponse>(ApplicationErrors.NotFound(
                "vessels.not_found",
                "O navio solicitado não foi encontrado."))
            : Result.Success(vessel.ToResponse());
    }
}
