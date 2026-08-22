import { getStatusLabel } from './statusLabels'

const warningStatuses = new Set(['Requested', 'UnderReview', 'AtAnchorage'])
const successStatuses = new Set(['Planned', 'Berthed', 'InOperation', 'OperationCompleted'])
const neutralStatuses = new Set(['Draft', 'Unberthed', 'Closed'])

export function StatusBadge({ status }: { status: string }) {
  const tone = warningStatuses.has(status)
    ? 'warning'
    : successStatuses.has(status)
      ? 'success'
      : neutralStatuses.has(status)
        ? 'neutral'
        : 'danger'

  return <span className={`status-badge ${tone}`}>{getStatusLabel(status)}</span>
}
