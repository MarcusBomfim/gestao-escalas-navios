export type SecurityRole = 'Administrator' | 'Planner' | 'Operator' | 'Viewer'

export interface AuthenticatedUser {
  id: string
  displayName: string
  email: string
  organizationId: string | null
  roles: SecurityRole[]
}

export interface SessionResponse {
  accessToken: string
  accessTokenExpiresAtUtc: string
  user: AuthenticatedUser
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface Vessel {
  id: string
  name: string
  imoNumber: string | null
  flagCode: string
  type: string
  lengthOverallMeters: number
  beamMeters: number
  maximumDraftMeters: number
  callSign: string | null
  mmsi: string | null
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface VesselInput {
  name: string
  imoNumber: string | null
  flagCode: string
  type: string
  lengthOverallMeters: number
  beamMeters: number
  maximumDraftMeters: number
  callSign: string | null
  mmsi: string | null
}

export interface PortCallStatusHistory {
  previousStatus: string
  newStatus: string
  changedBy: string
  changedAtUtc: string
  reason: string | null
}

export interface PortCall {
  id: string
  publicCode: string
  vesselId: string
  vesselName: string
  portId: string
  portName: string
  purpose: string
  status: string
  voyageNumber: string | null
  previousPortUnLocode: string | null
  nextPortUnLocode: string | null
  plannedTerminalId: string | null
  plannedTerminalName: string | null
  plannedBerthId: string | null
  plannedBerthName: string | null
  version: number
  createdAtUtc: string
  updatedAtUtc: string
  closedAtUtc: string | null
  statusHistory: PortCallStatusHistory[]
}

export interface PortCallInput {
  vesselId: string
  portId: string
  purpose: string
  voyageNumber: string | null
  previousPortUnLocode: string | null
  nextPortUnLocode: string | null
}

export interface PortCallTransitionInput {
  newStatus: string
  expectedVersion: number
  reason: string | null
}

export interface BerthReference {
  id: string
  code: string
  name: string
  usefulLengthMeters: number
  maximumBeamMeters: number
  maximumDraftMeters: number
  supportedVesselTypes: string[]
  status: string
}

export interface TerminalReference {
  id: string
  code: string
  name: string
  timeZoneId: string
  berths: BerthReference[]
}

export interface PortReference {
  id: string
  name: string
  unLocode: string
  countryCode: string
  timeZoneId: string
  terminals: TerminalReference[]
}

export type BerthWindowStatus = 'Requested' | 'Confirmed' | 'Completed' | 'Cancelled'

export interface BerthWindowRevision {
  previousBerthId: string
  newBerthId: string
  previousStartsAtUtc: string
  previousEndsAtUtc: string
  newStartsAtUtc: string
  newEndsAtUtc: string
  changedBy: string
  reason: string
  changedAtUtc: string
}

export interface BerthWindow {
  id: string
  portCallId: string
  portCallPublicCode: string
  vesselId: string
  vesselName: string
  portId: string
  portName: string
  terminalId: string
  terminalName: string
  berthId: string
  berthCode: string
  berthName: string
  startsAtUtc: string
  endsAtUtc: string
  status: BerthWindowStatus
  requestedBy: string
  lastChangedBy: string | null
  lastChangeReason: string | null
  version: number
  createdAtUtc: string
  updatedAtUtc: string
  revisions: BerthWindowRevision[]
}

export interface PortCallBerthWindowResponse {
  window: BerthWindow | null
}

export interface RequestBerthWindowInput {
  berthId: string
  startsAtUtc: string
  endsAtUtc: string
  expectedPortCallVersion: number
}

export interface ReprogramBerthWindowInput {
  berthId: string
  startsAtUtc: string
  endsAtUtc: string
  expectedWindowVersion: number
  reason: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
}

export type OperationalMilestone =
  | 'ArrivedAtAnchorage'
  | 'PilotageStarted'
  | 'BerthingCompleted'
  | 'CargoOperationStarted'
  | 'CargoOperationCompleted'
  | 'UnberthingCompleted'
  | 'Departed'

export type CargoOperationDirection = 'Loading' | 'Discharge' | 'Both'
export type CargoQuantityUnit = 'MetricTon' | 'CubicMeter' | 'Teu' | 'Unit'

export interface OperationalEvent {
  id: string
  phase: string
  action: string
  occursAtUtc: string
  source: string
  recordedBy: string
  recordedAtUtc: string
}

export interface CargoOperation {
  id: string
  direction: CargoOperationDirection
  cargoType: string
  plannedQuantity: number
  actualQuantity: number | null
  quantityUnit: CargoQuantityUnit
  isDangerousCargo: boolean
  dangerousCargoClassification: string | null
  plannedStartAtUtc: string | null
  plannedEndAtUtc: string | null
  actualStartAtUtc: string | null
  actualEndAtUtc: string | null
  version: number
  status: 'Planned' | 'InProgress' | 'Completed'
}

export interface CargoUnitSummary {
  quantityUnit: CargoQuantityUnit
  plannedQuantity: number
  actualQuantity: number
  productivityPerHour: number | null
}

export interface OperationalExecution {
  portCallId: string
  portCallPublicCode: string
  portCallStatus: string
  portCallVersion: number
  nextMilestone: OperationalMilestone | null
  events: OperationalEvent[]
  cargoOperations: CargoOperation[]
  kpis: {
    portStayHours: number | null
    berthStayHours: number | null
    cargoOperationHours: number | null
    cargoSummaries: CargoUnitSummary[]
  }
}

export interface CreateCargoOperationInput {
  direction: CargoOperationDirection
  cargoType: string
  plannedQuantity: number
  quantityUnit: CargoQuantityUnit
  isDangerousCargo: boolean
  dangerousCargoClassification: string | null
  plannedStartAtUtc: string
  plannedEndAtUtc: string
  expectedPortCallVersion: number
}

export type OperationalAlertSeverity = 'Info' | 'Warning' | 'Critical'

export type OperationalAlertType =
  | 'MissingBerthPlan'
  | 'PendingBerthConfirmation'
  | 'ArrivalDelay'
  | 'BerthOverstay'
  | 'CargoDelay'
  | 'ScheduleDeviation'
  | 'StaleOperationalUpdate'

export interface OperationalAlert {
  id: string
  portCallId: string
  portCallPublicCode: string
  vesselName: string
  severity: OperationalAlertSeverity
  type: OperationalAlertType
  title: string
  description: string
  deviationMinutes: number | null
  detectedAtUtc: string
  actionPath: string
}

export interface ControlTowerCall {
  id: string
  publicCode: string
  vesselName: string
  status: string
  portName: string
  terminalName: string | null
  berthName: string | null
  windowStartsAtUtc: string | null
  windowEndsAtUtc: string | null
  lastActivityAtUtc: string | null
  nextMilestone: OperationalMilestone | null
  alertCount: number
  highestAlertSeverity: OperationalAlertSeverity | null
}

export interface ControlTower {
  generatedAtUtc: string
  summary: {
    activePortCalls: number
    inOperation: number
    callsRequiringAttention: number
    criticalAlerts: number
    occupiedBerths: number
    totalBerths: number
    berthOccupancyPercent: number
    scheduleCompliancePercent: number
  }
  alerts: OperationalAlert[]
  calls: ControlTowerCall[]
}

export interface NotificationItem extends OperationalAlert {
  isRead: boolean
  readAtUtc: string | null
}

export interface NotificationCenter {
  generatedAtUtc: string
  unreadCount: number
  items: NotificationItem[]
}
