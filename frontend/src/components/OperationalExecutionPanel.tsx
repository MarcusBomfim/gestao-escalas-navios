import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import {
  completeCargoOperation,
  createCargoOperation,
  getOperationalExecution,
  recordOperationalMilestone,
  startCargoOperation,
} from '../api/portManagement'
import type {
  CargoOperation,
  CargoQuantityUnit,
  CreateCargoOperationInput,
  OperationalExecution,
  OperationalMilestone,
} from '../api/types'
import { ApiError } from '../api/client'
import { FormError } from './FormFeedback'
import { QueryError, TableSkeleton } from './QueryFeedback'

const milestoneLabels: Record<OperationalMilestone, string> = {
  ArrivedAtAnchorage: 'Chegada ao fundeadouro',
  PilotageStarted: 'Início da praticagem',
  BerthingCompleted: 'Atracação concluída',
  CargoOperationStarted: 'Início da operação de carga',
  CargoOperationCompleted: 'Operação de carga concluída',
  UnberthingCompleted: 'Desatracação concluída',
  Departed: 'Saída do porto',
}

const eventLabels: Record<string, string> = {
  'Anchorage.Arrival': 'Chegada ao fundeadouro',
  'Pilotage.Start': 'Praticagem iniciada',
  'Berth.Completion': 'Navio atracado',
  'CargoOperation.Start': 'Operação de carga iniciada',
  'CargoOperation.Completion': 'Operação de carga concluída',
  'Departure.Start': 'Desatracação concluída',
  'Departure.Departure': 'Navio deixou o porto',
}

const unitLabels: Record<CargoQuantityUnit, string> = {
  MetricTon: 't',
  CubicMeter: 'm³',
  Teu: 'TEU',
  Unit: 'un.',
}

const cargoStatusLabels = { Planned: 'Planejada', InProgress: 'Em andamento', Completed: 'Concluída' }

export function OperationalExecutionPanel({ publicCode, canManage }: { publicCode: string; canManage: boolean }) {
  const queryClient = useQueryClient()
  const execution = useQuery({
    queryKey: ['operational-execution', publicCode],
    queryFn: () => getOperationalExecution(publicCode),
  })

  const refresh = async (updated?: OperationalExecution) => {
    if (updated) queryClient.setQueryData(['operational-execution', publicCode], updated)
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['operational-execution', publicCode] }),
      queryClient.invalidateQueries({ queryKey: ['port-call', publicCode] }),
      queryClient.invalidateQueries({ queryKey: ['port-calls'] }),
    ])
  }

  const milestone = useMutation({
    mutationFn: ({ value, occursAtUtc, version }: { value: OperationalMilestone; occursAtUtc: string; version: number }) =>
      recordOperationalMilestone(publicCode, value, occursAtUtc, version),
    onSuccess: refresh,
    onError: (error) => refreshOnConflict(error, refresh),
  })
  const createCargo = useMutation({
    mutationFn: (input: CreateCargoOperationInput) => createCargoOperation(publicCode, input),
    onSuccess: () => refresh(),
    onError: (error) => refreshOnConflict(error, refresh),
  })
  const startCargo = useMutation({
    mutationFn: (cargo: CargoOperation) => startCargoOperation(publicCode, cargo.id, new Date().toISOString(), cargo.version),
    onSuccess: () => refresh(),
    onError: (error) => refreshOnConflict(error, refresh),
  })
  const completeCargo = useMutation({
    mutationFn: ({ cargo, actualQuantity }: { cargo: CargoOperation; actualQuantity: number }) =>
      completeCargoOperation(publicCode, cargo.id, actualQuantity, new Date().toISOString(), cargo.version),
    onSuccess: () => refresh(),
    onError: (error) => refreshOnConflict(error, refresh),
  })

  if (execution.isPending) return <section className="content-card operational-panel"><TableSkeleton rows={5} /></section>
  if (execution.isError) return <section className="content-card operational-panel"><QueryError error={execution.error} /></section>

  const data = execution.data
  const canRegisterCargo = ['Planned', 'AtAnchorage', 'ClearedForBerthing', 'Berthed', 'InOperation'].includes(data.portCallStatus)
  const milestoneBlockReason = data.nextMilestone === 'CargoOperationStarted' && data.cargoOperations.length === 0
    ? 'Cadastre ao menos uma operação de carga antes de iniciar.'
    : data.nextMilestone === 'CargoOperationCompleted' && data.cargoOperations.some((cargo) => cargo.status !== 'Completed')
      ? 'Conclua todas as movimentações antes de encerrar a operação.'
      : null

  return (
    <section className="content-card operational-panel">
      <header className="operational-heading">
        <div><p>Execução operacional</p><h3>Linha do tempo e movimentação de carga</h3></div>
        <span className="live-indicator"><i /> Dados realizados</span>
      </header>

      <KpiGrid data={data} />

      {!canManage && <div className="form-information"><strong>Acompanhamento em tempo real</strong><span>Seu perfil permite consultar eventos, cargas e indicadores sem alterar a operação.</span></div>}

      {canManage && data.nextMilestone && (
        <MilestoneForm
          milestone={data.nextMilestone}
          version={data.portCallVersion}
          isPending={milestone.isPending}
          error={milestone.error}
          blockReason={milestoneBlockReason}
          onSubmit={(value, occursAtUtc, version) => milestone.mutate({ value, occursAtUtc, version })}
        />
      )}

      <div className="operational-columns">
        <div className="cargo-section">
          <div className="subsection-heading"><div><span>Movimentações</span><h4>Operações de carga</h4></div><strong>{data.cargoOperations.length}</strong></div>
          {data.cargoOperations.length === 0
            ? <div className="operational-empty">Nenhuma operação de carga cadastrada.</div>
            : data.cargoOperations.map((cargo) => (
              <CargoCard
                key={cargo.id}
                cargo={cargo}
                canManage={canManage}
                portCallStatus={data.portCallStatus}
                isPending={startCargo.isPending || completeCargo.isPending}
                onStart={() => startCargo.mutate(cargo)}
                onComplete={(actualQuantity) => completeCargo.mutate({ cargo, actualQuantity })}
              />
            ))}
          <FormError error={startCargo.error ?? completeCargo.error} />
          {canManage && canRegisterCargo && <CargoForm version={data.portCallVersion} isPending={createCargo.isPending} error={createCargo.error} onSubmit={(input) => createCargo.mutate(input)} />}
        </div>

        <div className="event-section">
          <div className="subsection-heading"><div><span>Rastreabilidade</span><h4>Eventos realizados</h4></div><strong>{data.events.length}</strong></div>
          {data.events.length === 0
            ? <div className="operational-empty">O primeiro marco operacional aparecerá aqui.</div>
            : <ol className="operational-timeline">{[...data.events].reverse().map((item) => (
              <li key={item.id}><i /><div><strong>{eventLabels[`${item.phase}.${item.action}`] ?? `${item.phase} · ${item.action}`}</strong><time>{formatDateTime(item.occursAtUtc)}</time><small>{item.source}</small></div></li>
            ))}</ol>}
        </div>
      </div>
    </section>
  )
}

function KpiGrid({ data }: { data: OperationalExecution }) {
  const cargo = data.kpis.cargoSummaries[0]
  return (
    <div className="operational-kpis">
      <Kpi label="Permanência no porto" value={formatHours(data.kpis.portStayHours)} />
      <Kpi label="Tempo atracado" value={formatHours(data.kpis.berthStayHours)} />
      <Kpi label="Tempo de operação" value={formatHours(data.kpis.cargoOperationHours)} />
      <Kpi label="Produtividade" value={cargo?.productivityPerHour ? `${formatNumber(cargo.productivityPerHour)} ${unitLabels[cargo.quantityUnit]}/h` : '—'} />
    </div>
  )
}

function Kpi({ label, value }: { label: string; value: string }) {
  return <article><span>{label}</span><strong>{value}</strong></article>
}

function MilestoneForm({ milestone, version, isPending, error, blockReason, onSubmit }: {
  milestone: OperationalMilestone
  version: number
  isPending: boolean
  error: unknown
  blockReason: string | null
  onSubmit: (milestone: OperationalMilestone, occursAtUtc: string, version: number) => void
}) {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const value = String(new FormData(event.currentTarget).get('occursAt'))
    onSubmit(milestone, new Date(value).toISOString(), version)
  }
  return (
    <form className="milestone-form" onSubmit={handleSubmit}>
      <div><span>Próximo marco</span><strong>{milestoneLabels[milestone]}</strong></div>
      <label className="field">Horário realizado<input type="datetime-local" name="occursAt" required defaultValue={toLocalInput(new Date().toISOString())} /></label>
      <button className="primary-action" disabled={isPending || Boolean(blockReason)}>{isPending ? 'Registrando…' : 'Registrar marco'}</button>
      {blockReason && <small className="milestone-block-reason">{blockReason}</small>}
      <FormError error={error} />
    </form>
  )
}

function CargoForm({ version, isPending, error, onSubmit }: { version: number; isPending: boolean; error: unknown; onSubmit: (input: CreateCargoOperationInput) => void }) {
  const [dangerous, setDangerous] = useState(false)
  const [plannedTimes] = useState(() => ({
    start: toLocalInput(new Date(Date.now() + 60 * 60 * 1000).toISOString()),
    end: toLocalInput(new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString()),
  }))
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = event.currentTarget
    const values = new FormData(form)
    onSubmit({
      direction: String(values.get('direction')) as CreateCargoOperationInput['direction'],
      cargoType: String(values.get('cargoType')).trim(),
      plannedQuantity: Number(values.get('plannedQuantity')),
      quantityUnit: String(values.get('quantityUnit')) as CargoQuantityUnit,
      isDangerousCargo: dangerous,
      dangerousCargoClassification: dangerous ? String(values.get('dangerousCargoClassification')).trim() : null,
      plannedStartAtUtc: new Date(String(values.get('plannedStartAt'))).toISOString(),
      plannedEndAtUtc: new Date(String(values.get('plannedEndAt'))).toISOString(),
      expectedPortCallVersion: version,
    })
  }
  return (
    <form className="cargo-form" onSubmit={handleSubmit}>
      <div className="subsection-heading"><div><span>Planejamento</span><h4>Adicionar carga</h4></div></div>
      <div className="cargo-form-grid">
        <label className="field span-2">Tipo de carga<input name="cargoType" required maxLength={120} placeholder="Ex.: contêineres secos" /></label>
        <label className="field">Movimento<select name="direction"><option value="Loading">Embarque</option><option value="Discharge">Descarga</option><option value="Both">Embarque e descarga</option></select></label>
        <label className="field">Unidade<select name="quantityUnit"><option value="MetricTon">Toneladas</option><option value="Teu">TEU</option><option value="CubicMeter">Metros cúbicos</option><option value="Unit">Unidades</option></select></label>
        <label className="field">Quantidade prevista<input name="plannedQuantity" type="number" min="0.001" step="0.001" required /></label>
        <label className="field">Início previsto<input name="plannedStartAt" type="datetime-local" required defaultValue={plannedTimes.start} /></label>
        <label className="field">Fim previsto<input name="plannedEndAt" type="datetime-local" required defaultValue={plannedTimes.end} /></label>
        <label className="check-field"><input type="checkbox" checked={dangerous} onChange={(event) => setDangerous(event.target.checked)} /><span>Carga perigosa</span></label>
        {dangerous && <label className="field span-2">Classificação da carga perigosa<input name="dangerousCargoClassification" maxLength={80} required placeholder="Ex.: IMO Classe 3" /></label>}
      </div>
      <FormError error={error} />
      <button className="secondary-action" disabled={isPending}>{isPending ? 'Adicionando…' : 'Adicionar operação'}</button>
    </form>
  )
}

function CargoCard({ cargo, canManage, portCallStatus, isPending, onStart, onComplete }: {
  cargo: CargoOperation
  canManage: boolean
  portCallStatus: string
  isPending: boolean
  onStart: () => void
  onComplete: (quantity: number) => void
}) {
  const handleComplete = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    onComplete(Number(new FormData(event.currentTarget).get('actualQuantity')))
  }
  return (
    <article className="cargo-card">
      <header><div><span>{cargo.direction === 'Loading' ? 'Embarque' : cargo.direction === 'Discharge' ? 'Descarga' : 'Movimento misto'}</span><strong>{cargo.cargoType}</strong></div><em className={cargo.status.toLowerCase()}>{cargoStatusLabels[cargo.status]}</em></header>
      <dl><div><dt>Previsto</dt><dd>{formatNumber(cargo.plannedQuantity)} {unitLabels[cargo.quantityUnit]}</dd></div><div><dt>Realizado</dt><dd>{cargo.actualQuantity === null ? '—' : `${formatNumber(cargo.actualQuantity)} ${unitLabels[cargo.quantityUnit]}`}</dd></div></dl>
      {cargo.isDangerousCargo && <small className="dangerous-cargo">Carga perigosa · {cargo.dangerousCargoClassification}</small>}
      {canManage && cargo.status === 'Planned' && portCallStatus === 'InOperation' && <button className="secondary-action compact-action" type="button" disabled={isPending} onClick={onStart}>Iniciar movimentação</button>}
      {canManage && cargo.status === 'InProgress' && <form className="complete-cargo-form" onSubmit={handleComplete}><label className="field">Quantidade realizada<input name="actualQuantity" type="number" min="0" step="0.001" required defaultValue={cargo.plannedQuantity} /></label><button className="primary-action" disabled={isPending}>Concluir carga</button></form>}
    </article>
  )
}

async function refreshOnConflict(error: unknown, refresh: () => Promise<void>) {
  if (error instanceof ApiError && error.status === 409) await refresh()
}

function formatDateTime(value: string) { return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) }
function formatNumber(value: number) { return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(value) }
function formatHours(value: number | null) { return value === null ? '—' : `${formatNumber(value)} h` }
function toLocalInput(value: string) { const date = new Date(value); return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16) }
