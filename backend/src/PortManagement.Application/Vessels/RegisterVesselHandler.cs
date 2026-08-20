using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Vessels;

namespace PortManagement.Application.Vessels;

public sealed class RegisterVesselHandler(
    IVesselRepository vessels,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<VesselResponse>> HandleAsync(
        RegisterVesselCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var imoNumber = string.IsNullOrWhiteSpace(command.ImoNumber)
                ? null
                : ImoNumber.Parse(command.ImoNumber);

            if (imoNumber is not null
                && await vessels.ActiveImoExistsAsync(imoNumber, cancellationToken))
            {
                return Result.Failure<VesselResponse>(ApplicationErrors.Conflict(
                    "vessels.imo_already_exists",
                    "Já existe um navio ativo com o número IMO informado."));
            }

            var vessel = new Vessel(
                Guid.NewGuid(),
                command.Name,
                imoNumber,
                command.FlagCode,
                command.Type,
                command.LengthOverallMeters,
                command.BeamMeters,
                command.MaximumDraftMeters,
                DateTimeOffset.UtcNow,
                command.CallSign,
                command.Mmsi);

            await vessels.AddAsync(vessel, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (UniqueConstraintException exception)
                when (exception.ConstraintName == "ix_vessels_imo_number")
            {
                return Result.Failure<VesselResponse>(ApplicationErrors.Conflict(
                    "vessels.imo_already_exists",
                    "Já existe um navio ativo com o número IMO informado."));
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
