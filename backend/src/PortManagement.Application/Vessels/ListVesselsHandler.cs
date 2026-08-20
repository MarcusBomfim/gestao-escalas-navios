using PortManagement.Application.Common;

namespace PortManagement.Application.Vessels;

public sealed class ListVesselsHandler(IVesselRepository vessels)
{
    public async Task<Result<PagedResult<VesselResponse>>> HandleAsync(
        ListVesselsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page is < 1 or > 1_000_000 || query.PageSize is < 1 or > 100)
        {
            return Result.Failure<PagedResult<VesselResponse>>(ApplicationErrors.Validation(
                "pagination.invalid",
                "A página deve ser maior que zero e o tamanho deve estar entre 1 e 100."));
        }

        if (query.Search?.Length > 100)
        {
            return Result.Failure<PagedResult<VesselResponse>>(ApplicationErrors.Validation(
                "search.too_long",
                "O texto de busca deve possuir no máximo 100 caracteres."));
        }

        var result = await vessels.ListAsync(query, cancellationToken);
        return Result.Success(result);
    }
}
