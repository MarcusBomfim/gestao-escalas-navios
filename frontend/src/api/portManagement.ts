import { download, request } from './client'
import type {
  PagedResponse,
  BerthWindow,
  BerthWindowStatus,
  PortCall,
  PortCallInput,
  PortCallTransitionInput,
  PortCallBerthWindowResponse,
  PortReference,
  Vessel,
  VesselInput,
  RequestBerthWindowInput,
  ReprogramBerthWindowInput,
  OperationalExecution,
  OperationalMilestone,
  CargoOperation,
  CreateCargoOperationInput,
  ControlTower,
  NotificationCenter,
  AuditAction,
  AuditRecord,
  ObservabilitySummary,
  ManagedUser,
  UserManagementOptions,
  CreateUserInput,
  UpdateUserInput,
  SecurityRole,
  AuthenticatedUser,
  ManagedOrganization,
  ManagedBerth,
  ManagedPort,
  ManagedTerminal,
  OrganizationInput,
  OrganizationUpdateInput,
  PortInput,
  PortUpdateInput,
  TerminalInput,
  TerminalUpdateInput,
  BerthInput,
  BerthUpdateInput,
  OrganizationType,
} from './types'

export interface ListOptions {
  page?: number
  pageSize?: number
  search?: string
}

export interface ListPortCallsOptions extends ListOptions {
  status?: string
  portId?: string
}

export interface ListAuditOptions {
  page?: number
  pageSize?: number
  action?: AuditAction
  entityType?: string
  fromUtc?: string
  toUtc?: string
}

export interface ListUsersOptions extends ListOptions {
  role?: SecurityRole
  isActive?: boolean
}

export interface ListOrganizationsOptions extends ListOptions {
  type?: OrganizationType
  isActive?: boolean
}

export function listVessels(options: ListOptions = {}) {
  const query = createSearchParams(options)
  query.set('activeOnly', 'true')
  return request<PagedResponse<Vessel>>(`/api/v1/vessels?${query}`)
}

export function listPortCalls(options: ListPortCallsOptions = {}) {
  const query = createSearchParams(options)
  return request<PagedResponse<PortCall>>(`/api/v1/port-calls?${query}`)
}

export function listPorts() {
  return request<PortReference[]>('/api/v1/reference-data/ports')
}

export function listUsers(options: ListUsersOptions = {}) {
  const query = createSearchParams(options)
  if (options.role) query.set('role', options.role)
  if (options.isActive !== undefined) query.set('isActive', String(options.isActive))
  return request<PagedResponse<ManagedUser>>(`/api/v1/users/?${query}`)
}

export function getUserManagementOptions() {
  return request<UserManagementOptions>('/api/v1/users/options')
}

export function createUser(input: CreateUserInput) {
  return request<AuthenticatedUser>('/api/v1/users/', jsonRequest('POST', input))
}

export function updateUser(id: string, input: UpdateUserInput) {
  return request<ManagedUser>(
    `/api/v1/users/${encodeURIComponent(id)}`,
    jsonRequest('PUT', input),
  )
}

export function listManagedOrganizations(options: ListOrganizationsOptions = {}) {
  const query = createSearchParams(options)
  if (options.type) query.set('type', options.type)
  if (options.isActive !== undefined) query.set('isActive', String(options.isActive))
  return request<PagedResponse<ManagedOrganization>>(
    `/api/v1/admin/master-data/organizations?${query}`,
  )
}

export function createManagedOrganization(input: OrganizationInput) {
  return request<ManagedOrganization>(
    '/api/v1/admin/master-data/organizations',
    jsonRequest('POST', input),
  )
}

export function updateManagedOrganization(id: string, input: OrganizationUpdateInput) {
  return request<ManagedOrganization>(
    `/api/v1/admin/master-data/organizations/${encodeURIComponent(id)}`,
    jsonRequest('PUT', input),
  )
}

export function listManagedPortStructure() {
  return request<ManagedPort[]>('/api/v1/admin/master-data/ports')
}

export function createManagedPort(input: PortInput) {
  return request<ManagedPort>(
    '/api/v1/admin/master-data/ports',
    jsonRequest('POST', input),
  )
}

export function updateManagedPort(id: string, input: PortUpdateInput) {
  return request<ManagedPort>(
    `/api/v1/admin/master-data/ports/${encodeURIComponent(id)}`,
    jsonRequest('PUT', input),
  )
}

export function createManagedTerminal(portId: string, input: TerminalInput) {
  return request<ManagedTerminal>(
    `/api/v1/admin/master-data/ports/${encodeURIComponent(portId)}/terminals`,
    jsonRequest('POST', input),
  )
}

export function updateManagedTerminal(id: string, input: TerminalUpdateInput) {
  return request<ManagedTerminal>(
    `/api/v1/admin/master-data/terminals/${encodeURIComponent(id)}`,
    jsonRequest('PUT', input),
  )
}

export function createManagedBerth(terminalId: string, input: BerthInput) {
  return request<ManagedBerth>(
    `/api/v1/admin/master-data/terminals/${encodeURIComponent(terminalId)}/berths`,
    jsonRequest('POST', input),
  )
}

export function updateManagedBerth(id: string, input: BerthUpdateInput) {
  return request<ManagedBerth>(
    `/api/v1/admin/master-data/berths/${encodeURIComponent(id)}`,
    jsonRequest('PUT', input),
  )
}

export function getVessel(id: string) {
  return request<Vessel>(`/api/v1/vessels/${encodeURIComponent(id)}`)
}

export function registerVessel(input: VesselInput) {
  return request<Vessel>('/api/v1/vessels/', jsonRequest('POST', input))
}

export function updateVessel(id: string, input: VesselInput) {
  return request<Vessel>(`/api/v1/vessels/${encodeURIComponent(id)}`, jsonRequest('PUT', input))
}

export function getPortCall(publicCode: string) {
  return request<PortCall>(`/api/v1/port-calls/${encodeURIComponent(publicCode)}`)
}

export function createPortCall(input: PortCallInput, idempotencyKey: string) {
  return request<PortCall>('/api/v1/port-calls/', {
    ...jsonRequest('POST', input),
    headers: {
      'Content-Type': 'application/json',
      'Idempotency-Key': idempotencyKey,
    },
  })
}

export function transitionPortCall(publicCode: string, input: PortCallTransitionInput) {
  return request<PortCall>(
    `/api/v1/port-calls/${encodeURIComponent(publicCode)}/transitions`,
    jsonRequest('POST', input),
  )
}

export interface ListBerthWindowsOptions {
  page?: number
  pageSize?: number
  portId?: string
  berthId?: string
  status?: BerthWindowStatus
  fromUtc?: string
  toUtc?: string
}

export function listBerthWindows(options: ListBerthWindowsOptions = {}) {
  const query = new URLSearchParams()
  query.set('page', String(options.page ?? 1))
  query.set('pageSize', String(options.pageSize ?? 20))
  if (options.portId) query.set('portId', options.portId)
  if (options.berthId) query.set('berthId', options.berthId)
  if (options.status) query.set('status', options.status)
  if (options.fromUtc) query.set('fromUtc', options.fromUtc)
  if (options.toUtc) query.set('toUtc', options.toUtc)
  return request<PagedResponse<BerthWindow>>(`/api/v1/planning/berth-windows?${query}`)
}

export function getPortCallBerthWindow(publicCode: string) {
  return request<PortCallBerthWindowResponse>(
    `/api/v1/planning/port-calls/${encodeURIComponent(publicCode)}/berth-window`,
  )
}

export function requestBerthWindow(publicCode: string, input: RequestBerthWindowInput) {
  return request<BerthWindow>(
    `/api/v1/planning/port-calls/${encodeURIComponent(publicCode)}/berth-window`,
    jsonRequest('POST', input),
  )
}

export function reprogramBerthWindow(publicCode: string, input: ReprogramBerthWindowInput) {
  return request<BerthWindow>(
    `/api/v1/planning/port-calls/${encodeURIComponent(publicCode)}/berth-window`,
    jsonRequest('PUT', input),
  )
}

export function confirmBerthWindow(publicCode: string, expectedWindowVersion: number) {
  return request<BerthWindow>(
    `/api/v1/planning/port-calls/${encodeURIComponent(publicCode)}/berth-window/confirm`,
    jsonRequest('POST', { expectedWindowVersion }),
  )
}

export function cancelBerthWindow(
  publicCode: string,
  expectedWindowVersion: number,
  reason: string,
) {
  return request<BerthWindow>(
    `/api/v1/planning/port-calls/${encodeURIComponent(publicCode)}/berth-window/cancel`,
    jsonRequest('POST', { expectedWindowVersion, reason }),
  )
}

export function getOperationalExecution(publicCode: string) {
  return request<OperationalExecution>(
    `/api/v1/operations/port-calls/${encodeURIComponent(publicCode)}/`,
  )
}

export function getControlTower() {
  return request<ControlTower>('/api/v1/control-tower')
}

export function getNotificationCenter() {
  return request<NotificationCenter>('/api/v1/notifications/')
}

export function listAuditRecords(options: ListAuditOptions = {}) {
  const query = createAuditSearchParams(options)
  return request<PagedResponse<AuditRecord>>(`/api/v1/audit/?${query}`)
}

export function exportAuditRecords(options: Omit<ListAuditOptions, 'page' | 'pageSize'> = {}) {
  const query = createAuditSearchParams(options)
  return download(`/api/v1/audit/export?${query}`)
}

export function exportOperationalReport() {
  return download('/api/v1/reports/operations/export')
}

export function getObservabilitySummary() {
  return request<ObservabilitySummary>('/api/v1/observability/summary')
}

export function markNotificationRead(alertId: string) {
  return request<NotificationCenter>(
    `/api/v1/notifications/${encodeURIComponent(alertId)}/read`,
    { method: 'POST' },
  )
}

export function markAllNotificationsRead() {
  return request<NotificationCenter>('/api/v1/notifications/read-all', { method: 'POST' })
}

export function recordOperationalMilestone(
  publicCode: string,
  milestone: OperationalMilestone,
  occursAtUtc: string,
  expectedPortCallVersion: number,
) {
  return request<OperationalExecution>(
    `/api/v1/operations/port-calls/${encodeURIComponent(publicCode)}/milestones`,
    jsonRequest('POST', { milestone, occursAtUtc, expectedPortCallVersion }),
  )
}

export function createCargoOperation(publicCode: string, input: CreateCargoOperationInput) {
  return request<CargoOperation>(
    `/api/v1/operations/port-calls/${encodeURIComponent(publicCode)}/cargo-operations`,
    jsonRequest('POST', input),
  )
}

export function startCargoOperation(
  publicCode: string,
  cargoOperationId: string,
  startedAtUtc: string,
  expectedVersion: number,
) {
  return request<CargoOperation>(
    `/api/v1/operations/port-calls/${encodeURIComponent(publicCode)}/cargo-operations/${encodeURIComponent(cargoOperationId)}/start`,
    jsonRequest('POST', { startedAtUtc, expectedVersion }),
  )
}

export function completeCargoOperation(
  publicCode: string,
  cargoOperationId: string,
  actualQuantity: number,
  completedAtUtc: string,
  expectedVersion: number,
) {
  return request<CargoOperation>(
    `/api/v1/operations/port-calls/${encodeURIComponent(publicCode)}/cargo-operations/${encodeURIComponent(cargoOperationId)}/complete`,
    jsonRequest('POST', { actualQuantity, completedAtUtc, expectedVersion }),
  )
}

function createSearchParams(options: ListPortCallsOptions) {
  const query = new URLSearchParams()
  query.set('page', String(options.page ?? 1))
  query.set('pageSize', String(options.pageSize ?? 20))

  if (options.search?.trim()) {
    query.set('search', options.search.trim())
  }
  if (options.status) {
    query.set('status', options.status)
  }
  if (options.portId) {
    query.set('portId', options.portId)
  }

  return query
}

function createAuditSearchParams(options: ListAuditOptions) {
  const query = new URLSearchParams()
  if (options.page) query.set('page', String(options.page))
  if (options.pageSize) query.set('pageSize', String(options.pageSize))
  if (options.action) query.set('action', options.action)
  if (options.entityType?.trim()) query.set('entityType', options.entityType.trim())
  if (options.fromUtc) query.set('fromUtc', options.fromUtc)
  if (options.toUtc) query.set('toUtc', options.toUtc)
  return query
}

function jsonRequest(method: 'POST' | 'PUT', body: object): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }
}
