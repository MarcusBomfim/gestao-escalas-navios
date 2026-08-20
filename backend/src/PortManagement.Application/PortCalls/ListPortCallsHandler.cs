using PortManagement.Application.Common;

namespace PortManagement.Application.PortCalls;

public sealed class ListPortCallsHandler(IPortCallRepository portCalls)
{
    public async Task<Result<PagedResult<PortCallResponse>>> HandleAsync(
        ListPortCallsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page is < 1 or > 1_000_000 || query.PageSize is < 1 or > 100)
        {
            return Result.Failure<PagedResult<PortCallResponse>>(ApplicationErrors.Validation(
                "pagination.invalid",
                "A página deve ser maior que zero e o tamanho deve estar entre 1 e 100."));
        }

        if (query.Search?.Length > 100)
        {
            return Result.Failure<PagedResult<PortCallResponse>>(ApplicationErrors.Validation(
                "search.too_long",
                "O texto de busca deve possuir no máximo 100 caracteres."));
        }

        var result = await portCalls.ListAsync(query, cancellationToken);
        return Result.Success(result);
    }
}
