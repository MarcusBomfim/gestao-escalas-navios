import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  cancelBerthWindow,
  confirmBerthWindow,
  getPortCallBerthWindow,
  getVessel,
  listPorts,
  reprogramBerthWindow,
  requestBerthWindow,
} from '../api/portManagement'
import type { BerthReference, BerthWindow, PortCall, TerminalReference, Vessel } from '../api/types'
import { ApiError } from '../api/client'
import { FormError } from './FormFeedback'
import { QueryError, TableSkeleton } from './QueryFeedback'

const windowStatusLabels: Record<string, string> = {
  Requested: 'Solicitada',
  Confirmed: 'Confirmada',
  Completed: 'Concluída',
  Cancelled: 'Cancelada',
}

export function BerthPlanningPanel({ portCall, canManage }: { portCall: PortCall; canManage: boolean }) {
  const queryClient = useQueryClient()
  const windowQuery = useQuery({
    queryKey: ['berth-window', portCall.publicCode],
    queryFn: () => getPortCallBerthWindow(portCall.publicCode),
  })
  const vessel = useQuery({
    queryKey: ['vessel', portCall.vesselId],
    queryFn: () => getVessel(portCall.vesselId),
    enabled: canManage,
  })
  const ports = useQuery({
    queryKey: ['ports'],
    queryFn: listPorts,
    enabled: canManage,
  })

  const updatePlanningCache = async (window: BerthWindow | null) => {
    queryClient.setQueryData(['berth-window', portCall.publicCode], { window })
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['port-call', portCall.publicCode] }),
      queryClient.invalidateQueries({ queryKey: ['port-calls'] }),
      queryClient.invalidateQueries({ queryKey: ['berth-windows'] }),
    ])
  }

  const requestMutation = useMutation({
    mutationFn: (input: { berthId: string; startsAtUtc: string; endsAtUtc: string }) =>
      requestBerthWindow(portCall.publicCode, {
        ...input,
        expectedPortCallVersion: portCall.version,
      }),
    onSuccess: updatePlanningCache,
    onError: (error) => handlePlanningConflict(error, queryClient, portCall.publicCode),
  })
  const reprogramMutation = useMutation({
    mutationFn: (input: { berthId: string; startsAtUtc: string; endsAtUtc: string; reason: string }) => {
      const window = windowQuery.data?.window
      if (!window) throw new Error('Janela ativa não encontrada.')
      return reprogramBerthWindow(portCall.publicCode, {
        ...input,
        expectedWindowVersion: window.version,
      })
    },
    onSuccess: updatePlanningCache,
    onError: (error) => handlePlanningConflict(error, queryClient, portCall.publicCode),
  })
  const confirmMutation = useMutation({
    mutationFn: () => {
      const window = windowQuery.data?.window
      if (!window) throw new Error('Janela ativa não encontrada.')
      return confirmBerthWindow(portCall.publicCode, window.version)
    },
    onSuccess: updatePlanningCache,
    onError: (error) => handlePlanningConflict(error, queryClient, portCall.publicCode),
  })
  const cancelMutation = useMutation({
    mutationFn: (reason: string) => {
      const window = windowQuery.data?.window
      if (!window) throw new Error('Janela ativa não encontrada.')
      return cancelBerthWindow(portCall.publicCode, window.version, reason)
    },
    onSuccess: async () => updatePlanningCache(null),
    onError: (error) => handlePlanningConflict(error, queryClient, portCall.publicCode),
  })

  if (windowQuery.isPending || (canManage && (vessel.isPending || ports.isPending))) {
    return <section className="content-card planning-panel"><TableSkeleton rows={4} /></section>
  }

  if (windowQuery.isError || vessel.isError || ports.isError) {
    return <section className="content-card planning-panel"><QueryError error={windowQuery.error ?? vessel.error ?? ports.error} /></section>
  }

  const currentWindow = windowQuery.data.window
  const berthOptions = canManage && vessel.data && ports.data
    ? buildBerthOptions(portCall.portId, vessel.data, ports.data)
    : []
  const canStartPlanning = ['UnderReview', 'Planned'].includes(portCall.status)

  return (
    <section className="content-card planning-panel">
      <header className="planning-heading">
        <div><p>Planejamento de atracação</p><h3>Janela de terminal e berço</h3></div>
        {currentWindow && <span className={`window-state ${currentWindow.status.toLowerCase()}`}>{windowStatusLabels[currentWindow.status]}</span>}
      </header>

      {currentWindow ? (
        <WindowSummary window={currentWindow} />
      ) : (
        <div className="planning-empty">
          <strong>Nenhuma janela ativa</strong>
          <span>Defina um berço compatível e o período previsto de ocupação.</span>
        </div>
      )}

      {!canManage && (
        <div className="form-information"><strong>Planejamento em modo de consulta</strong><span>Somente administradores e planejadores podem alterar a janela de berço.</span></div>
      )}

      {canManage && !canStartPlanning && !currentWindow && (
        <div className="form-information"><strong>Escala ainda não elegível</strong><span>Avance a situação para “Em análise” antes de solicitar uma janela.</span></div>
      )}

      {canManage && canStartPlanning && (
        <PlanningForm
          berthOptions={berthOptions}
          window={currentWindow}
          error={requestMutation.error ?? reprogramMutation.error}
          isPending={requestMutation.isPending || reprogramMutation.isPending}
          onSubmit={(input) => currentWindow
            ? reprogramMutation.mutate(input)
            : requestMutation.mutate(input)}
        />
      )}

      {canManage && currentWindow && (
        <div className="planning-actions">
          {currentWindow.status === 'Requested' && (
            <button className="primary-action" type="button" disabled={confirmMutation.isPending} onClick={() => confirmMutation.mutate()}>
              {confirmMutation.isPending ? 'Confirmando…' : 'Confirmar janela'}
            </button>
          )}
          <CancelWindowForm isPending={cancelMutation.isPending} onCancel={(reason) => cancelMutation.mutate(reason)} />
          <FormError error={confirmMutation.error ?? cancelMutation.error} />
        </div>
      )}

      {currentWindow && currentWindow.revisions.length > 0 && (
        <div className="window-revisions">
          <strong>Reprogramações</strong>
          {[...currentWindow.revisions].reverse().map((revision) => (
            <article key={revision.changedAtUtc}>
              <span>{formatDateTime(revision.previousStartsAtUtc)} → {formatDateTime(revision.newStartsAtUtc)}</span>
              <p>{revision.reason}</p>
              <small>{formatDateTime(revision.changedAtUtc)}</small>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

interface BerthOption {
  berth: BerthReference
  terminal: TerminalReference
  compatible: boolean
  reason: string | null
}

function PlanningForm({
  berthOptions,
  window,
  error,
  isPending,
  onSubmit,
}: {
  berthOptions: BerthOption[]
  window: BerthWindow | null
  error: unknown
  isPending: boolean
  onSubmit: (input: { berthId: string; startsAtUtc: string; endsAtUtc: string; reason: string }) => void
}) {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    onSubmit({
      berthId: String(data.get('berthId')),
      startsAtUtc: new Date(String(data.get('startsAt'))).toISOString(),
      endsAtUtc: new Date(String(data.get('endsAt'))).toISOString(),
      reason: String(data.get('reason') ?? '').trim(),
    })
  }

  return (
    <form className="planning-form" onSubmit={handleSubmit}>
      <div className="planning-form-grid">
        <label className="field span-2">Terminal e berço
          <select name="berthId" required defaultValue={window?.berthId ?? ''}>
            <option value="" disabled>Selecione um berço compatível</option>
            {berthOptions.map(({ berth, terminal, compatible, reason }) => (
              <option key={berth.id} value={berth.id} disabled={!compatible}>
                {terminal.name} · {berth.code} — {berth.name}{compatible ? '' : ` (${reason})`}
              </option>
            ))}
          </select>
        </label>
        <label className="field">Início previsto<input name="startsAt" type="datetime-local" required defaultValue={window ? toLocalInput(window.startsAtUtc) : defaultStart()} /></label>
        <label className="field">Fim previsto<input name="endsAt" type="datetime-local" required defaultValue={window ? toLocalInput(window.endsAtUtc) : defaultEnd()} /></label>
        {window && <label className="field span-2">Justificativa da reprogramação<textarea name="reason" rows={3} maxLength={500} required placeholder="Descreva o motivo da alteração" /></label>}
      </div>
      <div className="compatibility-legend"><i aria-hidden="true" /><span>Berços incompatíveis ficam indisponíveis conforme tipo, comprimento, boca e calado.</span></div>
      <FormError error={error} />
      <button className="secondary-action planning-submit" type="submit" disabled={isPending || berthOptions.every((option) => !option.compatible)}>
        {isPending ? 'Salvando planejamento…' : window ? 'Reprogramar janela' : 'Solicitar janela'}
      </button>
    </form>
  )
}

function WindowSummary({ window }: { window: BerthWindow }) {
  return (
    <dl className="window-summary">
      <div><dt>Terminal</dt><dd>{window.terminalName}</dd></div>
      <div><dt>Berço</dt><dd>{window.berthCode} · {window.berthName}</dd></div>
      <div><dt>Início</dt><dd>{formatDateTime(window.startsAtUtc)}</dd></div>
      <div><dt>Fim</dt><dd>{formatDateTime(window.endsAtUtc)}</dd></div>
    </dl>
  )
}

function CancelWindowForm({ isPending, onCancel }: { isPending: boolean; onCancel: (reason: string) => void }) {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = event.currentTarget
    const reason = String(new FormData(form).get('cancelReason') ?? '').trim()
    if (reason) onCancel(reason)
  }

  return (
    <form className="cancel-window-form" onSubmit={handleSubmit}>
      <label className="field"><span className="sr-only">Justificativa do cancelamento</span><input name="cancelReason" required maxLength={500} placeholder="Justificativa para cancelar" /></label>
      <button className="danger-action" type="submit" disabled={isPending}>{isPending ? 'Cancelando…' : 'Cancelar janela'}</button>
    </form>
  )
}

function buildBerthOptions(portId: string, vessel: Vessel, ports: Awaited<ReturnType<typeof listPorts>>) {
  return ports
    .filter((port) => port.id === portId)
    .flatMap((port) => port.terminals.flatMap((terminal) => terminal.berths.map((berth) => {
      const reason = incompatibilityReason(berth, vessel)
      return { berth, terminal, compatible: reason === null, reason }
    })))
}

function incompatibilityReason(berth: BerthReference, vessel: Vessel) {
  if (berth.status !== 'Available') return 'indisponível'
  if (vessel.lengthOverallMeters > berth.usefulLengthMeters) return 'comprimento excedido'
  if (vessel.beamMeters > berth.maximumBeamMeters) return 'boca excedida'
  if (vessel.maximumDraftMeters > berth.maximumDraftMeters) return 'calado excedido'
  if (berth.supportedVesselTypes.length > 0 && !berth.supportedVesselTypes.includes(vessel.type)) return 'tipo não suportado'
  return null
}

async function handlePlanningConflict(error: unknown, queryClient: ReturnType<typeof useQueryClient>, publicCode: string) {
  if (error instanceof ApiError && error.status === 409) {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['berth-window', publicCode] }),
      queryClient.invalidateQueries({ queryKey: ['port-call', publicCode] }),
      queryClient.invalidateQueries({ queryKey: ['berth-windows'] }),
    ])
  }
}

function toLocalInput(value: string) {
  const date = new Date(value)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}

function defaultStart() {
  const date = new Date(Date.now() + 4 * 60 * 60 * 1000)
  date.setMinutes(0, 0, 0)
  return toLocalInput(date.toISOString())
}

function defaultEnd() {
  const date = new Date(Date.now() + 12 * 60 * 60 * 1000)
  date.setMinutes(0, 0, 0)
  return toLocalInput(date.toISOString())
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}
