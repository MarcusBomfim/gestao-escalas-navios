export const portCallPurposes = [
  { value: 'CargoOperation', label: 'Operação de carga' },
  { value: 'PassengerOperation', label: 'Operação de passageiros' },
  { value: 'Bunkering', label: 'Abastecimento' },
  { value: 'Maintenance', label: 'Manutenção' },
  { value: 'CrewChange', label: 'Troca de tripulação' },
  { value: 'Shelter', label: 'Abrigo' },
  { value: 'Other', label: 'Outra finalidade' },
] as const

export const portCallPurposeLabels = Object.fromEntries(
  portCallPurposes.map(({ value, label }) => [value, label]),
) as Record<string, string>

const allowedTransitions: Record<string, string[]> = {
  Draft: ['Requested', 'Cancelled'],
  Requested: ['UnderReview', 'Cancelled'],
  UnderReview: ['Planned', 'Cancelled'],
  Planned: ['Cancelled'],
  AtAnchorage: ['Cancelled'],
  ClearedForBerthing: ['Cancelled'],
  Berthed: ['Cancelled'],
  InOperation: ['Cancelled'],
  OperationCompleted: ['Cancelled'],
  Unberthed: [],
  Closed: [],
  Cancelled: [],
}

export function getAllowedTransitions(status: string) {
  return allowedTransitions[status] ?? []
}
