import { environment } from '../config/environment'
import type { ProblemDetails, SessionResponse } from './types'

type SessionListener = (session: SessionResponse | null) => void

let accessToken: string | null = null
let sessionListener: SessionListener | null = null
let refreshPromise: Promise<SessionResponse> | null = null

export class ApiError extends Error {
  readonly status: number
  readonly code: string | null

  constructor(status: number, message: string, code: string | null = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

export function subscribeToSession(listener: SessionListener) {
  sessionListener = listener
  return () => {
    if (sessionListener === listener) {
      sessionListener = null
    }
  }
}

export function getAccessToken() {
  return accessToken
}

export async function signIn(email: string, password: string) {
  const session = await sessionRequest('/api/v1/auth/login', {
    body: JSON.stringify({ email, password }),
    headers: { 'Content-Type': 'application/json' },
  })
  setSession(session)
  return session
}

export async function restoreSession() {
  if (!refreshPromise) {
    refreshPromise = sessionRequest('/api/v1/auth/refresh')
      .then((session) => {
        setSession(session)
        return session
      })
      .catch((error: unknown) => {
        setSession(null)
        throw error
      })
      .finally(() => {
        refreshPromise = null
      })
  }

  return refreshPromise
}

export async function signOut() {
  try {
    await request<void>('/api/v1/auth/logout', { method: 'POST' }, false)
  } finally {
    setSession(null)
  }
}

export async function request<T>(
  path: string,
  init: RequestInit = {},
  retryAfterRefresh = true,
): Promise<T> {
  const headers = new Headers(init.headers)
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(buildUrl(path), {
    ...init,
    credentials: 'include',
    headers,
  })

  if (response.status === 401 && retryAfterRefresh && !path.startsWith('/api/v1/auth/')) {
    await restoreSession()
    return request<T>(path, init, false)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export async function download(path: string, retryAfterRefresh = true): Promise<Blob> {
  const headers = new Headers()
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(buildUrl(path), {
    credentials: 'include',
    headers,
  })

  if (response.status === 401 && retryAfterRefresh) {
    await restoreSession()
    return download(path, false)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return response.blob()
}

async function sessionRequest(path: string, init: RequestInit = {}) {
  const response = await fetch(buildUrl(path), {
    ...init,
    method: 'POST',
    credentials: 'include',
  })

  if (!response.ok) {
    throw await toApiError(response)
  }

  return response.json() as Promise<SessionResponse>
}

function setSession(session: SessionResponse | null) {
  accessToken = session?.accessToken ?? null
  sessionListener?.(session)
}

function buildUrl(path: string) {
  return `${environment.apiUrl.replace(/\/$/, '')}${path}`
}

async function toApiError(response: Response) {
  let problem: ProblemDetails | null = null
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    problem = null
  }

  return new ApiError(
    response.status,
    problem?.detail ?? 'Não foi possível concluir a solicitação.',
    problem?.code ?? null,
  )
}
