import { Link } from 'react-router-dom'

export function Logo({ compact = false }: { compact?: boolean }) {
  return (
    <Link className="brand" to="/" aria-label="Gestão de Escalas — início">
      <span className="brand-mark" aria-hidden="true">GE</span>
      {!compact && (
        <span className="brand-copy">
          <strong>Gestão de Escalas</strong>
          <small>Operações portuárias</small>
        </span>
      )}
    </Link>
  )
}
