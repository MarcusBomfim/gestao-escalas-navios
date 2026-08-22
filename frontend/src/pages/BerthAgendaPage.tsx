import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listBerthWindows } from '../api/portManagement'
import type { BerthWindowStatus } from '../api/types'
import { EmptyState, QueryError, TableSkeleton } from '../components/QueryFeedback'

const statusLabels: Record<BerthWindowStatus, string> = {
  Requested: 'Solicitada',
  Confirmed: 'Confirmada',
  Completed: 'Concluída',
  Cancelled: 'Cancelada',
}

export function BerthAgendaPage() {
  const [date, setDate] = useState(todayInput())
  const [status, setStatus] = useState<BerthWindowStatus | ''>('')
  const period = dayPeriod(date)
  const windows = useQuery({
    queryKey: ['berth-windows', { date, status }],
    queryFn: () => listBerthWindows({
      pageSize: 100,
      fromUtc: period.fromUtc,
      toUtc: period.toUtc,
      ...(status ? { status } : {}),
    }),
  })

  const confirmed = windows.data?.items.filter((window) => window.status === 'Confirmed').length ?? 0
  const requested = windows.data?.items.filter((window) => window.status === 'Requested').length ?? 0

  return (
    <div className="page-stack">
      <section className="page-introduction">
        <div>
          <p>Ocupação de berços</p>
          <h2>Agenda operacional</h2>
          <span>Visualize janelas solicitadas e confirmadas sem perder o contexto da escala.</span>
        </div>
        <div className="agenda-summary" aria-label="Resumo da agenda">
          <span><strong>{confirmed}</strong> confirmadas</span>
          <span><strong>{requested}</strong> solicitadas</span>
        </div>
      </section>

      <section className="content-card agenda-card">
        <header className="agenda-toolbar">
          <label className="field">Dia da operação<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label>
          <label className="field">Situação<select value={status} onChange={(event) => setStatus(event.target.value as BerthWindowStatus | '')}><option value="">Todas as situações</option>{Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        </header>

        {windows.isPending && <TableSkeleton rows={5} />}
        {windows.isError && <QueryError error={windows.error} />}
        {windows.data?.items.length === 0 && <EmptyState title="Agenda livre neste período" description="Selecione outro dia ou remova o filtro de situação." />}

        {windows.data && windows.data.items.length > 0 && (
          <div className="berth-schedule">
            {windows.data.items.map((window) => (
              <article key={window.id} className={`schedule-entry ${window.status.toLowerCase()}`}>
                <div className="schedule-time"><strong>{formatTime(window.startsAtUtc)}</strong><span>{formatTime(window.endsAtUtc)}</span></div>
                <div className="schedule-line" aria-hidden="true"><i /></div>
                <div className="schedule-main">
                  <span>{window.terminalName}</span>
                  <h3>{window.berthCode} · {window.berthName}</h3>
                  <p>{window.vesselName} · {window.portCallPublicCode}</p>
                </div>
                <div className="schedule-actions">
                  <span className={`window-state ${window.status.toLowerCase()}`}>{statusLabels[window.status]}</span>
                  <Link to={`/escalas/${window.portCallPublicCode}`}>Abrir escala →</Link>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}

function dayPeriod(value: string) {
  const start = new Date(`${value}T00:00:00`)
  const end = new Date(start)
  end.setDate(end.getDate() + 1)
  return { fromUtc: start.toISOString(), toUtc: end.toISOString() }
}

function todayInput() {
  const now = new Date()
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10)
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}
