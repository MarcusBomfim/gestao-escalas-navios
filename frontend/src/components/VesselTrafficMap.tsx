import { Link } from 'react-router-dom'
import type { VesselNavigationState, VesselTraffic } from '../api/types'

const navigationLabels: Record<VesselNavigationState, string> = {
  AwaitingSchedule: 'Aguardando programação',
  Approaching: 'Em aproximação',
  Anchored: 'No fundeadouro',
  Manoeuvring: 'Em manobra',
  Berthed: 'Atracado',
  Operating: 'Em operação',
  ReadyToSail: 'Pronto para saída',
  Departing: 'Em saída',
}

export function VesselTrafficMap({ traffic }: { traffic: VesselTraffic }) {
  return (
    <section className="content-card traffic-panel" aria-labelledby="traffic-title">
      <header className="traffic-heading">
        <div>
          <span>Consciência situacional</span>
          <h2 id="traffic-title">Mapa operacional</h2>
          <p>{traffic.coverageLabel}</p>
        </div>
        <div className="traffic-update">
          <strong><i aria-hidden="true" /> Dados simulados</strong>
          <small>Posição às {formatTime(traffic.generatedAtUtc)}</small>
        </div>
      </header>

      {traffic.positions.length === 0 ? (
        <div className="tower-empty">
          <strong>Nenhum navio no mapa</strong>
          <span>As escalas ativas serão posicionadas automaticamente.</span>
        </div>
      ) : (
        <div className="traffic-layout">
          <div className="traffic-chart">
            <svg
              viewBox="0 0 1000 500"
              role="img"
              aria-labelledby="traffic-map-title traffic-map-description"
              preserveAspectRatio="xMidYMid meet"
            >
              <title id="traffic-map-title">Posições simuladas dos navios</title>
              <desc id="traffic-map-description">
                Representação esquemática do terminal, canal de acesso, fundeadouro e navios com escalas ativas.
              </desc>
              <defs>
                <pattern id="traffic-grid" width="50" height="50" patternUnits="userSpaceOnUse">
                  <path d="M 50 0 L 0 0 0 50" className="traffic-grid-line" />
                </pattern>
                <linearGradient id="traffic-water" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0" stopColor="#071d2d" />
                  <stop offset="1" stopColor="#0a3047" />
                </linearGradient>
              </defs>

              <rect width="1000" height="500" rx="18" fill="url(#traffic-water)" />
              <rect width="1000" height="500" rx="18" fill="url(#traffic-grid)" />
              <path className="traffic-coast" d="M0 0H425L385 82L410 145L350 205L372 280L310 345L328 420L270 500H0Z" />
              <path className="traffic-channel" d="M190 395C330 355 405 270 510 245C645 211 757 222 940 145" />
              <path className="traffic-channel-center" d="M190 395C330 355 405 270 510 245C645 211 757 222 940 145" />

              <g className="terminal-zone">
                <rect x="95" y="105" width="190" height="54" rx="8" />
                <rect x="75" y="205" width="210" height="54" rx="8" />
                <rect x="45" y="305" width="230" height="54" rx="8" />
                <text x="95" y="91">TERMINAIS</text>
              </g>
              <g className="map-labels" aria-hidden="true">
                <text x="500" y="214">CANAL DE ACESSO</text>
                <text x="675" y="390">FUNDEADOURO</text>
                <text x="805" y="75">ÁREA DE APROXIMAÇÃO</text>
              </g>

              {traffic.positions.map((position) => (
                <g
                  key={position.portCallId}
                  transform={`translate(${position.xPercent * 10} ${position.yPercent * 5})`}
                >
                  <Link
                    to={`/escalas/${encodeURIComponent(position.portCallPublicCode)}`}
                    className={`traffic-position ${position.navigationState.toLowerCase()}`}
                    aria-label={`${position.vesselName}: ${navigationLabels[position.navigationState]}`}
                  >
                    <title>{position.vesselName} — {navigationLabels[position.navigationState]}</title>
                    <circle className="position-pulse" r="22" />
                    <circle className="position-core" r="11" />
                    <path className="position-heading" d="M0 -7L17 0L0 7Z" transform={`rotate(${position.courseDegrees})`} />
                    <text x="0" y="34">{getVesselMonogram(position.vesselName)}</text>
                  </Link>
                </g>
              ))}
            </svg>

            <div className="traffic-legend" aria-label="Legenda do mapa">
              <span><i className="moving" /> Em movimento</span>
              <span><i className="waiting" /> Em espera</span>
              <span><i className="alongside" /> No terminal</span>
            </div>
          </div>

          <div className="traffic-vessels" aria-label="Navios posicionados">
            {traffic.positions.map((position) => (
              <Link key={position.portCallId} to={`/escalas/${encodeURIComponent(position.portCallPublicCode)}`}>
                <span className={`traffic-state ${position.navigationState.toLowerCase()}`} aria-hidden="true" />
                <div>
                  <strong>{position.vesselName}</strong>
                  <small>{position.portCallPublicCode} · {position.berthName ?? position.terminalName ?? 'Área externa'}</small>
                </div>
                <div className="traffic-vessel-state">
                  <strong>{navigationLabels[position.navigationState]}</strong>
                  <small>{formatSpeed(position.speedKnots)}</small>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}

      <footer className="traffic-disclaimer">
        <span aria-hidden="true">SIM</span>
        <p><strong>Visualização demonstrativa.</strong> As posições são relativas, geradas pelo sistema e não representam rastreamento AIS real.</p>
      </footer>
    </section>
  )
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  }).format(new Date(value))
}

function formatSpeed(value: number) {
  if (value === 0) return 'Sem deslocamento'
  return `${new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 }).format(value)} nós`
}

function getVesselMonogram(name: string) {
  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0))
    .join('')
    .toUpperCase()
}
