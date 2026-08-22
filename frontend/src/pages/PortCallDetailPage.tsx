import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { getPortCall, transitionPortCall } from '../api/portManagement'
import type { PortCallTransitionInput } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { FormError } from '../components/FormFeedback'
import { getAllowedTransitions, portCallPurposeLabels } from '../components/portCallOptions'
import { QueryError, TableSkeleton } from '../components/QueryFeedback'
import { StatusBadge } from '../components/StatusBadge'
import { getStatusLabel } from '../components/statusLabels'
import { BerthPlanningPanel } from '../components/BerthPlanningPanel'
import { OperationalExecutionPanel } from '../components/OperationalExecutionPanel'

export function PortCallDetailPage() {
  const { publicCode = '' } = useParams()
  const { hasAnyRole } = useAuth()
  const queryClient = useQueryClient()
  const portCall = useQuery({
    queryKey: ['port-call', publicCode],
    queryFn: () => getPortCall(publicCode),
  })
  const transition = useMutation({
    mutationFn: (input: PortCallTransitionInput) => transitionPortCall(publicCode, input),
    onSuccess: async (updated) => {
      queryClient.setQueryData(['port-call', publicCode], updated)
      await queryClient.invalidateQueries({ queryKey: ['port-calls'] })
    },
    onError: async (error) => {
      if (error instanceof ApiError && error.code === 'port_calls.version_conflict') {
        await queryClient.invalidateQueries({ queryKey: ['port-call', publicCode] })
      }
    },
  })

  if (portCall.isPending) {
    return <section className="content-card"><TableSkeleton rows={6} /></section>
  }

  if (portCall.isError) {
    return <section className="content-card"><QueryError error={portCall.error} /></section>
  }

  const data = portCall.data
  const nextStatuses = getAllowedTransitions(data.status)
  const canTransition = hasAnyRole('Administrator', 'Planner', 'Operator')
  const canManagePlanning = hasAnyRole('Administrator', 'Planner')

  const handleTransition = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = event.currentTarget
    const formData = new FormData(form)
    const newStatus = String(formData.get('newStatus'))
    const reasonField = form.elements.namedItem('reason') as HTMLTextAreaElement
    const reasonValue = reasonField.value.trim()
    reasonField.setCustomValidity(
      newStatus === 'Cancelled' && !reasonValue
        ? 'Informe a justificativa do cancelamento.'
        : '',
    )
    if (!form.reportValidity()) {
      return
    }
    transition.mutate({
      newStatus,
      expectedVersion: data.version,
      reason: reasonValue || null,
    }, {
      onSuccess: () => form.reset(),
    })
  }

  return (
    <div className="page-stack">
      <section className="detail-hero content-card">
        <div>
          <Link className="back-link" to="/escalas">← Voltar para escalas</Link>
          <p>Escala portuária</p>
          <h2>{data.publicCode}</h2>
          <span>Criada em {formatDateTime(data.createdAtUtc)} · versão operacional {data.version}</span>
        </div>
        <StatusBadge status={data.status} />
      </section>

      <div className="detail-grid">
        <section className="content-card detail-panel">
          <header><p>Dados da escala</p><h3>Operação e rota</h3></header>
          <dl className="details-list">
            <div><dt>Navio</dt><dd>{data.vesselName}</dd></div>
            <div><dt>Porto</dt><dd>{data.portName}</dd></div>
            <div><dt>Finalidade</dt><dd>{portCallPurposeLabels[data.purpose] ?? data.purpose}</dd></div>
            <div><dt>Viagem</dt><dd>{data.voyageNumber ?? 'Não informada'}</dd></div>
            <div><dt>Origem anterior</dt><dd>{data.previousPortUnLocode ?? 'Não informada'}</dd></div>
            <div><dt>Próximo destino</dt><dd>{data.nextPortUnLocode ?? 'Não informado'}</dd></div>
            <div><dt>Terminal</dt><dd>{data.plannedTerminalName ?? 'A definir'}</dd></div>
            <div><dt>Berço</dt><dd>{data.plannedBerthName ?? 'A definir'}</dd></div>
          </dl>
        </section>

        <section className="content-card detail-panel transition-panel">
          <header><p>Fluxo operacional</p><h3>Atualizar situação</h3></header>
          {!canTransition && <div className="form-information"><strong>Perfil de consulta</strong><span>Seu acesso permite acompanhar a escala, mas não alterar sua situação.</span></div>}
          {canTransition && nextStatuses.length === 0 && <div className="form-information"><strong>Fluxo concluído</strong><span>Esta escala não possui novas transições disponíveis.</span></div>}
          {canTransition && nextStatuses.length > 0 && (
            <form className="transition-form" onSubmit={handleTransition}>
              <label className="field">Próxima situação<select name="newStatus" required defaultValue=""><option value="" disabled>Selecione uma transição</option>{nextStatuses.map((status) => <option key={status} value={status}>{getStatusLabel(status)}</option>)}</select></label>
              <label className="field">Justificativa ou observação<textarea name="reason" maxLength={500} rows={4} placeholder="Obrigatória em caso de cancelamento" /></label>
              <FormError error={transition.error} />
              <button className="primary-action" type="submit" disabled={transition.isPending}>{transition.isPending ? 'Atualizando…' : 'Confirmar transição'}</button>
            </form>
          )}
        </section>
      </div>

      <BerthPlanningPanel portCall={data} canManage={canManagePlanning} />

      <OperationalExecutionPanel
        publicCode={data.publicCode}
        canManage={hasAnyRole('Administrator', 'Operator')}
      />

      <section className="content-card history-panel">
        <header><p>Rastreabilidade</p><h3>Histórico da escala</h3></header>
        {data.statusHistory.length === 0 ? (
          <div className="history-empty"><strong>Escala em rascunho</strong><span>A primeira movimentação aparecerá aqui.</span></div>
        ) : (
          <ol className="status-timeline">
            {[...data.statusHistory].reverse().map((history, index) => (
              <li key={`${history.changedAtUtc}-${index}`}>
                <span className="timeline-marker" aria-hidden="true" />
                <div>
                  <strong>{getStatusLabel(history.previousStatus)} → {getStatusLabel(history.newStatus)}</strong>
                  <time dateTime={history.changedAtUtc}>{formatDateTime(history.changedAtUtc)}</time>
                  {history.reason && <p>{history.reason}</p>}
                  <small>Registro: {history.changedBy}</small>
                </div>
              </li>
            ))}
          </ol>
        )}
      </section>
    </div>
  )
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
