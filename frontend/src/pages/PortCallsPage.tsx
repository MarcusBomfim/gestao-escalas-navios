import { useState, type FormEvent } from 'react'
import { useQuery } from '@tanstack/react-query'
import { listPortCalls } from '../api/portManagement'
import { EmptyState, QueryError, TableSkeleton } from '../components/QueryFeedback'
import { Pagination } from '../components/Pagination'
import { StatusBadge } from '../components/StatusBadge'
import { getStatusLabel } from '../components/statusLabels'
import { useAuth } from '../auth/AuthContext'

const statuses = [
  'Draft', 'Requested', 'UnderReview', 'Planned', 'AtAnchorage',
  'ClearedForBerthing', 'Berthed', 'InOperation', 'OperationCompleted',
  'Unberthed', 'Closed', 'Cancelled',
]

export function PortCallsPage() {
  const { hasAnyRole } = useAuth()
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const portCalls = useQuery({
    queryKey: ['port-calls', { page, search, status }],
    queryFn: () => listPortCalls({ page, pageSize: 10, search, status }),
  })

  const handleSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSearch(searchInput.trim())
    setPage(1)
  }

  return (
    <div className="page-stack">
      <section className="page-introduction">
        <div>
          <p>Programação de navios</p>
          <h2>{portCalls.data?.totalItems ?? '—'} escalas registradas</h2>
          <span>Consulte a situação, o trajeto e o planejamento de atracação.</span>
        </div>
        {hasAnyRole('Administrator', 'Planner', 'Operator') && (
          <span className="permission-note">Seu perfil permite atuar nas escalas</span>
        )}
      </section>

      <section className="content-card data-card">
        <header className="data-toolbar multi-filter">
          <form className="search-form" onSubmit={handleSearch}>
            <label className="sr-only" htmlFor="call-search">Buscar escala</label>
            <input
              id="call-search"
              type="search"
              placeholder="Código, navio ou viagem…"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
            />
            <button type="submit">Buscar</button>
          </form>
          <label className="select-filter">
            <span className="sr-only">Filtrar por situação</span>
            <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
              <option value="">Todas as situações</option>
              {statuses.map((option) => <option key={option} value={option}>{getStatusLabel(option)}</option>)}
            </select>
          </label>
        </header>

        {portCalls.isPending && <TableSkeleton rows={7} />}
        {portCalls.isError && <QueryError error={portCalls.error} />}
        {portCalls.data?.items.length === 0 && (
          <EmptyState title="Nenhuma escala encontrada" description="Ajuste a busca ou selecione outra situação." />
        )}

        {portCalls.data && portCalls.data.items.length > 0 && (
          <div className="table-scroll">
            <table>
              <thead><tr><th>Escala</th><th>Navio</th><th>Situação</th><th>Terminal / berço</th><th>Rota</th><th>Atualização</th></tr></thead>
              <tbody>
                {portCalls.data.items.map((call) => (
                  <tr key={call.id}>
                    <td><strong>{call.publicCode}</strong><small>{call.voyageNumber ?? 'Viagem não informada'}</small></td>
                    <td><strong>{call.vesselName}</strong><small>{call.portName}</small></td>
                    <td><StatusBadge status={call.status} /></td>
                    <td>{call.plannedTerminalName ?? 'A definir'}<small>{call.plannedBerthName ?? 'Sem berço'}</small></td>
                    <td><span className="route-code">{call.previousPortUnLocode ?? '—'} → {call.nextPortUnLocode ?? '—'}</span></td>
                    <td>{formatDate(call.updatedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <Pagination page={page} totalPages={portCalls.data?.totalPages ?? 0} onChange={setPage} />
      </section>
    </div>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}
