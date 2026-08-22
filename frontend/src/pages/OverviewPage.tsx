import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listPortCalls, listPorts, listVessels } from '../api/portManagement'
import { QueryError, TableSkeleton } from '../components/QueryFeedback'
import { StatusBadge } from '../components/StatusBadge'

const finalStatuses = new Set(['Closed', 'Cancelled'])

export function OverviewPage() {
  const vessels = useQuery({
    queryKey: ['vessels', 'overview'],
    queryFn: () => listVessels({ pageSize: 100 }),
  })
  const portCalls = useQuery({
    queryKey: ['port-calls', 'overview'],
    queryFn: () => listPortCalls({ pageSize: 100 }),
  })
  const ports = useQuery({
    queryKey: ['ports'],
    queryFn: listPorts,
    staleTime: 5 * 60 * 1000,
  })

  const activeCalls = portCalls.data?.items.filter((call) => !finalStatuses.has(call.status)).length ?? 0
  const terminalCount = ports.data?.reduce((total, port) => total + port.terminals.length, 0) ?? 0
  const berthCount = ports.data?.reduce(
    (total, port) => total + port.terminals.reduce(
      (terminalTotal, terminal) => terminalTotal + terminal.berths.length,
      0,
    ),
    0,
  ) ?? 0

  return (
    <div className="page-stack">
      <section className="context-banner">
        <div>
          <span className="live-indicator"><i aria-hidden="true" /> Ambiente conectado</span>
          <h2>Panorama demonstrativo do porto</h2>
          <p>Indicadores calculados a partir dos registros sintéticos carregados pela API.</p>
        </div>
        <span className="context-code">BRSSZ</span>
      </section>

      <section className="metrics-grid" aria-label="Indicadores operacionais">
        <MetricCard label="Escalas cadastradas" value={portCalls.data?.totalItems} loading={portCalls.isPending} code="ES" />
        <MetricCard label="Escalas em andamento" value={activeCalls} loading={portCalls.isPending} code="AT" accent />
        <MetricCard label="Navios ativos" value={vessels.data?.totalItems} loading={vessels.isPending} code="NV" />
        <MetricCard label="Terminais e berços" value={ports.isPending ? undefined : `${terminalCount} / ${berthCount}`} loading={ports.isPending} code="TB" />
      </section>

      {(vessels.isError || portCalls.isError || ports.isError) && (
        <QueryError error={vessels.error ?? portCalls.error ?? ports.error} />
      )}

      <div className="overview-grid">
        <section className="content-card recent-calls">
          <header className="card-heading">
            <div><span>Movimentação</span><h2>Escalas recentes</h2></div>
            <Link to="/escalas">Ver todas →</Link>
          </header>

          {portCalls.isPending ? <TableSkeleton rows={5} /> : (
            <div className="compact-list">
              {portCalls.data?.items.slice(0, 5).map((call) => (
                <article key={call.id}>
                  <span className="vessel-monogram" aria-hidden="true">{getVesselMonogram(call.vesselName)}</span>
                  <div className="compact-primary">
                    <strong>{call.vesselName}</strong>
                    <small>{call.publicCode} · {call.voyageNumber ?? 'Sem viagem'}</small>
                  </div>
                  <div className="compact-secondary">
                    <StatusBadge status={call.status} />
                    <small>{call.plannedBerthName ?? 'Berço a definir'}</small>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="content-card port-structure">
          <header className="card-heading">
            <div><span>Infraestrutura</span><h2>Estrutura portuária</h2></div>
          </header>

          {ports.isPending ? <TableSkeleton rows={3} /> : (
            <div className="structure-list">
              {ports.data?.flatMap((port) => port.terminals.map((terminal) => (
                <article key={terminal.id}>
                  <div><strong>{terminal.name}</strong><small>{terminal.code} · {port.unLocode}</small></div>
                  <span>{terminal.berths.length} berços</span>
                  <ul>
                    {terminal.berths.map((berth) => (
                      <li key={berth.id}>
                        <span>{berth.code}</span>
                        <small>Calado {formatDecimal(berth.maximumDraftMeters)} m</small>
                      </li>
                    ))}
                  </ul>
                </article>
              )))}
            </div>
          )}
        </section>
      </div>
    </div>
  )
}

interface MetricCardProps {
  label: string
  value: number | string | undefined
  loading: boolean
  code: string
  accent?: boolean
}

function MetricCard({ label, value, loading, code, accent = false }: MetricCardProps) {
  return (
    <article className={`metric-card${accent ? ' accent' : ''}`}>
      <span className="metric-code" aria-hidden="true">{code}</span>
      <span>{label}</span>
      {loading ? <i className="metric-skeleton" /> : <strong>{value ?? 0}</strong>}
      <small>Dados do ambiente demo</small>
    </article>
  )
}

function getVesselMonogram(name: string) {
  return name.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()
}

function formatDecimal(value: number) {
  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 }).format(value)
}
