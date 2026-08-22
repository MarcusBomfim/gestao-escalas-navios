export const statusLabels: Record<string, string> = {
  Draft: 'Rascunho',
  Requested: 'Solicitada',
  UnderReview: 'Em análise',
  Planned: 'Planejada',
  AtAnchorage: 'No fundeadouro',
  ClearedForBerthing: 'Liberada para atracar',
  Berthed: 'Atracada',
  InOperation: 'Em operação',
  OperationCompleted: 'Operação concluída',
  Unberthed: 'Desatracada',
  Closed: 'Encerrada',
  Cancelled: 'Cancelada',
}

export function getStatusLabel(status: string) {
  return statusLabels[status] ?? status
}
