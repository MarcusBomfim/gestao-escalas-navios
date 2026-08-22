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

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
}
