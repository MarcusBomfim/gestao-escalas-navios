import { request } from './client'
import type {
  PagedResponse,
  PortCall,
  PortCallInput,
  PortCallTransitionInput,
  PortReference,
  Vessel,
  VesselInput,
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
