import { ApiError } from '../api/client'

export function QueryError({ error }: { error: unknown }) {
  const message = error instanceof ApiError
    ? error.message
    : 'Não foi possível carregar os dados agora.'

  return (
    <div className="query-feedback error" role="alert">
      <strong>Falha ao consultar a API</strong>
      <p>{message}</p>
      {error instanceof ApiError && error.correlationId && <small>Referência: {error.correlationId}</small>}
    </div>
  )
}

export function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <div className="query-feedback empty">
      <strong>{title}</strong>
      <p>{description}</p>
    </div>
  )
}

export function TableSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="table-skeleton" aria-label="Carregando registros" role="status">
      {Array.from({ length: rows }, (_, index) => <span key={index} />)}
    </div>
  )
}
