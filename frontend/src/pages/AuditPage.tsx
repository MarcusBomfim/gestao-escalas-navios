import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import {
  exportAuditRecords,
  exportOperationalReport,
  getControlTower,
  listAuditRecords,
} from '../api/portManagement'
import type { AuditAction } from '../api/types'
import { Pagination } from '../components/Pagination'
import { QueryError, TableSkeleton } from '../components/QueryFeedback'

const actionLabels: Record<AuditAction, string> = {
  Created: 'Criação',
  Updated: 'Atualização',
  Deleted: 'Exclusão',
}

const entityLabels: Record<string, string> = {
  Vessel: 'Navio',
  PortCall: 'Escala',
  PortCallStatusHistory: 'Histórico da escala',
  PortCallEvent: 'Marco operacional',
  BerthWindow: 'Janela de berço',
  BerthWindowRevision: 'Reprogramação',
  CargoOperation: 'Operação de carga',
}

export function AuditPage() {
  const [page, setPage] = useState(1)
  const [action, setAction] = useState<AuditAction | ''>('')
  const [entityType, setEntityType] = useState('')
  const [downloadState, setDownloadState] = useState<'audit' | 'operations' | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const tower = useQuery({ queryKey: ['control-tower'], queryFn: getControlTower })
  const audit = useQuery({
    queryKey: ['audit', { page, action, entityType }],
    queryFn: () => listAuditRecords({
      page,
      pageSize: 15,
      ...(action ? { action } : {}),
      ...(entityType ? { entityType } : {}),
    }),
  })

  const handleExport = async (kind: 'audit' | 'operations') => {
    setDownloadState(kind)
    setDownloadError(null)
    try {
      const blob = kind === 'audit'
        ? await exportAuditRecords({
            ...(action ? { action } : {}),
            ...(entityType ? { entityType } : {}),
          })
        : await exportOperationalReport()
      saveBlob(blob, kind === 'audit' ? 'auditoria-operacional.csv' : 'relatorio-operacional.csv')
    } catch (error) {
      setDownloadError(error instanceof Error ? error.message : 'Não foi possível gerar o arquivo.')
    } finally {
      setDownloadState(null)
    }
  }

  return (
    <div className="page-stack audit-page">
      <section className="page-introduction audit-introduction">
        <div>
          <p>Governança operacional</p>
          <h2>Auditoria e relatórios</h2>
          <span>Acompanhe alterações importantes e exporte evidências sem expor o conteúdo dos dados modificados.</span>
        </div>
        <div className="audit-export-actions">
          <button type="button" disabled={downloadState !== null} onClick={() => void handleExport('operations')}>
            {downloadState === 'operations' ? 'Gerando…' : 'Exportar operação'}
          </button>
          <button className="primary-action" type="button" disabled={downloadState !== null} onClick={() => void handleExport('audit')}>
            {downloadState === 'audit' ? 'Gerando…' : 'Exportar auditoria'}
          </button>
        </div>
      </section>

      {downloadError && <div className="form-feedback error" role="alert">{downloadError}</div>}

      <section className="audit-summary" aria-label="Resumo operacional para relatório">
        <article><span>Escalas ativas</span><strong>{tower.data?.summary.activePortCalls ?? '—'}</strong></article>
        <article><span>Em operação</span><strong>{tower.data?.summary.inOperation ?? '—'}</strong></article>
        <article><span>Requerem atenção</span><strong>{tower.data?.summary.callsRequiringAttention ?? '—'}</strong></article>
        <article><span>Aderência</span><strong>{tower.data ? `${formatNumber(tower.data.summary.scheduleCompliancePercent)}%` : '—'}</strong></article>
      </section>

      <section className="content-card data-card">
        <header className="data-toolbar audit-toolbar">
          <div>
            <strong>Trilha de alterações</strong>
            <span>{audit.data?.totalItems ?? '—'} registro(s) encontrado(s)</span>
          </div>
          <div className="audit-filters">
            <label>
              <span>Ação</span>
              <select value={action} onChange={(event) => { setAction(event.target.value as AuditAction | ''); setPage(1) }}>
                <option value="">Todas</option>
                <option value="Created">Criação</option>
                <option value="Updated">Atualização</option>
                <option value="Deleted">Exclusão</option>
              </select>
            </label>
            <label>
              <span>Entidade</span>
              <select value={entityType} onChange={(event) => { setEntityType(event.target.value); setPage(1) }}>
                <option value="">Todas</option>
                {Object.entries(entityLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
          </div>
        </header>

        {audit.isPending && <TableSkeleton rows={8} />}
        {audit.isError && <QueryError error={audit.error} />}
        {audit.data && audit.data.items.length === 0 && <div className="audit-empty">Nenhuma alteração corresponde aos filtros selecionados.</div>}
        {audit.data && audit.data.items.length > 0 && (
          <div className="table-scroll">
            <table className="audit-table">
              <thead><tr><th>Data</th><th>Usuário</th><th>Ação</th><th>Registro</th><th>Campos</th><th>Origem</th></tr></thead>
              <tbody>
                {audit.data.items.map((record) => (
                  <tr key={record.id}>
                    <td><time dateTime={record.occurredAtUtc}>{formatDateTime(record.occurredAtUtc)}</time></td>
                    <td><strong>{record.userDisplayName}</strong></td>
                    <td><span className={`audit-action ${record.action.toLowerCase()}`}>{actionLabels[record.action]}</span></td>
                    <td><strong>{entityLabels[record.entityType] ?? record.entityType}</strong><small>{shortId(record.entityId)}</small></td>
                    <td>{record.changedFields.length > 0 ? record.changedFields.join(', ') : '—'}</td>
                    <td><code>{record.httpMethod}</code><small>{record.requestPath}</small></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <Pagination page={page} totalPages={audit.data?.totalPages ?? 0} onChange={setPage} />
      </section>
    </div>
  )
}

function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function formatNumber(value: number) {
  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 }).format(value)
}

function shortId(value: string) {
  return value.length > 18 ? `${value.slice(0, 8)}…${value.slice(-6)}` : value
}
