using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Vessels;

namespace PortManagement.Application.Vessels;

public sealed class UpdateVesselHandler(
    IVesselRepository vessels,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<VesselResponse>> HandleAsync(
        UpdateVesselCommand command,
        CancellationToken cancellationToken)
    {
        var vessel = await vessels.FindTrackedByIdAsync(command.Id, cancellationToken);
        if (vessel is null)
        {
            return Result.Failure<VesselResponse>(ApplicationErrors.NotFound(
                "vessels.not_found",
                "O navio solicitado não foi encontrado."));
        }

        try
        {
            var imoNumber = string.IsNullOrWhiteSpace(command.ImoNumber)
                ? null
                : ImoNumber.Parse(command.ImoNumber);

            if (imoNumber is not null
                && await vessels.ActiveImoExistsAsync(imoNumber, vessel.Id, cancellationToken))
            {
                return Result.Failure<VesselResponse>(ApplicationErrors.Conflict(
                    "vessels.imo_already_exists",
                    "Já existe outro navio ativo com o número IMO informado."));
            }

            vessel.UpdateDetails(
                command.Name,
                imoNumber,
                command.FlagCode,
                command.Type,
                command.LengthOverallMeters,
                command.BeamMeters,
                command.MaximumDraftMeters,
                command.CallSign,
                command.Mmsi,
                DateTimeOffset.UtcNow);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (UniqueConstraintException exception)
                when (exception.ConstraintName == "ix_vessels_imo_number")
            {
                return Result.Failure<VesselResponse>(ApplicationErrors.Conflict(
                    "vessels.imo_already_exists",
                    "Já existe outro navio ativo com o número IMO informado."));
            }

            return Result.Success(vessel.ToResponse());
        }
        catch (DomainException exception)
        {
            return Result.Failure<VesselResponse>(ApplicationErrors.Validation(
                "vessels.invalid_data",
                exception.Message));
        }
    }
}
