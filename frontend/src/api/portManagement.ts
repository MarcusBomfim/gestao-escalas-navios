import { request } from './client'
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

function jsonRequest(method: 'POST' | 'PUT', body: object): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }
}
