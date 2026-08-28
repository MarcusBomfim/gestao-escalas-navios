import { Link } from 'react-router-dom'

export function PortIcon() {
  return (
    <svg viewBox="0 0 40 40" role="presentation">
      <path d="M7 23.5h26l-4.4 7.4H11.4L7 23.5Z" />
      <path d="M13 20V9h10v11M23 13h5l3 7" />
      <path d="M4 34c3 0 3-1.8 6-1.8S13 34 16 34s3-1.8 6-1.8S25 34 28 34s3-1.8 6-1.8" />
    </svg>
  )
}

export function Logo({ compact = false }: { compact?: boolean }) {
  return (
    <Link className="brand" to="/" aria-label="Gestão de Escalas — início">
      <span className="brand-mark" aria-hidden="true">
        <PortIcon />
      </span>
      {!compact && (
        <span className="brand-copy">
          <strong>Porto Control</strong>
          <small>Gestão de escalas</small>
        </span>
      )}
    </Link>
  )
}
