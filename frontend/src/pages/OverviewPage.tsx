import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getControlTower } from '../api/portManagement'
import type {
  ControlTowerCall,
  OperationalAlert,
  OperationalAlertSeverity,
  OperationalMilestone,
} from '../api/types'
import { QueryError, TableSkeleton } from '../components/QueryFeedback'
import { StatusBadge } from '../components/StatusBadge'

type AlertFilter = 'All' | OperationalAlertSeverity

const severityLabels: Record<OperationalAlertSeverity, string> = {
  Critical: 'Crítico',
  Warning: 'Atenção',
  Info: 'Informativo',
}

const milestoneLabels: Record<OperationalMilestone, string> = {
  ArrivedAtAnchorage: 'Chegada ao fundeadouro',
  PilotageStarted: 'Início da praticagem',
  BerthingCompleted: 'Atracação concluída',
  CargoOperationStarted: 'Início da operação',
  CargoOperationCompleted: 'Conclusão da operação',
  UnberthingCompleted: 'Desatracação concluída',
  Departed: 'Saída do porto',
}

export function OverviewPage() {
  const [alertFilter, setAlertFilter] = useState<AlertFilter>('All')
  const tower = useQuery({
    queryKey: ['control-tower'],
    queryFn: getControlTower,
    refetchInterval: 60_000,
  })

  if (tower.isPending) {
    return <div className="page-stack"><section className="content-card tower-loading"><TableSkeleton rows={8} /></section></div>
  }

  if (tower.isError) {
    return <div className="page-stack"><QueryError error={tower.error} /></div>
  }

  const data = tower.data
  const visibleAlerts = alertFilter === 'All'
    ? data.alerts
    : data.alerts.filter((alert) => alert.severity === alertFilter)

  return (
    <div className="page-stack">
      <section className="context-banner control-banner">
        <div>
          <span className="live-indicator"><i aria-hidden="true" /> Monitoramento ativo</span>
          <h2>Torre de controle operacional</h2>
          <p>Prioridades calculadas a partir do planejamento, eventos realizados e movimentações de carga.</p>
        </div>
        <div className="control-compliance">
          <strong>{formatPercent(data.summary.scheduleCompliancePercent)}</strong>
          <span>Aderência à programação</span>
          <small>Atualizado às {formatTime(data.generatedAtUtc)}</small>
        </div>
      </section>

      <section className="metrics-grid" aria-label="Indicadores da torre de controle">
        <MetricCard label="Escalas ativas" value={data.summary.activePortCalls} code="EA" detail="Fluxos ainda não encerrados" />
        <MetricCard label="Em operação" value={data.summary.inOperation} code="OP" detail="Navios movimentando carga" />
        <MetricCard label="Requerem atenção" value={data.summary.callsRequiringAttention} code="AL" detail={`${data.summary.criticalAlerts} alerta(s) crítico(s)`} danger={data.summary.criticalAlerts > 0} />
        <MetricCard label="Ocupação de berços" value={formatPercent(data.summary.berthOccupancyPercent)} code="OB" detail={`${data.summary.occupiedBerths} de ${data.summary.totalBerths} berços`} accent />
      </section>

      <div className="tower-grid">
        <section className="content-card alert-queue">
          <header className="card-heading tower-heading">
            <div><span>Prioridades</span><h2>Fila de alertas</h2></div>
            <strong>{data.alerts.length}</strong>
          </header>

          <div className="alert-filters" aria-label="Filtrar alertas">
            {(['All', 'Critical', 'Warning', 'Info'] as AlertFilter[]).map((filter) => (
              <button key={filter} type="button" className={alertFilter === filter ? 'active' : ''} onClick={() => setAlertFilter(filter)}>
                {filter === 'All' ? 'Todos' : severityLabels[filter]}
                <span>{filter === 'All' ? data.alerts.length : data.alerts.filter((alert) => alert.severity === filter).length}</span>
              </button>
            ))}
          </div>

          {visibleAlerts.length === 0
            ? <div className="tower-empty"><strong>Nenhum alerta neste filtro</strong><span>A operação não possui ocorrências com esta prioridade.</span></div>
            : <div className="alert-list">{visibleAlerts.map((alert) => <AlertCard key={alert.id} alert={alert} />)}</div>}
        </section>

        <section className="content-card monitored-calls">
          <header className="card-heading tower-heading">
            <div><span>Operação</span><h2>Escalas monitoradas</h2></div>
            <Link to="/escalas">Ver todas →</Link>
          </header>

          {data.calls.length === 0
            ? <div className="tower-empty"><strong>Nenhuma escala ativa</strong><span>Novas escalas aparecerão aqui quando entrarem no fluxo.</span></div>
            : <div className="monitored-list">{data.calls.map((call) => <MonitoredCall key={call.id} call={call} />)}</div>}
        </section>
      </div>
    </div>
  )
}

function MetricCard({ label, value, code, detail, accent = false, danger = false }: {
  label: string
  value: number | string
  code: string
  detail: string
  accent?: boolean
  danger?: boolean
}) {
  return (
    <article className={`metric-card${accent ? ' accent' : ''}${danger ? ' danger' : ''}`}>
      <span className="metric-code" aria-hidden="true">{code}</span>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{detail}</small>
    </article>
  )
}

function AlertCard({ alert }: { alert: OperationalAlert }) {
  return (
    <article className={`alert-card ${alert.severity.toLowerCase()}`}>
      <span className="alert-marker" aria-hidden="true" />
      <div className="alert-body">
        <header><span>{severityLabels[alert.severity]}</span>{alert.deviationMinutes !== null && <strong>{formatDeviation(alert.deviationMinutes)}</strong>}</header>
        <h3>{alert.title}</h3>
        <p>{alert.description}</p>
        <footer><span>{alert.vesselName} · {alert.portCallPublicCode}</span><Link to={alert.actionPath}>Analisar escala →</Link></footer>
      </div>
    </article>
  )
}

function MonitoredCall({ call }: { call: ControlTowerCall }) {
  return (
    <Link className="monitored-call" to={`/escalas/${encodeURIComponent(call.publicCode)}`}>
      <div className="monitored-main">
        <span className="vessel-monogram" aria-hidden="true">{getVesselMonogram(call.vesselName)}</span>
        <div><strong>{call.vesselName}</strong><small>{call.publicCode}</small></div>
      </div>
      <div className="monitored-status"><StatusBadge status={call.status} />{call.alertCount > 0 && <span className={`call-alert ${call.highestAlertSeverity?.toLowerCase()}`}>{call.alertCount}</span>}</div>
      <dl>
        <div><dt>Berço</dt><dd>{call.berthName ?? 'A definir'}</dd></div>
        <div><dt>Janela</dt><dd>{call.windowStartsAtUtc ? formatWindow(call.windowStartsAtUtc, call.windowEndsAtUtc) : 'Sem programação'}</dd></div>
        <div><dt>Próximo marco</dt><dd>{call.nextMilestone ? milestoneLabels[call.nextMilestone] : 'Aguardando planejamento'}</dd></div>
      </dl>
    </Link>
  )
}

function formatPercent(value: number) { return `${new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 }).format(value)}%` }
function formatTime(value: string) { return new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' }).format(new Date(value)) }
function formatDeviation(minutes: number) {
  const absolute = Math.abs(minutes)
  const hours = Math.floor(absolute / 60)
  const remaining = absolute % 60
  const value = hours > 0 ? `${hours}h${remaining > 0 ? ` ${remaining}min` : ''}` : `${remaining}min`
  return minutes < 0 ? `${value} adiantado` : `${value} de desvio`
}
function formatWindow(start: string, end: string | null) {
  const formatter = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' })
  return `${formatter.format(new Date(start))}–${end ? formatter.format(new Date(end)) : '—'}`
}
function getVesselMonogram(name: string) { return name.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase() }
