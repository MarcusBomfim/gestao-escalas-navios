using PortManagement.Application.Common;

namespace PortManagement.Application.Planning;

public sealed class GetPortCallBerthWindowHandler(IBerthWindowRepository windows)
{
    public async Task<Result<PortCallBerthWindowResponse>> HandleAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var normalizedCode = publicCode.Trim().ToUpperInvariant();
        var portCallReference = await windows.FindPortCallForPlanningAsync(normalizedCode, cancellationToken);
        if (portCallReference is null)
        {
            return Result.Failure<PortCallBerthWindowResponse>(ApplicationErrors.NotFound(
                "port_calls.not_found",
                "A escala solicitada não foi encontrada."));
        }

        var window = await windows.GetActiveDetailsByPublicCodeAsync(normalizedCode, cancellationToken);
        return Result.Success(new PortCallBerthWindowResponse(window));
    }
}

public sealed class ListBerthWindowsHandler(IBerthWindowRepository windows)
{
    public async Task<Result<PagedResult<BerthWindowResponse>>> HandleAsync(
        ListBerthWindowsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page is < 1 or > 1_000_000 || query.PageSize is < 1 or > 100)
        {
            return Result.Failure<PagedResult<BerthWindowResponse>>(ApplicationErrors.Validation(
                "pagination.invalid",
                "Página e tamanho de página devem estar dentro dos limites permitidos."));
        }

        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.ToUtc <= query.FromUtc)
        {
            return Result.Failure<PagedResult<BerthWindowResponse>>(ApplicationErrors.Validation(
                "planning.invalid_period_filter",
                "O fim do período consultado deve ser posterior ao início."));
        }

        return Result.Success(await windows.ListAsync(query, cancellationToken));
    }
}
