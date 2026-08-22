import { request } from './client'
import type { PagedResponse, PortCall, PortReference, Vessel } from './types'

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
