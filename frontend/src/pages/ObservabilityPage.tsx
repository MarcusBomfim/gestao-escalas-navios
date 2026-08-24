import { useQuery } from '@tanstack/react-query'
import { getObservabilitySummary } from '../api/portManagement'
import { QueryError } from '../components/QueryFeedback'

const componentLabels: Record<string, string> = {
  postgresql: 'PostgreSQL',
  self: 'API',
}

export function ObservabilityPage() {
  const observability = useQuery({
    queryKey: ['observability'],
    queryFn: getObservabilitySummary,
    refetchInterval: 15_000,
  })
  const data = observability.data
  const errorRate = data && data.api.totalRequests > 0
    ? (data.api.serverErrors / data.api.totalRequests) * 100
    : 0

  return (
    <div className="page-stack observability-page">
      <section className="page-introduction observability-introduction">
        <div>
          <p>Diagnóstico técnico</p>
          <h2>Saúde e observabilidade</h2>
          <span>Métricas da instância atual, prontidão dos componentes e desempenho recente da API.</span>
        </div>
        <div className={`readiness-badge ${data?.readinessStatus.toLowerCase() ?? 'loading'}`}>
          <i aria-hidden="true" />
          <span>Prontidão</span>
          <strong>{data ? healthLabel(data.readinessStatus) : 'Verificando'}</strong>
        </div>
      </section>

      {observability.isError && <QueryError error={observability.error} />}

      <section className="observability-metrics" aria-label="Métricas da API">
        <MetricCard label="Requisições" value={formatNumber(data?.api.totalRequests)} detail={`${formatNumber(data?.api.requestsLastMinute)} no último minuto`} />
        <MetricCard label="Tempo médio" value={formatMilliseconds(data?.api.averageDurationMilliseconds)} detail={`P95 em ${formatMilliseconds(data?.api.p95DurationMilliseconds)}`} />
        <MetricCard label="Erros do servidor" value={formatNumber(data?.api.serverErrors)} detail={`${formatDecimal(errorRate)}% das requisições`} tone={data?.api.serverErrors ? 'danger' : 'success'} />
        <MetricCard label="Tempo ativo" value={formatUptime(data?.api.uptimeSeconds)} detail={`${formatNumber(data?.api.activeRequests)} requisição(ões) ativa(s)`} />
      </section>

      <section className="observability-grid">
        <article className="content-card component-health">
          <header>
            <div><span>Infraestrutura</span><h3>Componentes monitorados</h3></div>
            <time>{data ? `Atualizado às ${formatTime(data.generatedAtUtc)}` : 'Carregando…'}</time>
          </header>
          <div className="component-list">
            {observability.isPending && <div className="component-placeholder" />}
            {data?.components.map((component) => (
              <div key={component.name}>
                <span className={`component-state ${component.status.toLowerCase()}`}><i aria-hidden="true" />{healthLabel(component.status)}</span>
                <strong>{componentLabels[component.name] ?? component.name}</strong>
                <small>Verificação em {formatMilliseconds(component.durationMilliseconds)}</small>
              </div>
            ))}
          </div>
        </article>

        <article className="content-card observability-notes">
          <span>Rastreabilidade</span>
          <h3>Correlação ponta a ponta</h3>
          <p>Cada resposta inclui o cabeçalho <code>X-Correlation-ID</code>. O mesmo identificador aparece nos logs estruturados e nos registros de auditoria.</p>
          <dl>
            <div><dt>Vida</dt><dd><code>/health/live</code></dd></div>
            <div><dt>Prontidão</dt><dd><code>/health/ready</code></dd></div>
            <div><dt>Atualização</dt><dd>A cada 15 segundos</dd></div>
          </dl>
        </article>
      </section>
    </div>
  )
}

function MetricCard({ label, value, detail, tone = '' }: { label: string; value: string; detail: string; tone?: string }) {
  return <article className={tone}><span>{label}</span><strong>{value}</strong><small>{detail}</small></article>
}

function healthLabel(value: 'Healthy' | 'Degraded' | 'Unhealthy') {
  return value === 'Healthy' ? 'Saudável' : value === 'Degraded' ? 'Degradado' : 'Indisponível'
}

function formatNumber(value: number | undefined) {
  return value === undefined ? '—' : new Intl.NumberFormat('pt-BR').format(value)
}

function formatDecimal(value: number) {
  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(value)
}

function formatMilliseconds(value: number | undefined) {
  return value === undefined ? '—' : `${formatDecimal(value)} ms`
}

function formatUptime(seconds: number | undefined) {
  if (seconds === undefined) return '—'
  const days = Math.floor(seconds / 86_400)
  const hours = Math.floor((seconds % 86_400) / 3_600)
  const minutes = Math.floor((seconds % 3_600) / 60)
  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h ${minutes}min`
  return `${minutes}min`
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }).format(new Date(value))
}
