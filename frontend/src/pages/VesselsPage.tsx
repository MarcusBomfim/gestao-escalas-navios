import { useState, type FormEvent } from 'react'
import { useQuery } from '@tanstack/react-query'
import { listVessels } from '../api/portManagement'
import { EmptyState, QueryError, TableSkeleton } from '../components/QueryFeedback'
import { Pagination } from '../components/Pagination'
import { useAuth } from '../auth/AuthContext'

const vesselTypeLabels: Record<string, string> = {
  ContainerShip: 'Porta-contêineres',
  BulkCarrier: 'Graneleiro',
  GeneralCargo: 'Carga geral',
  Tanker: 'Petroleiro',
  RoRo: 'Ro-Ro',
  Passenger: 'Passageiros',
  Other: 'Outro',
}

export function VesselsPage() {
  const { hasAnyRole } = useAuth()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const vessels = useQuery({
    queryKey: ['vessels', { page, search }],
    queryFn: () => listVessels({ page, pageSize: 10, search }),
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
          <p>Frota de referência</p>
          <h2>{vessels.data?.totalItems ?? '—'} navios ativos cadastrados</h2>
          <span>Características dimensionais usadas no planejamento das escalas.</span>
        </div>
        {hasAnyRole('Administrator', 'Planner') && (
          <span className="permission-note">Seu perfil permite cadastrar navios</span>
        )}
      </section>

      <section className="content-card data-card">
        <header className="data-toolbar">
          <form className="search-form" onSubmit={handleSearch}>
            <label className="sr-only" htmlFor="vessel-search">Buscar navio</label>
            <input
              id="vessel-search"
              type="search"
              placeholder="Buscar por nome, IMO ou indicativo…"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
            />
            <button type="submit">Buscar</button>
          </form>
          {search && <button className="clear-filter" type="button" onClick={() => { setSearch(''); setSearchInput('') }}>Limpar filtro</button>}
        </header>

        {vessels.isPending && <TableSkeleton rows={7} />}
        {vessels.isError && <QueryError error={vessels.error} />}
        {vessels.data?.items.length === 0 && (
          <EmptyState title="Nenhum navio encontrado" description="Altere o termo utilizado na busca." />
        )}

        {vessels.data && vessels.data.items.length > 0 && (
          <div className="table-scroll">
            <table>
              <thead><tr><th>Navio</th><th>Tipo</th><th>Bandeira</th><th>Dimensões</th><th>Calado máx.</th><th>Situação</th></tr></thead>
              <tbody>
                {vessels.data.items.map((vessel) => (
                  <tr key={vessel.id}>
                    <td><strong>{vessel.name}</strong><small>{vessel.imoNumber ?? vessel.callSign ?? 'Sem identificação externa'}</small></td>
                    <td>{vesselTypeLabels[vessel.type] ?? vessel.type}</td>
                    <td><span className="flag-code">{vessel.flagCode}</span></td>
                    <td>{formatMeasurement(vessel.lengthOverallMeters)} × {formatMeasurement(vessel.beamMeters)} m</td>
                    <td>{formatMeasurement(vessel.maximumDraftMeters)} m</td>
                    <td><span className={`record-state ${vessel.isActive ? 'active' : ''}`}>{vessel.isActive ? 'Ativo' : 'Inativo'}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <Pagination page={page} totalPages={vessels.data?.totalPages ?? 0} onChange={setPage} />
      </section>
    </div>
  )
}

function formatMeasurement(value: number) {
  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 }).format(value)
}
